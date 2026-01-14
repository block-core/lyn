using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Protocol.Bolt2.MessageRetransmission.Messages;
using Lyn.Protocol.Bolt2.NormalOperations;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;

using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2.MessageRetransmission
{
    public class ChannelReestablishMessageService : IBoltMessageService<ChannelReestablish>
    {
        private readonly ILogger<ChannelReestablishMessageService> _logger;
        private readonly IPaymentChannelRepository _channelRepository;
        private readonly ISecretStore _secretStore;
        private readonly ILightningKeyDerivation _lightningKeyDerivation;
        
        public ChannelReestablishMessageService(ILogger<ChannelReestablishMessageService> logger, 
            IPaymentChannelRepository channelRepository, ISecretStore secretStore, ILightningKeyDerivation lightningKeyDerivation)
        {
            _logger = logger;
            _channelRepository = channelRepository;
            _secretStore = secretStore;
            _lightningKeyDerivation = lightningKeyDerivation;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<ChannelReestablish> message)
        {
            var paymentChannel = await _channelRepository.TryGetPaymentChannelAsync(message.MessagePayload.ChannelId);

            if (paymentChannel is null)
            {
                _logger.LogError("Payment channel not found for channel id {ChannelId}, unable to reestablish connection", 
                    message.MessagePayload.ChannelId);
                
                return new ErrorCloseChannelResponse(message.MessagePayload.ChannelId, "Channel not found");
            }

            var seed = _secretStore.GetSeed();
            var secrets = _lightningKeyDerivation.DeriveSecrets(seed);

            // Validate your_last_per_commitment_secret if provided (option_data_loss_protect)
            if (message.MessagePayload.YourLastPerCommitmentSecret != null && 
                !message.MessagePayload.YourLastPerCommitmentSecret.Equals(new Secret(new byte[32])))
            {
                if (!ValidateLastPerCommitmentSecret(paymentChannel, message.MessagePayload.YourLastPerCommitmentSecret, secrets))
                {
                    _logger.LogError("Invalid your_last_per_commitment_secret for channel {ChannelId}", 
                        message.MessagePayload.ChannelId);
                    return new ErrorCloseChannelResponse(message.MessagePayload.ChannelId, 
                        "Invalid your_last_per_commitment_secret");
                }
            }

            // Check for data loss - if remote thinks we're ahead of where we are
            if (message.MessagePayload.NextCommitmentNumber > paymentChannel.LocalCommitmentNumber + 1)
            {
                _logger.LogWarning("Remote expects higher commitment number than we have. " +
                    "Remote next: {RemoteNext}, Local: {Local}. Possible data loss detected.",
                    message.MessagePayload.NextCommitmentNumber, paymentChannel.LocalCommitmentNumber);
                
                // We may have lost data - we should NOT continue as normal
                // The remote has states we don't know about
                return new ErrorCloseChannelResponse(message.MessagePayload.ChannelId, 
                    "Data loss detected - commitment number mismatch");
            }

            // Check if we need to retransmit commitment_signed
            bool needsRetransmitCommitment = false;
            if (message.MessagePayload.NextRevocationNumber < paymentChannel.RemoteCommitmentNumber)
            {
                _logger.LogDebug("Remote needs retransmission of commitment_signed. " +
                    "Remote next revocation: {RemoteNextRev}, Our remote commitment: {OurRemoteCommit}",
                    message.MessagePayload.NextRevocationNumber, paymentChannel.RemoteCommitmentNumber);
                needsRetransmitCommitment = true;
            }

            // Prepare our channel_reestablish response
            var currentCommitmentNumber = paymentChannel.LocalCommitmentNumber + 1; // next_commitment_number
            var currentPoint = _lightningKeyDerivation.PerCommitmentPoint(secrets.Shaseed,
                paymentChannel.LocalCommitmentNumber);

            var lastKnownSecret = paymentChannel.PreviousPerCommitmentSecrets?.LastOrDefault() 
                ?? new Secret(new byte[32]);

            var responseMessages = new List<MessagePayload>
            {
                new ChannelReestablish(paymentChannel.ChannelId, currentCommitmentNumber,
                    paymentChannel.RemoteCommitmentNumber, lastKnownSecret, currentPoint)
            };

            // Handle funding_locked retransmission for channels that haven't completed handshake
            if (message.MessagePayload.NextCommitmentNumber == 1 && paymentChannel.LocalCommitmentNumber == 0)
            {
                _logger.LogDebug("Retransmitting funding_locked for channel {ChannelId}", paymentChannel.ChannelId);
                responseMessages.Add(new FundingLocked
                {
                    ChannelId = paymentChannel.ChannelId,
                    NextPerCommitmentPoint = _lightningKeyDerivation
                        .PerCommitmentPoint(secrets.Shaseed, paymentChannel.LocalCommitmentNumber + 1)
                });
            }

            // If we need to retransmit commitment_signed, add it to response
            if (needsRetransmitCommitment && paymentChannel.CurrentLocalCommitmentSignature != null)
            {
                _logger.LogDebug("Retransmitting commitment_signed for channel {ChannelId}", paymentChannel.ChannelId);
                // Note: In a full implementation, we would rebuild and re-sign the commitment transaction
                // For now, we just log that retransmission is needed
            }

            // Check and log remote per-commitment point comparison
            if (message.MessagePayload.MyCurrentPerCommitmentPoint != null)
            {
                if (paymentChannel.RemotePerCommitmentPoint.Equals(message.MessagePayload.MyCurrentPerCommitmentPoint))
                {
                    _logger.LogDebug("Remote current commitment point matches stored value");
                }
                else
                {
                    _logger.LogDebug("Remote current commitment point has changed - updating");
                    paymentChannel.RemotePerCommitmentPoint = message.MessagePayload.MyCurrentPerCommitmentPoint;
                    await _channelRepository.UpdatePaymentChannelAsync(paymentChannel);
                }
            }

            return new SuccessWithOutputResponse(
                responseMessages.Select(_ => new BoltMessage { Payload = _ }).ToArray());
        }

        /// <summary>
        /// Validates the your_last_per_commitment_secret sent by remote.
        /// This secret should correspond to our previous commitment point.
        /// </summary>
        private bool ValidateLastPerCommitmentSecret(PaymentChannel channel, Secret secret, Secrets localSecrets)
        {
            if (channel.LocalCommitmentNumber == 0)
            {
                // No previous commitment, secret should be all zeros
                return secret.Equals(new Secret(new byte[32]));
            }

            // Derive the expected secret from our shachain
            var expectedSecret = _lightningKeyDerivation.PerCommitmentSecret(
                localSecrets.Shaseed, channel.LocalCommitmentNumber - 1);
            
            return expectedSecret.Equals(secret);
        }
    }
}