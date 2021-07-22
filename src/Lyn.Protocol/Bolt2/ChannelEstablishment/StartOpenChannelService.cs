using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Entities;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages.TlvRecords;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public class StartOpenChannelService : IStartOpenChannelService
    {
        private readonly ILogger<OpenChannelMessageService> _logger;
        private readonly IRandomNumberGenerator _randomNumberGenerator;
        private readonly ILightningKeyDerivation _lightningKeyDerivation;
        private readonly IChannelCandidateRepository _channelStateRepository;
        private readonly IPeerRepository _peerRepository;
        private readonly IChainConfigProvider _chainConfigProvider;
        private readonly IBoltFeatures _boltFeatures;
        private readonly IParseFeatureFlags _parseFeatureFlags;
        private readonly ISecretStore _secretStore;

        public StartOpenChannelService(ILogger<OpenChannelMessageService> logger,
            IRandomNumberGenerator randomNumberGenerator,
            ILightningKeyDerivation lightningKeyDerivation,
            IChannelCandidateRepository channelStateRepository,
            IPeerRepository peerRepository,
            IChainConfigProvider chainConfigProvider,
            IBoltFeatures boltFeatures,
            IParseFeatureFlags parseFeatureFlags,
            ISecretStore secretStore)
        {
            _logger = logger;
            _randomNumberGenerator = randomNumberGenerator;
            _lightningKeyDerivation = lightningKeyDerivation;
            _channelStateRepository = channelStateRepository;
            _peerRepository = peerRepository;
            _chainConfigProvider = chainConfigProvider;
            _boltFeatures = boltFeatures;
            _parseFeatureFlags = parseFeatureFlags;
            _secretStore = secretStore;
        }

        public async Task<BoltMessage> CreateOpenChannelAsync(CreateOpenChannelIn createOpenChannelIn)
        {
            var peer = _peerRepository.TryGetPeerAsync(createOpenChannelIn.NodeId);

            if (peer == null)
                throw new ApplicationException($"Peer was not found or is not connected");

            ChainParameters? chainParameters = _chainConfigProvider.GetConfiguration(createOpenChannelIn.ChainHash);

            if (chainParameters == null)
                throw new ApplicationException($"Invalid chain hash");

            OpenChannel openChannel = new();

            openChannel.ChainHash = chainParameters.Chainhash;

            openChannel.TemporaryChannelId = new ChannelId(_randomNumberGenerator.GetBytes(32));

            if (createOpenChannelIn.PrivateChannel && !chainParameters.ChannelBoundariesConfig.AllowPrivateChannels)
                throw new ApplicationException($"Private channels are not enabled");

            bool localSupportLargeChannels = (_boltFeatures.SupportedFeatures & Features.OptionSupportLargeChannel) != 0;
            bool remoteSupportLargeChannels = (peer.Featurs & Features.OptionSupportLargeChannel) != 0;

            if (localSupportLargeChannels == false || remoteSupportLargeChannels == false)
            {
                if (createOpenChannelIn.FundingAmount > chainParameters.ChannelBoundariesConfig.LargeChannelAmount) // 2^24
                    throw new ApplicationException($"Peer enforces max channel capacity of {chainParameters.ChannelBoundariesConfig.LargeChannelAmount}sats");
            }

            openChannel.FundingSatoshis = createOpenChannelIn.FundingAmount;

            MiliSatoshis fundingMiliSatoshis = createOpenChannelIn.FundingAmount;
            if (createOpenChannelIn.PushOnOpen > fundingMiliSatoshis)
                throw new ApplicationException($"Not enough capacity to pay peer {createOpenChannelIn.PushOnOpen}msat on opening of the channel with capacity {fundingMiliSatoshis}msat");

            openChannel.PushMsat = createOpenChannelIn.PushOnOpen;

            Secret seed = _secretStore.GetSeed();
            Secrets secrets = _lightningKeyDerivation.DeriveSecrets(seed);

            openChannel.FundingPubkey = _lightningKeyDerivation.PublicKeyFromPrivateKey(secrets.FundingPrivkey);

            Basepoints basepoints = _lightningKeyDerivation.DeriveBasepoints(secrets);
            openChannel.RevocationBasepoint = basepoints.Revocation;
            openChannel.HtlcBasepoint = basepoints.Htlc;
            openChannel.PaymentBasepoint = basepoints.Payment;
            openChannel.DelayedPaymentBasepoint = basepoints.DelayedPayment;

            openChannel.FirstPerCommitmentPoint = _lightningKeyDerivation.PerCommitmentPoint(secrets.Shaseed, 0);

            Satoshis localReserve = (ulong)(chainParameters.ChannelBoundariesConfig.ChannelReservePercentage * (ulong)openChannel.FundingSatoshis);
            openChannel.ChannelReserveSatoshis = localReserve;

            if (chainParameters.ChannelConfig.ChannelReserve < chainParameters.ChannelConfig.DustLimit)
                chainParameters.ChannelConfig.ChannelReserve = chainParameters.ChannelConfig.DustLimit;

            openChannel.ChannelFlags = createOpenChannelIn.PrivateChannel ? (byte)0 : (byte)ChannelFlags.ChannelFlags.AnnounceChannel;
            openChannel.ToSelfDelay = chainParameters.ChannelConfig.ToSelfDelay;
            openChannel.FeeratePerKw = createOpenChannelIn.FeeRate;
            openChannel.DustLimitSatoshis = chainParameters.ChannelConfig.DustLimit;
            openChannel.HtlcMinimumMsat = chainParameters.ChannelBoundariesConfig.MinEffectiveHtlcCapacity;
            openChannel.MaxHtlcValueInFlightMsat = chainParameters.ChannelConfig.MaxHtlcValueInFlight;
            openChannel.MaxAcceptedHtlcs = chainParameters.ChannelConfig.MaxAcceptedHtlcs;

            byte[]? upfrontShutdownScript = chainParameters.ChannelConfig.UpfrontShutdownScript;
            bool localSupportUpfrontShutdownScript = (_boltFeatures.SupportedFeatures & Features.OptionUpfrontShutdownScript) != 0;
            bool remoteSupportUpfrontShutdownScript = (peer.Featurs & Features.OptionUpfrontShutdownScript) != 0;

            if (localSupportUpfrontShutdownScript && remoteSupportUpfrontShutdownScript)
            {
                if (upfrontShutdownScript == null || upfrontShutdownScript.Length == 0)
                    upfrontShutdownScript = new byte[] { 0x0000 };
            }

            var boltMessage = new BoltMessage {Payload = openChannel};

            if (upfrontShutdownScript != null)
            {
                boltMessage.Extension = new TlVStream
                {
                    Records = new List<TlvRecord>
                    {
                        new UpfrontShutdownScriptTlvRecord {ShutdownScriptpubkey = upfrontShutdownScript}
                    }
                };
            }
            
            ChannelCandidate channelCandidate = new()
            {
                ChannelOpener = ChannelSide.Local,
                ChannelId = openChannel.TemporaryChannelId,
                OpenChannel = openChannel,
                OpenChannelUpfrontShutdownScript = upfrontShutdownScript
            };

            await _channelStateRepository.CreateAsync(channelCandidate);

            return boltMessage;
        }
    }
}