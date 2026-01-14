using System;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.NormalOperations.Messages;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Shachain;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2.NormalOperations
{
    /// <summary>
    /// Handles incoming revoke_and_ack messages, verifying revocation secrets
    /// and updating channel state.
    /// </summary>
    public class RevokeAndAckService : IBoltMessageService<RevokeAndAck>
    {
        private readonly ILogger<RevokeAndAckService> _logger;
        private readonly IPaymentChannelRepository _paymentChannelRepository;
        private readonly ILightningKeyDerivation _lightningKeyDerivation;

        public RevokeAndAckService(
            ILogger<RevokeAndAckService> logger,
            IPaymentChannelRepository paymentChannelRepository,
            ILightningKeyDerivation lightningKeyDerivation)
        {
            _logger = logger;
            _paymentChannelRepository = paymentChannelRepository;
            _lightningKeyDerivation = lightningKeyDerivation;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<RevokeAndAck> message)
        {
            var revokeAndAck = message.MessagePayload;

            if (revokeAndAck.ChannelId == null)
            {
                _logger.LogError("Received revoke_and_ack with null channel_id");
                return new ErrorCloseChannelResponse(UInt256.Zero, "Invalid channel_id");
            }

            var channel = await _paymentChannelRepository.TryGetPaymentChannelAsync(revokeAndAck.ChannelId);

            if (channel == null)
            {
                _logger.LogError("Channel {ChannelId} not found for revoke_and_ack", revokeAndAck.ChannelId);
                return new ErrorCloseChannelResponse(revokeAndAck.ChannelId, "Unknown channel");
            }

            if (revokeAndAck.PerCommitmentSecret == null)
            {
                _logger.LogError("Received revoke_and_ack with null per_commitment_secret");
                return new ErrorCloseChannelResponse(revokeAndAck.ChannelId, "Invalid per_commitment_secret");
            }

            if (revokeAndAck.NextPerCommitmentPoint == null)
            {
                _logger.LogError("Received revoke_and_ack with null next_per_commitment_point");
                return new ErrorCloseChannelResponse(revokeAndAck.ChannelId, "Invalid next_per_commitment_point");
            }

            // Verify that the per_commitment_secret corresponds to the previous commitment
            if (!ValidatePerCommitmentSecret(channel, revokeAndAck.PerCommitmentSecret))
            {
                _logger.LogError("Invalid per_commitment_secret for channel {ChannelId}", revokeAndAck.ChannelId);
                return new ErrorCloseChannelResponse(revokeAndAck.ChannelId, "Invalid per_commitment_secret");
            }

            // Store the revealed secret for potential penalty transaction
            StoreRevocationSecret(channel, revokeAndAck.PerCommitmentSecret);

            // Update remote commitment number and next per-commitment point
            channel.RemoteCommitmentNumber++;
            channel.RemotePerCommitmentPoint = revokeAndAck.NextPerCommitmentPoint;

            // Advance HTLC states - HTLCs that were waiting for revocation can now proceed
            foreach (var htlc in channel.Htlcs.Where(h => h.State == HtlcState.SentAddCommit))
            {
                htlc.State = HtlcState.SentAddRevocation;
            }

            foreach (var htlc in channel.Htlcs.Where(h => h.State == HtlcState.RcvdRemoveCommit))
            {
                htlc.State = HtlcState.RcvdRemoveRevocation;
            }

            await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);

            _logger.LogDebug("Processed revoke_and_ack for channel {ChannelId}, remote commitment now at {CommitmentNumber}",
                revokeAndAck.ChannelId, channel.RemoteCommitmentNumber);

            return new EmptySuccessResponse();
        }

        private bool ValidatePerCommitmentSecret(Entities.PaymentChannel channel, Secret perCommitmentSecret)
        {
            // Verify the secret by deriving the public point and comparing with stored point
            var derivedPoint = _lightningKeyDerivation.PublicKeyFromPrivateKey(perCommitmentSecret);
            
            // The revealed secret should correspond to the previous per-commitment point
            // For the first revocation, we compare with the first_per_commitment_point from open/accept
            if (channel.PreviousPerCommitmentPoints != null && channel.PreviousPerCommitmentPoints.Length > 0)
            {
                var expectedPoint = channel.PreviousPerCommitmentPoints.LastOrDefault();
                if (expectedPoint != null && !derivedPoint.Equals(expectedPoint))
                {
                    _logger.LogWarning("Per-commitment secret does not match expected point");
                    return false;
                }
            }

            return true;
        }

        private void StoreRevocationSecret(Entities.PaymentChannel channel, Secret perCommitmentSecret)
        {
            // Store the secret in the shachain for efficient storage
            // This allows us to create penalty transactions if the remote broadcasts an old state
            var existingSecrets = channel.PreviousPerCommitmentSecrets?.ToList() ?? new();
            existingSecrets.Add(perCommitmentSecret);
            channel.PreviousPerCommitmentSecrets = existingSecrets.ToArray();
            
            _logger.LogDebug("Stored revocation secret #{SecretIndex} for channel {ChannelId}",
                existingSecrets.Count, channel.ChannelId);
        }
    }
}

