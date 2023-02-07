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
                _logger.LogError("Payment channel not found for channel id {message.MessagePayload.ChannelId} unable to reestablish connection");
                
                return new ErrorCloseChannelResponse(message.MessagePayload.ChannelId, "Channel not found");
            }


            if (paymentChannel.RemotePerCommitmentPoint.Equals(message.MessagePayload.MyCurrentPerCommitmentPoint))
            {
                _logger.LogDebug("Remote current commitment point has not changed");
            }

            var seed = _secretStore.GetSeed();
            var secrets = _lightningKeyDerivation.DeriveSecrets(seed);

            var currentCommitmentNumber = paymentChannel.LocalCommitmentNumber;
            var currentPoint = _lightningKeyDerivation.PerCommitmentPoint(secrets.Shaseed,
                paymentChannel.LocalCommitmentNumber); //TODO missing anchor outputs and remote static support here

            var lastKnownSecret = paymentChannel.PreviousPerCommitmentSecrets?.LastOrDefault() ?? new Secret(new byte[32]);

            var responseMessages = new List<MessagePayload>
            {
                new ChannelReestablish(paymentChannel.ChannelId, currentCommitmentNumber,
                    paymentChannel.RemoteCommitmentNumber, lastKnownSecret, currentPoint)
            };

            if (message.MessagePayload.NextCommitmentNumber == 1 && paymentChannel.LocalCommitmentNumber == 0)
            {
                responseMessages.Add(new FundingLocked
                {
                    ChannelId = paymentChannel.ChannelId,
                    NextPerCommitmentPoint = _lightningKeyDerivation
                        .PerCommitmentPoint(secrets.Shaseed, paymentChannel.LocalCommitmentNumber + 1)
                });
            }


            return new SuccessWithOutputResponse(
                responseMessages.Select(_ => new BoltMessage { Payload = _ }).ToArray());
        }
    }
}