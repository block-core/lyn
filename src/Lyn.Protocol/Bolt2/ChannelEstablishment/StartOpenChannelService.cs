﻿using Lyn.Protocol.Bolt1;
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
        private readonly IChannelStateRepository _channelStateRepository;
        private readonly IPeerRepository _peerRepository;
        private readonly IChainConfigProvider _chainConfigProvider;
        private readonly IChannelConfigProvider _channelConfigProvider;
        private readonly IBoltFeatures _boltFeatures;
        private readonly IParseFeatureFlags _parseFeatureFlags;
        private readonly ISecretProvider _secretProvider;
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
            ISecretProvider secretProvider)
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
            _secretProvider = secretProvider;
            _messageSender = messageSender;
        }

        public async Task StartOpenChannelAsync(StartOpenChannelIn startOpenChannelIn)
        {
            Peer peer = _peerRepository.GetPeer(startOpenChannelIn.NodeId);

            if (peer == null)
                throw new ApplicationException($"Peer was not found or is not connected");

            ChainParameters? chainParameters = _chainConfigProvider.GetConfiguration(startOpenChannelIn.ChainHash);

            if (chainParameters == null)
                throw new ApplicationException($"Invalid chain hash");

            ChannelConfig? channelConfig = _channelConfigProvider.GetConfiguration(startOpenChannelIn.ChainHash);

            if (channelConfig == null)
                throw new ApplicationException($"Invalid chain hash");

            OpenChannel openChannel = new();

            // Bolt 2 - MUST ensure the chain_hash value identifies the chain it wishes to open the channel within.
            openChannel.ChainHash = chainParameters.GenesisBlockhash;

            // Bolt 2 - MUST ensure temporary_channel_id is unique from any other channel ID with the same peer
            openChannel.TemporaryChannelId = new ChannelId(_randomNumberGenerator.GetBytes(32));

            // Bolt 2 -
            // if both nodes advertised option_support_large_channel:
            // MAY set funding_satoshis greater than or equal to 2 ^ 24 satoshi.
            //    otherwise:
            // MUST set funding_satoshis to less than 2 ^ 24 satoshi.
            bool localSupportLargChannels = _boltFeatures.SupportedFeatures == Features.OptionSupportLargeChannel;
            bool remoteSupportLargChannels = _parseFeatureFlags.ParseFeatures(peer.Featurs) == Features.OptionSupportLargeChannel;
            if (localSupportLargChannels == false || remoteSupportLargChannels == false)
            {
                if (startOpenChannelIn.FundingAmount > chainParameters.LargeChannelAmount) // 2^24
                    throw new ApplicationException($"Peer enforces max channel capacity of {chainParameters.LargeChannelAmount}sats");
            }

            openChannel.FundingSatoshis = startOpenChannelIn.FundingAmount;

            // Bolt 2 - MUST set push_msat to equal or less than 1000 * funding_satoshis.
            MiliSatoshis fundingMiliSatoshis = startOpenChannelIn.FundingAmount;
            if (startOpenChannelIn.PushOnOpen > fundingMiliSatoshis)
                throw new ApplicationException($"Not enough capacity to pay peer {startOpenChannelIn.PushOnOpen}msat on opening of the channel with capacity {fundingMiliSatoshis}msat");

            openChannel.PushMsat = startOpenChannelIn.PushOnOpen;

            Secret seed = _secretProvider.GetSeed();
            Secrets secrets = _lightningKeyDerivation.DeriveSecrets(seed);

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

            // Bolt 2 - MUST set channel_reserve_satoshis greater than or equal to dust_limit_satoshis.
            if (channelConfig.ChannelReserve < channelConfig.DustLimit)
                throw new ApplicationException($"ChannelReserve = {channelConfig.ChannelReserve}sat must be greater then DustLimit = {channelConfig.DustLimit}sat");

            openChannel.ChannelReserveSatoshis = channelConfig.ChannelReserve;

            // Bolt 2 - MUST set undefined bits in channel_flags to 0.
            openChannel.ChannelFlags = startOpenChannelIn.PrivateChannel ? (byte)0 : (byte)ChannelFlags.ChannelFlags.AnnounceChannel;

            // Bolt 2 - set to_self_delay sufficient to ensure the sender can irreversibly spend a commitment transaction output, in case of misbehavior by the receiver.
            openChannel.ToSelfDelay = channelConfig.ToSelfDelay;

            // Bolt 2 - set feerate_per_kw to at least the rate it estimates would cause the transaction to be immediately included in a block.
            openChannel.FeeratePerKw = startOpenChannelIn.FeeRate;

            // Bolt 2 - set dust_limit_satoshis to a sufficient value to allow commitment transactions to propagate through the Bitcoin network.
            openChannel.DustLimitSatoshis = channelConfig.DustLimit;

            // Bolt 2 - set htlc_minimum_msat to the minimum value HTLC it's willing to accept from this peer.
            openChannel.HtlcMinimumMsat = channelConfig.HtlcMinimum;

            // Bolt 2 - if both nodes advertised the option_upfront_shutdown_script feature:
            // MUST include upfront_shutdown_script with either a valid shutdown_scriptpubkey as required by shutdown scriptpubkey, or a zero - length shutdown_scriptpubkey(ie. 0x0000).
            //    otherwise:
            // MAY include upfront_shutdown_script.
            //    if it includes open_channel_tlvs:
            // MUST include upfront_shutdown_script.

            // todo: dan create the shutdown_scriptpubkey once tlv is done.

            openChannel.Extension = new TlVStream
            {
                Records = new List<TlvRecord>
                {
                }
            };

            // Create the channel durable state
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

            await _messageSender.SendMessageAsync(new PeerMessage<OpenChannel>(startOpenChannelIn.NodeId, openChannel));
        }
    }
}