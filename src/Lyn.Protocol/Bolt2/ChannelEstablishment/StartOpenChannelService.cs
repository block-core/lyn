using System;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Entities;
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
    public class StartOpenChannelService : IStartOpenChannelService
    {
        private readonly ILogger<OpenChannelMessageService> _logger;
        private readonly IBoltValidationService<OpenChannel> _validationService;
        private readonly ILightningTransactions _lightningTransactions;
        private readonly IRandomNumberGenerator _randomNumberGenerator;
        private readonly ILightningKeyDerivation _lightningKeyDerivation;
        private readonly IChannelStateRepository _channelStateRepository;
        private readonly IPeerRepository _peerRepository;
        private readonly IBoltMessageSender<OpenChannel> _messageSender;

        public StartOpenChannelService(ILogger<OpenChannelMessageService> logger,
            IBoltValidationService<OpenChannel> validationService,
            IBoltMessageSender<OpenChannel> messageSender,
            ILightningTransactions lightningTransactions,
            IRandomNumberGenerator randomNumberGenerator,
            ILightningKeyDerivation lightningKeyDerivation,
            IChannelStateRepository channelStateRepository,
            IPeerRepository peerRepository)
        {
            _logger = logger;
            _validationService = validationService;
            _lightningTransactions = lightningTransactions;
            _randomNumberGenerator = randomNumberGenerator;
            _lightningKeyDerivation = lightningKeyDerivation;
            _channelStateRepository = channelStateRepository;
            _peerRepository = peerRepository;
            _messageSender = messageSender;
        }

        public async Task StartOpenChannel(
            PublicKey nodeId,
            ChainParameters chainParameters,
            Satoshis fundingAmount,
            MiliSatoshis pushOnOpen,
            Satoshis channelReserveSatoshis)
        {
            Peer peer = _peerRepository.GetPeer(nodeId);

            if (peer == null)
                throw new ApplicationException($"Peer was not founds or is not connected");

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

            // todo: check `option_support_large_channel` in features
            bool optionSupportLargeChannel = false;
            const ulong largeChannelAmount = 16_777_216; // todo: dan move this to configuration
            if (!optionSupportLargeChannel)
            {
                if (fundingAmount > largeChannelAmount) // 2^24
                    throw new ApplicationException($"Peer enforces max channel capacity of {largeChannelAmount}sats");
            }

            openChannel.FundingSatoshis = fundingAmount;

            // Bolt 2 - MUST set push_msat to equal or less than 1000 * funding_satoshis.

            MiliSatoshis fundingMiliSatoshis = fundingAmount;
            if (pushOnOpen > fundingMiliSatoshis)
                throw new ApplicationException($"Not enough capacity to pay peer {pushOnOpen}msat on opening of the channel with capacity {fundingMiliSatoshis}msat");

            openChannel.PushMsat = pushOnOpen;

            Secrets secrets = _lightningKeyDerivation.DeriveSecrets(null); // todo: dan create seed store

            openChannel.FundingPubkey = _lightningKeyDerivation.PublicKeyFromPrivateKey(secrets.FundingPrivkey);

            // MUST set funding_pubkey, revocation_basepoint, htlc_basepoint, payment_basepoint,
            // and delayed_payment_basepoint to valid secp256k1 pubkeys in compressed format

            Basepoints basepoints = _lightningKeyDerivation.DeriveBasepoints(secrets);

            openChannel.RevocationBasepoint = basepoints.Revocation;
            openChannel.HtlcBasepoint = basepoints.Htlc;
            openChannel.PaymentBasepoint = basepoints.Payment;
            openChannel.DelayedPaymentBasepoint = basepoints.DelayedPayment;

            // MUST set first_per_commitment_point to the per-commitment point to be used for the initial commitment transaction, derived as specified in BOLT #3.

            openChannel.FirstPerCommitmentPoint = _lightningKeyDerivation.PerCommitmentPoint(secrets.Shaseed, 0);

            // MUST set channel_reserve_satoshis greater than or equal to dust_limit_satoshis.

            openChannel.ChannelReserveSatoshis = channelReserveSatoshis < chainParameters.DustLimit
                ? chainParameters.DustLimit
                : channelReserveSatoshis;

            // MUST set undefined bits in channel_flags to 0.

            openChannel.ChannelFlags = 0; // todo: dan fix once bolt9 is done

            // if both nodes advertised the option_upfront_shutdown_script feature:
            // MUST include upfront_shutdown_script with either a valid shutdown_scriptpubkey as required by shutdown scriptpubkey, or a zero - length shutdown_scriptpubkey(ie. 0x0000).
            //    otherwise:
            // MAY include upfront_shutdown_script.
            // if it includes open_channel_tlvs:
            // MUST include upfront_shutdown_script.

            // todo: dan create the shutdown_scriptpubkey once tlv is done.

            // Create the channel durable state
            ChannelState channelState = new()
            {
                Funding = openChannel.FundingSatoshis,
                ChannelId = openChannel.TemporaryChannelId,
                LocalPublicKey = openChannel.FundingPubkey,
                LocalPoints = basepoints,
                PushMsat = openChannel.PushMsat,
                LocalFirstPerCommitmentPoint = openChannel.FirstPerCommitmentPoint,
            };

            _channelStateRepository.AddOrUpdate(channelState);

            await _messageSender.SendMessageAsync(new PeerMessage<OpenChannel> { Message = openChannel, NodeId = nodeId });
        }
    }
}