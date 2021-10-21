using System;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Protocol.Bolt2.NormalOperations;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public class FundingLockedMessageService : IBoltMessageService<FundingLocked>
    {
        private readonly ILogger<FundingLockedMessageService> _logger;
        private readonly IChannelCandidateRepository _channelCandidateRepository;
        private readonly IPaymentChannelRepository _paymentChannelRepository;
        private readonly ISecretStore _secretStore;
        private readonly ILightningKeyDerivation _lightningKeyDerivation;

        public FundingLockedMessageService(ILogger<FundingLockedMessageService> logger, 
            IChannelCandidateRepository channelCandidateRepository, IPaymentChannelRepository paymentChannelRepository, 
            ISecretStore secretStore, ILightningKeyDerivation lightningKeyDerivation)
        {
            _logger = logger;
            _channelCandidateRepository = channelCandidateRepository;
            _paymentChannelRepository = paymentChannelRepository;
            _secretStore = secretStore;
            _lightningKeyDerivation = lightningKeyDerivation;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<FundingLocked> message)
        {
            _logger.LogDebug("Processing funding locked from peer");

            var existingChannel =
                await _paymentChannelRepository.TryGetPaymentChannelAsync(message.MessagePayload.ChannelId); //TODO validate the message before using it

            if (existingChannel != null)
            {
                // this was a reconnection process for a new channel can just ignore the message
                return new EmptySuccessResponse();
            }
            
            var channelCandidate = await _channelCandidateRepository.GetAsync(message.MessagePayload.ChannelId);

            if (channelCandidate == null)
            {
                _logger.LogWarning($"Channel candidate not found in the repository for channel id {message.MessagePayload.ChannelId}");
                
                return new ErrorCloseChannelResponse(message.MessagePayload.ChannelId,
                    "open channel is in an invalid state");
            }

            channelCandidate.FundingLocked = message.MessagePayload;

            if (channelCandidate.ChannelOpener !=  ChannelSide.Local) // We will be publishing to block chain in that case
            {
                _logger.LogDebug("Confirmation of funding transaction not received yet");
                
                await _channelCandidateRepository.UpdateAsync(channelCandidate); //Waiting for confirmation from our side as well
                
                return new EmptySuccessResponse();
            }

            //Time to create the payment channel
            PaymentChannel paymentChannel = new PaymentChannel(
                channelCandidate.ChannelId ?? throw new InvalidOperationException(),
                channelCandidate.FundingSignedRemote?.Signature ?? throw new InvalidOperationException(),
                message.MessagePayload.NextPerCommitmentPoint ?? throw new InvalidOperationException(),
                new[]
                {
                    channelCandidate.AcceptChannel?.FirstPerCommitmentPoint ?? throw new InvalidOperationException()
                },
                channelCandidate.OpenChannel?.FundingSatoshis ?? throw new InvalidOperationException(),
                new OutPoint
                {
                    Hash = channelCandidate.FundingCreated?.FundingTxid ?? throw new InvalidOperationException(),
                    Index = channelCandidate.FundingCreated.FundingOutputIndex ?? throw new InvalidOperationException()
                },
                channelCandidate.OpenChannel.DustLimitSatoshis,
                channelCandidate.AcceptChannel.DustLimitSatoshis ?? throw new InvalidOperationException(),
                channelCandidate.OpenChannel.FeeratePerKw,
                channelCandidate.OpenChannel.FundingPubkey,
                channelCandidate.AcceptChannel.FundingPubkey ?? throw new InvalidOperationException(),
                channelCandidate.OpenChannel.PushMsat,
                channelCandidate.OpenChannel.GetBasePoints(),
                channelCandidate.AcceptChannel.GetBasePoints()
            );

            await _paymentChannelRepository.AddNewPaymentChannelAsync(paymentChannel);
            
            _logger.LogDebug("Payment channel created");
            
            //TODO update gossip repo with new channel

            var seed = _secretStore.GetSeed();
            var secrets = _lightningKeyDerivation.DeriveSecrets(seed);

            var fundingLockedResponse = new FundingLocked
            {
                ChannelId = channelCandidate.FundingLocked.ChannelId,
                NextPerCommitmentPoint = _lightningKeyDerivation.PerCommitmentPoint(secrets.Shaseed, paymentChannel.LocalCommitmentNumber + 1)
            };
            
            _logger.LogDebug("Replaying with funding locked ");

            return new SuccessWithOutputResponse(new BoltMessage { Payload = fundingLockedResponse });
        }
    }
}