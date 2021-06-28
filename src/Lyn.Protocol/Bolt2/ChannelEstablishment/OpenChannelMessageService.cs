using System;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt2.Configuration;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Protocol.Connection;
using Lyn.Types.Bolt;
using Lyn.Types.Bolt.Messages;
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
        private readonly ISecretProvider _secretProvider;
        private readonly IBoltFeatures _boltFeatures;
        private readonly IParseFeatureFlags _parseFeatureFlags;
        private readonly IBoltMessageSender<AcceptChannel> _messageSender;

        public OpenChannelMessageService(ILogger<OpenChannelMessageService> logger,
            IBoltValidationService<OpenChannel> validationService,
            IBoltMessageSender<AcceptChannel> messageSender,
            ILightningTransactions lightningTransactions,
            IRandomNumberGenerator randomNumberGenerator,
            ILightningKeyDerivation lightningKeyDerivation,
            IChannelStateRepository channelStateRepository,
            IChainConfigProvider chainConfigProvider,
            IChannelConfigProvider channelConfigProvider,
            ISecretProvider secretProvider,
            IBoltFeatures boltFeatures,
            IParseFeatureFlags parseFeatureFlags)
        {
            _logger = logger;
            _validationService = validationService;
            _lightningTransactions = lightningTransactions;
            _randomNumberGenerator = randomNumberGenerator;
            _lightningKeyDerivation = lightningKeyDerivation;
            _channelStateRepository = channelStateRepository;
            _chainConfigProvider = chainConfigProvider;
            _channelConfigProvider = channelConfigProvider;
            _secretProvider = secretProvider;
            _boltFeatures = boltFeatures;
            _parseFeatureFlags = parseFeatureFlags;
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

            string failReason = CheckMessage(openChannel, chainParameters, channelConfig);

            if (!string.IsNullOrEmpty(failReason))
            {
                // todo: fail the channel.
                return;
            }

            // Create the channel durable state
            ChannelState channelState = new()
            {
                FundingAmount = openChannel.FundingSatoshis,
                ChannelId = openChannel.TemporaryChannelId,
                RemotePublicKey = openChannel.FundingPubkey,
                PushMsat = openChannel.PushMsat,
                RemoteFirstPerCommitmentPoint = openChannel.FirstPerCommitmentPoint,
                RemotePoints = new Basepoints
                {
                    Payment = openChannel.PaymentBasepoint,
                    Htlc = openChannel.HtlcBasepoint,
                    DelayedPayment = openChannel.DelayedPaymentBasepoint,
                    Revocation = openChannel.RevocationBasepoint,
                },
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

            AcceptChannel acceptChannel = new();

            Secret seed = _secretProvider.GetSeed();
            Secrets secrets = _lightningKeyDerivation.DeriveSecrets(seed);

            acceptChannel.FundingPubkey = _lightningKeyDerivation.PublicKeyFromPrivateKey(secrets.FundingPrivkey);

            // Bolt 2 - MUST set funding_pubkey, revocation_basepoint, htlc_basepoint, payment_basepoint,
            // and delayed_payment_basepoint to valid secp256k1 pubkeys in compressed format
            Basepoints basepoints = _lightningKeyDerivation.DeriveBasepoints(secrets);
            acceptChannel.RevocationBasepoint = basepoints.Revocation;
            acceptChannel.HtlcBasepoint = basepoints.Htlc;
            acceptChannel.PaymentBasepoint = basepoints.Payment;
            acceptChannel.DelayedPaymentBasepoint = basepoints.DelayedPayment;

            // Bolt 2 - MUST set first_per_commitment_point to the per-commitment point to be used for the initial commitment transaction, derived as specified in BOLT #3.
            acceptChannel.FirstPerCommitmentPoint = _lightningKeyDerivation.PerCommitmentPoint(secrets.Shaseed, 0);

            channelState.LocalPublicKey = acceptChannel.FundingPubkey;
            channelState.LocalPoints = basepoints;
            channelState.LocalConfig = channelConfig;
            channelState.LocalFirstPerCommitmentPoint = acceptChannel.FirstPerCommitmentPoint;

            _channelStateRepository.Create(channelState);

            await _messageSender.SendMessageAsync(new PeerMessage<AcceptChannel>(message.NodeId, acceptChannel));
        }

        private string CheckMessage(OpenChannel openChannel, ChainParameters chainParameters, ChannelConfig channelConfig)
        {
            // Bolt 2 -
            //  The receiving node MAY fail the channel if:
            //    announce_channel is false(0), yet it wishes to publicly announce the channel.
            //    funding_satoshis is too small.
            //    it considers htlc_minimum_msat too large.
            //    it considers max_htlc_value_in_flight_msat too small.
            //    it considers channel_reserve_satoshis too large.
            //    it considers max_accepted_htlcs too small.
            //    it considers dust_limit_satoshis too small and plans to rely on the sending node publishing its commitment
            //       transaction in the event of a data loss(see message-retransmission).

            if (openChannel.FundingSatoshis < chainParameters.MinFundingAmount) return "funding_satoshis is too small";
            if (openChannel.HtlcMinimumMsat > channelConfig.HtlcMinimum) return "htlc_minimum_msat too large";
            if (openChannel.MaxHtlcValueInFlightMsat < channelConfig.MaxHtlcValueInFlight) return "max_htlc_value_in_flight_msat too small";
            if (openChannel.ChannelReserveSatoshis > channelConfig.ChannelReserve) return "channel_reserve_satoshis too large";
            if (openChannel.MaxAcceptedHtlcs < channelConfig.MaxAcceptedHtlcs) return "max_accepted_htlcs too small";
            if (openChannel.DustLimitSatoshis < channelConfig.DustLimit) return "dust_limit_satoshis too small";

            // Bolt 2 -
            //  The receiving node MUST fail the channel if:
            //    the chain_hash value is set to a hash of a chain that is unknown to the receiver.
            //    push_msat is greater than funding_satoshis * 1000.
            //    to_self_delay is unreasonably large.
            //    max_accepted_htlcs is greater than 483.
            //    it considers feerate_per_kw too small for timely processing or unreasonably large.
            //    funding_pubkey, revocation_basepoint, htlc_basepoint, payment_basepoint, or delayed_payment_basepoint are not valid secp256k1 pubkeys in compressed format.
            //    dust_limit_satoshis is greater than channel_reserve_satoshis.
            //    the funder's amount for the initial commitment transaction is not sufficient for full fee payment.
            //    both to_local and to_remote amounts for the initial commitment transaction are less than or equal to channel_reserve_satoshis(see BOLT 3).
            //    funding_satoshis is greater than or equal to 2 ^ 24 and the receiver does not support option_support_large_channel.

            MiliSatoshis fundingMiliSatoshis = openChannel.FundingSatoshis;
            if (openChannel.PushMsat > fundingMiliSatoshis) return "push_msat is greater than funding_satoshis * 1000";
            if (openChannel.ToSelfDelay > chainParameters.MaxToSelfDelay) return "to_self_delay is unreasonably large";
            if (openChannel.MaxAcceptedHtlcs > 483) return "max_accepted_htlcs is greater than 483";
            if (openChannel.FeeratePerKw < chainParameters.TooLowFeeratePerKw) return "feerate_per_kw too small for timely processing";
            if (openChannel.FeeratePerKw > chainParameters.TooLargeFeeratePerKw) return "feerate_per_kw unreasonably large";

            if (!_lightningKeyDerivation.IsValidPublicKey(openChannel.FundingPubkey)) return "funding_pubkey not valid secp256k1 pubkeys in compressed format";
            if (!_lightningKeyDerivation.IsValidPublicKey(openChannel.RevocationBasepoint)) return "revocation_basepoint not valid secp256k1 pubkeys in compressed format";
            if (!_lightningKeyDerivation.IsValidPublicKey(openChannel.HtlcBasepoint)) return "htlc_basepoint not valid secp256k1 pubkeys in compressed format";
            if (!_lightningKeyDerivation.IsValidPublicKey(openChannel.PaymentBasepoint)) return "payment_basepoint not valid secp256k1 pubkeys in compressed format";
            if (!_lightningKeyDerivation.IsValidPublicKey(openChannel.DelayedPaymentBasepoint)) return "delayed_payment_basepoint not valid secp256k1 pubkeys in compressed format";

            if (openChannel.DustLimitSatoshis > openChannel.ChannelReserveSatoshis) return "dust_limit_satoshis is greater than channel_reserve_satoshis";

            // the funder's amount for the initial commitment transaction is not sufficient for full fee payment.
            // todo: dan calculate the full fee payment
            // todo: what needs to be doen here is we need to use the helper methods in bol3 to calculate
            // todo: the commitment transaction base fee and check that its above the channel capacity.

            if ((openChannel.FundingSatoshis > chainParameters.LargeChannelAmount)
                && _boltFeatures.SupportedFeatures != Features.OptionSupportLargeChannel) return "funding_satoshis too big for option_support_large_channel";

            return String.Empty;
        }
    }
}