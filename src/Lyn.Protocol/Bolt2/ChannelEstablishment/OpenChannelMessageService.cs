using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt2.Configuration;
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
            OpenChannel openChannel = message.Message;

            // Create the channel durable state
            ChannelState channelState = new()
            {
                Funding = openChannel.FundingSatoshis,
                ChannelId = openChannel.TemporaryChannelId,
                RemotePublicKey = openChannel.FundingPubkey,
                RemotePoints = new Basepoints
                {
                    Payment = openChannel.PaymentBasepoint,
                    Htlc = openChannel.HtlcBasepoint,
                    DelayedPayment = openChannel.DelayedPaymentBasepoint,
                    Revocation = openChannel.RevocationBasepoint,
                },
                PushMsat = openChannel.PushMsat,
                RemoteFirstPerCommitmentPoint = openChannel.FirstPerCommitmentPoint,
                RemoteConfig = new ChannelConfig
                {
                    ToSelfDelay = openChannel.ToSelfDelay,
                    ChannelReserve = openChannel.ChannelReserveSatoshis,
                    DustLimit = openChannel.DustLimitSatoshis,
                    HtlcMinimum = openChannel.HtlcMinimumMsat,
                    MaxAcceptedHtlcs = openChannel.MaxAcceptedHtlcs,
                    MaxHtlcValueInFlight = openChannel.MaxHtlcValueInFlightMsat
                },
            };

            _channelStateRepository.Create(channelState);

            AcceptChannel acceptChannel = new(); // todo: dan create the accept channel code

            await _messageSender.SendMessageAsync(new PeerMessage<AcceptChannel> { Message = acceptChannel, NodeId = message.NodeId });

            return;
        }
    }
}