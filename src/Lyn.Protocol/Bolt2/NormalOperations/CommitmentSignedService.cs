using System;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.NormalOperations.Messages;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2.NormalOperations
{
    /// <summary>
    /// Handles incoming commitment_signed messages and generates revoke_and_ack responses.
    /// </summary>
    public class CommitmentSignedService : IBoltMessageService<CommitmentSigned>
    {
        private readonly ILogger<CommitmentSignedService> _logger;
        private readonly IPaymentChannelRepository _paymentChannelRepository;
        private readonly ILightningTransactions _lightningTransactions;
        private readonly ILightningKeyDerivation _lightningKeyDerivation;
        private readonly ISecretStore _secretStore;

        public CommitmentSignedService(
            ILogger<CommitmentSignedService> logger,
            IPaymentChannelRepository paymentChannelRepository,
            ILightningTransactions lightningTransactions,
            ILightningKeyDerivation lightningKeyDerivation,
            ISecretStore secretStore)
        {
            _logger = logger;
            _paymentChannelRepository = paymentChannelRepository;
            _lightningTransactions = lightningTransactions;
            _lightningKeyDerivation = lightningKeyDerivation;
            _secretStore = secretStore;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<CommitmentSigned> message)
        {
            var commitmentSigned = message.MessagePayload;

            if (commitmentSigned.ChannelId == null)
            {
                _logger.LogError("Received commitment_signed with null channel_id");
                return new ErrorCloseChannelResponse(UInt256.Zero, "Invalid channel_id");
            }

            var channel = await _paymentChannelRepository.TryGetPaymentChannelAsync(commitmentSigned.ChannelId);

            if (channel == null)
            {
                _logger.LogError("Channel {ChannelId} not found for commitment_signed", commitmentSigned.ChannelId);
                return new ErrorCloseChannelResponse(commitmentSigned.ChannelId, "Unknown channel");
            }

            // Validate the signature against the new commitment transaction
            if (!ValidateCommitmentSignature(channel, commitmentSigned))
            {
                _logger.LogError("Invalid commitment signature for channel {ChannelId}", commitmentSigned.ChannelId);
                return new ErrorCloseChannelResponse(commitmentSigned.ChannelId, "Invalid commitment signature");
            }

            // Validate HTLC signatures if present
            if (commitmentSigned.NumHtlcs > 0 && !ValidateHtlcSignatures(channel, commitmentSigned))
            {
                _logger.LogError("Invalid HTLC signatures for channel {ChannelId}", commitmentSigned.ChannelId);
                return new ErrorCloseChannelResponse(commitmentSigned.ChannelId, "Invalid HTLC signatures");
            }

            // Store the new commitment signature
            channel.CurrentRemoteCommitmentSignature = commitmentSigned.Signature;

            // Get current per-commitment secret to send in revoke_and_ack
            var seed = _secretStore.GetSeed();
            var secrets = _lightningKeyDerivation.DeriveSecrets(seed);
            
            var currentPerCommitmentSecret = _lightningKeyDerivation.PerCommitmentSecret(
                secrets.Shaseed, channel.LocalCommitmentNumber);

            // Calculate next per-commitment point
            var nextPerCommitmentPoint = _lightningKeyDerivation.PerCommitmentPoint(
                secrets.Shaseed, channel.LocalCommitmentNumber + 1);

            // Advance commitment number
            channel.LocalCommitmentNumber++;

            // Update HTLCs to committed state
            foreach (var htlc in channel.Htlcs.Where(h => h.State == HtlcState.RcvdAddHtlc))
            {
                htlc.State = HtlcState.RcvdAddCommit;
            }

            await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);

            _logger.LogDebug("Sending revoke_and_ack for channel {ChannelId}, commitment {CommitmentNumber}",
                commitmentSigned.ChannelId, channel.LocalCommitmentNumber);

            var revokeAndAck = new RevokeAndAck
            {
                ChannelId = commitmentSigned.ChannelId,
                PerCommitmentSecret = currentPerCommitmentSecret,
                NextPerCommitmentPoint = nextPerCommitmentPoint
            };

            return new SuccessWithOutputResponse(new BoltMessage { Payload = revokeAndAck });
        }

        private bool ValidateCommitmentSignature(Entities.PaymentChannel channel, CommitmentSigned commitmentSigned)
        {
            if (commitmentSigned.Signature == null)
                return false;

            // Build the expected commitment transaction and verify the signature
            // This is a simplified validation - full implementation would rebuild 
            // the commitment transaction and verify the signature against it
            
            // TODO: Implement full signature verification using _lightningTransactions
            _logger.LogDebug("Validating commitment signature for channel {ChannelId}", channel.ChannelId);
            
            return true; // Placeholder - needs full implementation
        }

        private bool ValidateHtlcSignatures(Entities.PaymentChannel channel, CommitmentSigned commitmentSigned)
        {
            if (commitmentSigned.NumHtlcs == null || commitmentSigned.NumHtlcs == 0)
                return true;

            // Validate each HTLC signature
            // TODO: Implement HTLC signature verification
            _logger.LogDebug("Validating {NumHtlcs} HTLC signatures for channel {ChannelId}", 
                commitmentSigned.NumHtlcs, channel.ChannelId);
            
            return true; // Placeholder - needs full implementation
        }
    }
}

