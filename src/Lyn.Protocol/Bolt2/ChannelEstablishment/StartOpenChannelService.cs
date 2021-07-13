using Lyn.Protocol.Bolt1;
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
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Entities;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages.TlvRecords;
using Lyn.Protocol.Common.Messages;

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
        private readonly IChannelConfigProvider _channelConfigProvider;
        private readonly IBoltFeatures _boltFeatures;
        private readonly IParseFeatureFlags _parseFeatureFlags;
        private readonly ISecretStore _secretStore;

        public StartOpenChannelService(ILogger<OpenChannelMessageService> logger,
            IRandomNumberGenerator randomNumberGenerator,
            ILightningKeyDerivation lightningKeyDerivation,
            IChannelCandidateRepository channelStateRepository,
            IPeerRepository peerRepository,
            IChainConfigProvider chainConfigProvider,
            IChannelConfigProvider channelConfigProvider,
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
            _channelConfigProvider = channelConfigProvider;
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

            ChannelConfig? channelConfig = _channelConfigProvider.GetConfiguration(createOpenChannelIn.ChainHash);

            if (channelConfig == null)
                throw new ApplicationException($"Invalid chain hash");

            OpenChannel openChannel = new();

            openChannel.ChainHash = chainParameters.GenesisBlockhash;

            openChannel.TemporaryChannelId = new ChannelId(_randomNumberGenerator.GetBytes(32));

            if (createOpenChannelIn.PrivateChannel && !chainParameters.AllowPrivateChannels)
                throw new ApplicationException($"Private channels are not enabled");

            bool localSupportLargeChannels = (_boltFeatures.SupportedFeatures & Features.OptionSupportLargeChannel) != 0;
            bool remoteSupportLargeChannels = (peer.Featurs & Features.OptionSupportLargeChannel) != 0;

            if (localSupportLargeChannels == false || remoteSupportLargeChannels == false)
            {
                if (createOpenChannelIn.FundingAmount > chainParameters.LargeChannelAmount) // 2^24
                    throw new ApplicationException($"Peer enforces max channel capacity of {chainParameters.LargeChannelAmount}sats");
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

            Satoshis localReserve = (ulong)(chainParameters.ChannelReservePercentage * (ulong)openChannel.FundingSatoshis);
            openChannel.ChannelReserveSatoshis = localReserve;

            if (channelConfig.ChannelReserve < channelConfig.DustLimit)
                channelConfig.ChannelReserve = channelConfig.DustLimit;

            openChannel.ChannelFlags = createOpenChannelIn.PrivateChannel ? (byte)0 : (byte)ChannelFlags.ChannelFlags.AnnounceChannel;
            openChannel.ToSelfDelay = channelConfig.ToSelfDelay;
            openChannel.FeeratePerKw = createOpenChannelIn.FeeRate;
            openChannel.DustLimitSatoshis = channelConfig.DustLimit;
            openChannel.HtlcMinimumMsat = chainParameters.MinEffectiveHtlcCapacity;
            openChannel.MaxHtlcValueInFlightMsat = channelConfig.MaxHtlcValueInFlight;
            openChannel.MaxAcceptedHtlcs = channelConfig.MaxAcceptedHtlcs;

            byte[]? upfrontShutdownScript = channelConfig.UpfrontShutdownScript;
            bool localSupportUpfrontShutdownScript = (_boltFeatures.SupportedFeatures & Features.OptionUpfrontShutdownScript) != 0;
            bool remoteSupportUpfrontShutdownScript = (peer.Featurs & Features.OptionUpfrontShutdownScript) != 0;

            if (localSupportUpfrontShutdownScript && remoteSupportUpfrontShutdownScript)
            {
                if (upfrontShutdownScript == null || upfrontShutdownScript.Length == 0)
                    upfrontShutdownScript = new byte[] { 0x0000 };
            }

            var boltMessage = new BoltMessage
            {
                Payload = openChannel,
                Extension = new TlVStream
                {
                    Records = new List<TlvRecord>
                    {
                        new UpfrontShutdownScriptTlvRecord{ ShutdownScriptpubkey = upfrontShutdownScript}
                    }
                }
            };

            ChannelCandidate channelCandidate = new()
            {
                ChannelOpener = ChannelSide.Local,
                ChannelId = openChannel.TemporaryChannelId,
                OpenChannel = openChannel,
                LocalUpfrontShutdownScript = upfrontShutdownScript
            };

            await _channelStateRepository.CreateAsync(channelCandidate);

            return boltMessage;
        }
    }
}