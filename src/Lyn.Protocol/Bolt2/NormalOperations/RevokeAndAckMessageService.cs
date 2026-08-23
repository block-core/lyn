using System;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.NormalOperations.Messages;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2.NormalOperations
{
    public class RevokeAndAckMessageService : IBoltMessageService<RevokeAndAck>
    {
        private readonly ILogger<RevokeAndAckMessageService> _logger;
        private readonly IPaymentChannelRepository _paymentChannelRepository;
        private readonly ILightningKeyDerivation _lightningKeyDerivation;

        public RevokeAndAckMessageService(ILogger<RevokeAndAckMessageService> logger,
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

            if (revokeAndAck.ChannelId is null || revokeAndAck.PerCommitmentSecret is null ||
                revokeAndAck.NextPerCommitmentPoint is null)
                return new ErrorCloseChannelResponse(revokeAndAck.ChannelId ?? new UInt256(new byte[32]),
                    "Invalid revoke_and_ack message");

            var channel =
                await _paymentChannelRepository.TryGetPaymentChannelAsync(revokeAndAck.ChannelId);

            if (channel is null)
            {
                _logger.LogError("Payment channel not found for channel id {ChannelId}", revokeAndAck.ChannelId);

                return new ErrorCloseChannelResponse(revokeAndAck.ChannelId, "Channel not found");
            }

            // Verify that the revoked secret matches the previous per-commitment point we know about.
            var revokedSecretPubKey =
                _lightningKeyDerivation.PublicKeyFromPrivateKey((PrivateKey)revokeAndAck.PerCommitmentSecret);

            if (!revokedSecretPubKey.Equals(channel.PerCommitmentPoint))
                return new ErrorCloseChannelResponse(revokeAndAck.ChannelId,
                    "per_commitment_secret does not match previous per_commitment_point");

            // Rotate to the new per-commitment point provided by the remote peer.
            channel.PreviousPerCommitmentPoints = channel.PreviousPerCommitmentPoints
                .Append(channel.PerCommitmentPoint).ToArray();
            channel.PreviousPerCommitmentSecrets = channel.PreviousPerCommitmentSecrets
                .Append(revokeAndAck.PerCommitmentSecret).ToArray();
            channel.PerCommitmentPoint = revokeAndAck.NextPerCommitmentPoint;
            channel.RemoteCommitmentNumber++;

            await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);

            _logger.LogDebug("Processed revoke_and_ack for channel {ChannelId}", revokeAndAck.ChannelId);

            return new EmptySuccessResponse();
        }
    }
}
