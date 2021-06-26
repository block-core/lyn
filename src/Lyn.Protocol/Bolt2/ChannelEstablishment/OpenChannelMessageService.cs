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
        private readonly IChainConfigProvider _chainConfigProvider;
        private readonly IChannelConfigProvider _channelConfigProvider;
        private readonly IBoltMessageSender<AcceptChannel> _messageSender;

        public OpenChannelMessageService(ILogger<OpenChannelMessageService> logger,
            IBoltValidationService<OpenChannel> validationService,
            IBoltMessageSender<AcceptChannel> messageSender,
            ILightningTransactions lightningTransactions,
            IRandomNumberGenerator randomNumberGenerator,
            ILightningKeyDerivation lightningKeyDerivation,
            IChannelStateRepository channelStateRepository,
            IChainConfigProvider chainConfigProvider,
            IChannelConfigProvider channelConfigProvider)
        {
            _logger = logger;
            _validationService = validationService;
            _lightningTransactions = lightningTransactions;
            _randomNumberGenerator = randomNumberGenerator;
            _lightningKeyDerivation = lightningKeyDerivation;
            _channelStateRepository = channelStateRepository;
            _chainConfigProvider = chainConfigProvider;
            _channelConfigProvider = channelConfigProvider;
            _messageSender = messageSender;
        }

        public async Task ProcessMessageAsync(PeerMessage<OpenChannel> message)
        {
            OpenChannel openChannel = message.Message;

            ChannelState? currentState = _channelStateRepository.Get(message.Message.TemporaryChannelId);

            if (currentState != null)
            {
                // Bolt 2 - if the connection has been re-established after receiving a previous open_channel, BUT before receiving a funding_created message:
                // accept a new open_channel message.
                //    discard the previous open_channel message.

                // todo: dan write this logic
                return;
            }

            ChainParameters? chainParameters = _chainConfigProvider.GetConfiguration(openChannel.ChainHash);

            if (chainParameters == null)
            {
                // Bolt 2 - the chain_hash value is set to a hash of a chain that is unknown to the receiver.

                // todo: fail the channel.
                return;
            }

            ChannelConfig? channelConfig = _channelConfigProvider.GetConfiguration(openChannel.ChainHash);

            if (channelConfig == null)
            {
                // This is an internal issue but we still need to fail the channel

                // todo: fail the channel.
                return;
            }

            if (openChannel.ChannelReserveSatoshis < channelConfig.DustLimit)
            {
                // Bolt 2 - MUST set channel_reserve_satoshis greater than or equal to dust_limit_satoshis.

                // todo: fail the channel.
                return;
            }

            if (channelConfig.ChannelReserve < channelConfig.DustLimit)
            {
                // This is an internal issue but we still need to fail the channel

                // todo: fail the channel.
                return;
            }

            Secrets secrets = _lightningKeyDerivation.DeriveSecrets(null); // todo: dan create seed store

            openChannel.FundingPubkey = _lightningKeyDerivation.PublicKeyFromPrivateKey(secrets.FundingPrivkey);

            // Bolt 2 - MUST set funding_pubkey, revocation_basepoint, htlc_basepoint, payment_basepoint,
            // and delayed_payment_basepoint to valid secp256k1 pubkeys in compressed format
            Basepoints basepoints = _lightningKeyDerivation.DeriveBasepoints(secrets);
            openChannel.RevocationBasepoint = basepoints.Revocation;
            openChannel.HtlcBasepoint = basepoints.Htlc;
            openChannel.PaymentBasepoint = basepoints.Payment;
            openChannel.DelayedPaymentBasepoint = basepoints.DelayedPayment;

            // Bolt 2 - MUST set first_per_commitment_point to the per-commitment point to be used for the initial commitment transaction, derived as specified in BOLT #3.
            openChannel.FirstPerCommitmentPoint = _lightningKeyDerivation.PerCommitmentPoint(secrets.Shaseed, 0);

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
                LocalConfig = channelConfig,
            };

            _channelStateRepository.Create(channelState);

            AcceptChannel acceptChannel = new(); // todo: dan create the accept channel code

            await _messageSender.SendMessageAsync(new PeerMessage<AcceptChannel>(message.NodeId, acceptChannel));
        }
    }
}