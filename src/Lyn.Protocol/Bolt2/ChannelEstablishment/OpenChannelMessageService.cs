using System;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Entities;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt2.Configuration;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public class OpenChannelMessageService : IBoltMessageService<OpenChannel>
    {
        private readonly ILogger<OpenChannelMessageService> _logger;
        private readonly ILightningTransactions _lightningTransactions;
        private readonly IRandomNumberGenerator _randomNumberGenerator;
        private readonly ILightningKeyDerivation _lightningKeyDerivation;
        private readonly IChannelCandidateRepository _channelStateRepository;
        private readonly IChainConfigProvider _chainConfigProvider;
        private readonly IChannelConfigProvider _channelConfigProvider;
        private readonly IPeerRepository _peerRepository;
        private readonly ISecretStore _secretStore;
        private readonly IBoltFeatures _boltFeatures;

        public OpenChannelMessageService(ILogger<OpenChannelMessageService> logger,
            ILightningTransactions lightningTransactions,
            IRandomNumberGenerator randomNumberGenerator,
            ILightningKeyDerivation lightningKeyDerivation,
            IChannelCandidateRepository channelStateRepository,
            IChainConfigProvider chainConfigProvider,
            IChannelConfigProvider channelConfigProvider,
            IPeerRepository peerRepository,
            ISecretStore secretStore,
            IBoltFeatures boltFeatures)
        {
            _logger = logger;
            _lightningTransactions = lightningTransactions;
            _randomNumberGenerator = randomNumberGenerator;
            _lightningKeyDerivation = lightningKeyDerivation;
            _channelStateRepository = channelStateRepository;
            _chainConfigProvider = chainConfigProvider;
            _channelConfigProvider = channelConfigProvider;
            _peerRepository = peerRepository;
            _secretStore = secretStore;
            _boltFeatures = boltFeatures;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<OpenChannel> message)
        {
            OpenChannel openChannel = message.MessagePayload;

            var peer = _peerRepository.TryGetPeerAsync(message.NodeId);

            if (peer == null) // todo: dan asked himself wtf
            {
                // todo: dan write this logic
                return new MessageProcessingOutput();
            }

            ChannelCandidate? currentState = await _channelStateRepository.GetAsync(message.MessagePayload.TemporaryChannelId);

            if (currentState != null)
            {
                // todo: dan write this logic
                return new MessageProcessingOutput();
            }

            ChainParameters? chainParameters = _chainConfigProvider.GetConfiguration(openChannel.ChainHash);

            if (chainParameters == null)
            {
                // todo: fail the channel.
                return new MessageProcessingOutput { CloseChannel = true };
            }

            ChannelConfig? channelConfig = _channelConfigProvider.GetConfiguration(openChannel.ChainHash);

            if (channelConfig == null)
            {
                // todo: fail the channel.
                return new MessageProcessingOutput { CloseChannel = true };
            }

            bool optionAnchorOutputs = (peer.Featurs & Features.OptionAnchorOutputs) != 0;

            string failReason = CheckMessage(openChannel, chainParameters, channelConfig, optionAnchorOutputs);

            if (!string.IsNullOrEmpty(failReason))
            {
                // todo: fail the channel.
                return new MessageProcessingOutput { CloseChannel = true };
            }

            AcceptChannel acceptChannel = new();

            Secret seed = _secretStore.GetSeed();
            Secrets secrets = _lightningKeyDerivation.DeriveSecrets(seed);

            acceptChannel.FundingPubkey = _lightningKeyDerivation.PublicKeyFromPrivateKey(secrets.FundingPrivkey);

            Basepoints basepoints = _lightningKeyDerivation.DeriveBasepoints(secrets);
            acceptChannel.RevocationBasepoint = basepoints.Revocation;
            acceptChannel.HtlcBasepoint = basepoints.Htlc;
            acceptChannel.PaymentBasepoint = basepoints.Payment;
            acceptChannel.DelayedPaymentBasepoint = basepoints.DelayedPayment;

            acceptChannel.FirstPerCommitmentPoint = _lightningKeyDerivation.PerCommitmentPoint(secrets.Shaseed, 0);

            ChannelCandidate channelCandidate = new()
            {
                ChannelOpener = ChannelSide.Remote,
                ChannelId = openChannel.TemporaryChannelId,
                OpenChannel = openChannel,
                AcceptChannel = acceptChannel
            };

            await _channelStateRepository.CreateAsync(channelCandidate);

            var boltMessage = new BoltMessage
            {
                Payload = acceptChannel
            };

            return new MessageProcessingOutput { Success = true, ResponseMessages = new[] { boltMessage } };
        }

        private string CheckMessage(OpenChannel openChannel, ChainParameters chainParameters, ChannelConfig channelConfig, bool optionAnchorOutputs)
        {
            if (openChannel.FundingSatoshis < chainParameters.MinFundingAmount) return "funding_satoshis is too small";
            if (openChannel.HtlcMinimumMsat > channelConfig.HtlcMinimum) return "htlc_minimum_msat too large";
            if (openChannel.MaxHtlcValueInFlightMsat < channelConfig.MaxHtlcValueInFlight) return "max_htlc_value_in_flight_msat too small";
            if (openChannel.ChannelReserveSatoshis > channelConfig.ChannelReserve) return "channel_reserve_satoshis too large";
            if (openChannel.MaxAcceptedHtlcs < channelConfig.MaxAcceptedHtlcs) return "max_accepted_htlcs too small";
            if (openChannel.DustLimitSatoshis < channelConfig.DustLimit) return "dust_limit_satoshis too small";

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

            Satoshis baseFee = _lightningTransactions.GetBaseFee(openChannel.FeeratePerKw, optionAnchorOutputs, 0);
            if (openChannel.FundingSatoshis < openChannel.ChannelReserveSatoshis + baseFee) return "the funder's amount for the initial commitment transaction is not sufficient for full fee payment.";

            if ((openChannel.FundingSatoshis > chainParameters.LargeChannelAmount)
                && _boltFeatures.SupportedFeatures != Features.OptionSupportLargeChannel) return "funding_satoshis too big for option_support_large_channel";

            return string.Empty;
        }
    }
}