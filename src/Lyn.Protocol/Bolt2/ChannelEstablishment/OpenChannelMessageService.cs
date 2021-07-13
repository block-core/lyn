using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Entities;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages.TlvRecords;
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
        private readonly IPeerRepository _peerRepository;
        private readonly ISecretStore _secretStore;
        private readonly IBoltFeatures _boltFeatures;

        public OpenChannelMessageService(ILogger<OpenChannelMessageService> logger,
            ILightningTransactions lightningTransactions,
            IRandomNumberGenerator randomNumberGenerator,
            ILightningKeyDerivation lightningKeyDerivation,
            IChannelCandidateRepository channelStateRepository,
            IChainConfigProvider chainConfigProvider,
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
                return MessageProcessingOutput.CreateErrorMessage(openChannel.TemporaryChannelId, true, "chainhash is unknowen");
            }

            bool optionAnchorOutputs = (peer.Featurs & Features.OptionAnchorOutputs) != 0;

            string failReason = CheckMessage(openChannel, chainParameters, optionAnchorOutputs);

            if (!string.IsNullOrEmpty(failReason))
            {
                return MessageProcessingOutput.CreateErrorMessage(openChannel.TemporaryChannelId, true, failReason);
            }

            byte[]? remoteUpfrontShutdownScript = null;
            byte[]? localUpfrontShutdownScript = chainParameters.ChannelConfig.UpfrontShutdownScript;
            bool localSupportUpfrontShutdownScript = (_boltFeatures.SupportedFeatures & Features.OptionUpfrontShutdownScript) != 0;
            bool remoteSupportUpfrontShutdownScript = (peer.Featurs & Features.OptionUpfrontShutdownScript) != 0;

            if (remoteSupportUpfrontShutdownScript)
            {
                remoteUpfrontShutdownScript = message.Message.Extension?.Records.OfType<UpfrontShutdownScriptTlvRecord>().FirstOrDefault()?.ShutdownScriptpubkey;

                if (remoteUpfrontShutdownScript == null)
                {
                    return MessageProcessingOutput.CreateErrorMessage(openChannel.TemporaryChannelId, true, "failed to open channel");
                }
            }

            if (localSupportUpfrontShutdownScript && remoteSupportUpfrontShutdownScript)
            {
                if (localUpfrontShutdownScript == null || localUpfrontShutdownScript.Length == 0)
                    localUpfrontShutdownScript = new byte[] { 0x0000 };
            }

            AcceptChannel acceptChannel = new();
            acceptChannel.TemporaryChannelId = openChannel.TemporaryChannelId;

            Secret seed = _secretStore.GetSeed();
            Secrets secrets = _lightningKeyDerivation.DeriveSecrets(seed);

            acceptChannel.FundingPubkey = _lightningKeyDerivation.PublicKeyFromPrivateKey(secrets.FundingPrivkey);

            Basepoints basepoints = _lightningKeyDerivation.DeriveBasepoints(secrets);
            acceptChannel.RevocationBasepoint = basepoints.Revocation;
            acceptChannel.HtlcBasepoint = basepoints.Htlc;
            acceptChannel.PaymentBasepoint = basepoints.Payment;
            acceptChannel.DelayedPaymentBasepoint = basepoints.DelayedPayment;

            acceptChannel.FirstPerCommitmentPoint = _lightningKeyDerivation.PerCommitmentPoint(secrets.Shaseed, 0);

            acceptChannel.DustLimitSatoshis = chainParameters.ChannelConfig.DustLimit;
            Satoshis localReserve = (ulong)(chainParameters.ChannelBoundariesConfig.ChannelReservePercentage * (ulong)openChannel.FundingSatoshis);
            acceptChannel.ChannelReserveSatoshis = localReserve;
            acceptChannel.HtlcMinimumMsat = chainParameters.ChannelConfig.HtlcMinimum;
            acceptChannel.MaxHtlcValueInFlightMsat = chainParameters.ChannelConfig.MaxHtlcValueInFlight;
            acceptChannel.MinimumDepth = chainParameters.ChannelBoundariesConfig.MinimumDepth;
            acceptChannel.ToSelfDelay = chainParameters.ChannelConfig.ToSelfDelay;
            acceptChannel.MaxAcceptedHtlcs = chainParameters.ChannelConfig.MaxAcceptedHtlcs;

            ChannelCandidate channelCandidate = new()
            {
                ChannelOpener = ChannelSide.Remote,
                ChannelId = openChannel.TemporaryChannelId,
                OpenChannel = openChannel,
                AcceptChannel = acceptChannel,
                LocalUpfrontShutdownScript = localUpfrontShutdownScript,
                RemoteUpfrontShutdownScript = remoteUpfrontShutdownScript,
            };

            await _channelStateRepository.CreateAsync(channelCandidate);

            var boltMessage = new BoltMessage
            {
                Payload = acceptChannel,
                Extension = new TlVStream
                {
                    Records = new List<TlvRecord>
                    {
                        new UpfrontShutdownScriptTlvRecord {ShutdownScriptpubkey = localUpfrontShutdownScript}
                    }
                }
            };

            return new MessageProcessingOutput { Success = true, ResponseMessages = new[] { boltMessage } };
        }

        private string CheckMessage(OpenChannel openChannel, ChainParameters chainParameters, bool optionAnchorOutputs)
        {
            if (!chainParameters.ChannelBoundariesConfig.AllowPrivateChannels && ((ChannelFlags.ChannelFlags)openChannel.ChannelFlags & ChannelFlags.ChannelFlags.AnnounceChannel) == 0) return "private channels not supported";

            Satoshis localReserve = (ulong)(chainParameters.ChannelBoundariesConfig.ChannelReservePercentage * (ulong)openChannel.FundingSatoshis);
            Satoshis remoteReserve = openChannel.ChannelReserveSatoshis;
            Satoshis totalReserve = remoteReserve + localReserve;
            if (optionAnchorOutputs) totalReserve += 666;

            Satoshis capacity = openChannel.FundingSatoshis;
            if (capacity < totalReserve) return "channel_reserve_satoshis too large";
            capacity = openChannel.FundingSatoshis - totalReserve;

            Satoshis baseFee = _lightningTransactions.GetBaseFee(openChannel.FeeratePerKw, optionAnchorOutputs, 0);
            if (capacity < baseFee) return "the funder's amount for the initial commitment transaction is not sufficient for full fee payment.";
            capacity -= baseFee;

            Satoshis maxHtlcValueInFlightSat = (Satoshis)chainParameters.ChannelConfig.MaxHtlcValueInFlight;
            if (capacity > maxHtlcValueInFlightSat) // the other side capped the capacity
                capacity = maxHtlcValueInFlightSat;

            Satoshis htlcMinimumSat = (Satoshis)openChannel.HtlcMinimumMsat;
            if (capacity < htlcMinimumSat) return "htlc_minimum_msat too large"; // If the minimum htlc is greater than the capacity, the channel is usless

            if (capacity < chainParameters.ChannelBoundariesConfig.MinEffectiveHtlcCapacity) return "funding_satoshis is too small";

            if (openChannel.MaxAcceptedHtlcs == 0) return "max_accepted_htlcs too small";
            if (openChannel.MaxAcceptedHtlcs > 483) return "max_accepted_htlcs is greater than 483";

            if (openChannel.DustLimitSatoshis > openChannel.ChannelReserveSatoshis) return "dust_limit_satoshis is greater than channel_reserve_satoshis";
            if (openChannel.DustLimitSatoshis > localReserve) return "dust_limit_satoshis is greater than our channel_reserve_satoshis";

            if (openChannel.DustLimitSatoshis < chainParameters.ChannelConfig.DustLimit) return "dust_limit_satoshis too small";

            // todo: dan - investigate this logic - bolt2 openchannel received side -
            // it considers dust_limit_satoshis too small and plans to rely on the sending node publishing its commitment transaction in the event of a data loss

            MiliSatoshis fundingMiliSatoshis = openChannel.FundingSatoshis;
            if (openChannel.PushMsat > fundingMiliSatoshis) return "push_msat is greater than funding_satoshis * 1000";
            if (openChannel.ToSelfDelay > chainParameters.ChannelBoundariesConfig.MaxToSelfDelay) return "to_self_delay is unreasonably large";

            if (openChannel.FeeratePerKw < chainParameters.ChannelBoundariesConfig.TooLowFeeratePerKw) return "feerate_per_kw too small for timely processing";
            if (openChannel.FeeratePerKw > chainParameters.ChannelBoundariesConfig.TooLargeFeeratePerKw) return "feerate_per_kw unreasonably large";

            if (!_lightningKeyDerivation.IsValidPublicKey(openChannel.FundingPubkey)) return "funding_pubkey not valid secp256k1 pubkeys in compressed format";
            if (!_lightningKeyDerivation.IsValidPublicKey(openChannel.RevocationBasepoint)) return "revocation_basepoint not valid secp256k1 pubkeys in compressed format";
            if (!_lightningKeyDerivation.IsValidPublicKey(openChannel.HtlcBasepoint)) return "htlc_basepoint not valid secp256k1 pubkeys in compressed format";
            if (!_lightningKeyDerivation.IsValidPublicKey(openChannel.PaymentBasepoint)) return "payment_basepoint not valid secp256k1 pubkeys in compressed format";
            if (!_lightningKeyDerivation.IsValidPublicKey(openChannel.DelayedPaymentBasepoint)) return "delayed_payment_basepoint not valid secp256k1 pubkeys in compressed format";

            if ((openChannel.FundingSatoshis > chainParameters.ChannelBoundariesConfig.LargeChannelAmount)
                && _boltFeatures.SupportedFeatures != Features.OptionSupportLargeChannel) return "funding_satoshis too big for option_support_large_channel";

            return string.Empty;
        }
    }
}