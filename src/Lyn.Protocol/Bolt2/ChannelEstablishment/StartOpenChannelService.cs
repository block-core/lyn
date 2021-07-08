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
using Lyn.Protocol.Common.Messages;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public class StartOpenChannelService : IStartOpenChannelService
    {
        private readonly ILogger<OpenChannelMessageService> _logger;
        private readonly IRandomNumberGenerator _randomNumberGenerator;
        private readonly ILightningKeyDerivation _lightningKeyDerivation;
        private readonly IChannelStateRepository _channelStateRepository;
        private readonly IPeerRepository _peerRepository;
        private readonly IChainConfigProvider _chainConfigProvider;
        private readonly IChannelConfigProvider _channelConfigProvider;
        private readonly IBoltFeatures _boltFeatures;
        private readonly IParseFeatureFlags _parseFeatureFlags;
        private readonly ISecretStore _secretStore;
        private readonly IBoltMessageSender<OpenChannel> _messageSender;

        public StartOpenChannelService(ILogger<OpenChannelMessageService> logger,
            IBoltMessageSender<OpenChannel> messageSender,
            IRandomNumberGenerator randomNumberGenerator,
            ILightningKeyDerivation lightningKeyDerivation,
            IChannelStateRepository channelStateRepository,
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
            _messageSender = messageSender;
        }

        public async Task StartOpenChannelAsync(StartOpenChannelIn startOpenChannelIn)
        {
            var peer = _peerRepository.TryGetPeerAsync(startOpenChannelIn.NodeId);

            if (peer == null)
                throw new ApplicationException($"Peer was not found or is not connected");

            ChainParameters? chainParameters = _chainConfigProvider.GetConfiguration(startOpenChannelIn.ChainHash);

            if (chainParameters == null)
                throw new ApplicationException($"Invalid chain hash");

            ChannelConfig? channelConfig = _channelConfigProvider.GetConfiguration(startOpenChannelIn.ChainHash);

            if (channelConfig == null)
                throw new ApplicationException($"Invalid chain hash");
            
            OpenChannel openChannel = new();

            openChannel.ChainHash = chainParameters.GenesisBlockhash;

            openChannel.TemporaryChannelId = new ChannelId(_randomNumberGenerator.GetBytes(32));

            bool localSupportLargeChannels = (_boltFeatures.SupportedFeatures & Features.OptionSupportLargeChannel) != 0;
            bool remoteSupportLargeChannels = (peer.Featurs & Features.OptionSupportLargeChannel) != 0;
            
            if (localSupportLargeChannels == false || remoteSupportLargeChannels == false)
            {
                if (startOpenChannelIn.FundingAmount > chainParameters.LargeChannelAmount) // 2^24
                    throw new ApplicationException($"Peer enforces max channel capacity of {chainParameters.LargeChannelAmount}sats");
            }

            openChannel.FundingSatoshis = startOpenChannelIn.FundingAmount;

            MiliSatoshis fundingMiliSatoshis = startOpenChannelIn.FundingAmount;
            if (startOpenChannelIn.PushOnOpen > fundingMiliSatoshis)
                throw new ApplicationException($"Not enough capacity to pay peer {startOpenChannelIn.PushOnOpen}msat on opening of the channel with capacity {fundingMiliSatoshis}msat");

            openChannel.PushMsat = startOpenChannelIn.PushOnOpen;

            Secret seed = _secretStore.GetSeed();
            Secrets secrets = _lightningKeyDerivation.DeriveSecrets(seed);

            openChannel.FundingPubkey = _lightningKeyDerivation.PublicKeyFromPrivateKey(secrets.FundingPrivkey);

            Basepoints basepoints = _lightningKeyDerivation.DeriveBasepoints(secrets);
            openChannel.RevocationBasepoint = basepoints.Revocation;
            openChannel.HtlcBasepoint = basepoints.Htlc;
            openChannel.PaymentBasepoint = basepoints.Payment;
            openChannel.DelayedPaymentBasepoint = basepoints.DelayedPayment;

            openChannel.FirstPerCommitmentPoint = _lightningKeyDerivation.PerCommitmentPoint(secrets.Shaseed, 0);

            if (channelConfig.ChannelReserve < channelConfig.DustLimit)
                throw new ApplicationException($"ChannelReserve = {channelConfig.ChannelReserve}sat must be greater then DustLimit = {channelConfig.DustLimit}sat");

            openChannel.ChannelReserveSatoshis = channelConfig.ChannelReserve;

            openChannel.ChannelFlags = startOpenChannelIn.PrivateChannel ? (byte)0 : (byte)ChannelFlags.ChannelFlags.AnnounceChannel;
            openChannel.ToSelfDelay = channelConfig.ToSelfDelay;
            openChannel.FeeratePerKw = startOpenChannelIn.FeeRate;
            openChannel.DustLimitSatoshis = channelConfig.DustLimit;
            openChannel.HtlcMinimumMsat = channelConfig.HtlcMinimum;

            // todo: dan create the shutdown_scriptpubkey once tlv is done.

            var boltMessage = new BoltMessage
            {
                Payload = openChannel,
                Extension = new TlVStream
                {
                    Records = new List<TlvRecord>
                    {
                    }
                }
            };
            
            ChannelState channelState = new()
            {
                FundingAmount = openChannel.FundingSatoshis,
                ChannelId = openChannel.TemporaryChannelId,
                LocalPublicKey = openChannel.FundingPubkey,
                LocalPoints = basepoints,
                PushMsat = openChannel.PushMsat,
                LocalFirstPerCommitmentPoint = openChannel.FirstPerCommitmentPoint,
                LocalConfig = channelConfig,
                FeeratePerKw = startOpenChannelIn.FeeRate,
            };

            _channelStateRepository.Create(channelState);

            await _messageSender.SendMessageAsync(new PeerMessage<OpenChannel>(startOpenChannelIn.NodeId, boltMessage));
        }
    }
}