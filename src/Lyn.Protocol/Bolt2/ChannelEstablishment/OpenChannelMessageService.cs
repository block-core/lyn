using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Protocol.Connection;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public class OpenChannelMessageService : IBoltMessageService<OpenChannel>
    {
        private readonly ILogger<OpenChannelMessageService> _logger;
        private readonly IBoltValidationService<OpenChannel> _validationService;
        private readonly ILightningTransactions _lightningTransactions;
        private readonly IRandomNumberGenerator _randomNumberGenerator;
        private readonly ILightningKeyDerivation _lightningKeyDerivation;
        private readonly IChannelStateRepository _channelStateRepository;
        private readonly IBoltMessageSender<AcceptChannel> _messageSender;

        public OpenChannelMessageService(ILogger<OpenChannelMessageService> logger,
            IBoltValidationService<OpenChannel> validationService,
            IBoltMessageSender<AcceptChannel> messageSender,
            ILightningTransactions lightningTransactions,
            IRandomNumberGenerator randomNumberGenerator,
            ILightningKeyDerivation lightningKeyDerivation,
            IChannelStateRepository channelStateRepository)
        {
            _logger = logger;
            _validationService = validationService;
            _lightningTransactions = lightningTransactions;
            _randomNumberGenerator = randomNumberGenerator;
            _lightningKeyDerivation = lightningKeyDerivation;
            _channelStateRepository = channelStateRepository;
            _messageSender = messageSender;
        }

        public async Task ProcessMessageAsync(PeerMessage<OpenChannel> message)
        {
            if (!await _validationService.ValidateMessageAsync(message).ConfigureAwait(false)) return;

            OpenChannel openChannel = message.Message;

            // Create the channel durable state
            ChannelState channelState = new()
            {
                Funding = openChannel.FundingSatoshis,
                ChannelId = openChannel.TemporaryChannelId,
                LocalPublicKey = openChannel.FundingPubkey,
                LocalPoints = new Basepoints
                {
                    Payment = openChannel.PaymentBasepoint,
                    Htlc = openChannel.HtlcBasepoint,
                    DelayedPayment = openChannel.DelayedPaymentBasepoint,
                    Revocation = openChannel.RevocationBasepoint,
                },
                PushMsat = openChannel.PushMsat,
                LocalFirstPerCommitmentPoint = openChannel.FirstPerCommitmentPoint,
            };

            _channelStateRepository.AddOrUpdate(channelState);

            AcceptChannel acceptChannel = new(); // todo: dan create the accept channel code

            await _messageSender.SendMessageAsync(new PeerMessage<AcceptChannel> { Message = acceptChannel, NodeId = message.NodeId });

            return;
        }
    }
}