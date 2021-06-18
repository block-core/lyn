using System;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.Messags;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Protocol.Connection;
using Lyn.Types;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2
{
    public class OpenChannelService : IBoltMessageService<OpenChannel>
    {
        private readonly ILogger<OpenChannelService> _logger;
        private readonly ILightningTransactions _lightningTransactions;
        private readonly IRandomNumberGenerator _randomNumberGenerator;

        public OpenChannelService(ILogger<OpenChannelService> logger,
            ILightningTransactions lightningTransactions,
            IRandomNumberGenerator randomNumberGenerator)
        {
            _logger = logger;
            _lightningTransactions = lightningTransactions;
            _randomNumberGenerator = randomNumberGenerator;
        }

        public Task ProcessMessageAsync(PeerMessage<OpenChannel> message)
        {
            OpenChannel openChannel = message.Message;

            //The receiving node MUST:

            //ignore undefined bits in channel_flags.
            // openChannel.ChannelFlags

            //if the connection has been re-established after receiving a previous open_channel, BUT before receiving a funding_created message:
            //accept a new open_channel message.
            //    discard the previous open_channel message.
            //    The receiving node MAY fail the channel if:

            //announce_channel is false(0), yet it wishes to publicly announce the channel.
            //    funding_satoshis is too small.
            //it considers htlc_minimum_msat too large.
            //    it considers max_htlc_value_in_flight_msat too small.
            //    it considers channel_reserve_satoshis too large.
            //    it considers max_accepted_htlcs too small.
            //    it considers dust_limit_satoshis too small and plans to rely on the sending node publishing its commitment transaction in the event of a data loss(see message-retransmission).
            //The receiving node MUST fail the channel if:

            //the chain_hash value is set to a hash of a chain that is unknown to the receiver.
            //    push_msat is greater than funding_satoshis* 1000.
            //    to_self_delay is unreasonably large.
            //max_accepted_htlcs is greater than 483.
            //    it considers feerate_per_kw too small for timely processing or unreasonably large.
            //    funding_pubkey, revocation_basepoint, htlc_basepoint, payment_basepoint, or delayed_payment_basepoint are not valid secp256k1 pubkeys in compressed format.
            //    dust_limit_satoshis is greater than channel_reserve_satoshis.
            //    the funder's amount for the initial commitment transaction is not sufficient for full fee payment.
            //    both to_local and to_remote amounts for the initial commitment transaction are less than or equal to channel_reserve_satoshis (see BOLT 3).
            //    funding_satoshis is greater than or equal to 2^24 and the receiver does not support option_support_large_channel.
            //    The receiving node MUST NOT:

            //consider funds received, using push_msat, to be received until the funding transaction has reached sufficient depth.

            return Task.CompletedTask;
        }

        public void StartOpenChannel(
            ChainParameters chainParameters,
            Satoshis fundingAmount)
        {
            OpenChannel openChannel = new OpenChannel();

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

            openChannel.FundingSatoshis = fundingAmount;

            // Bolt 2 - MUST set push_msat to equal or less than 1000 * funding_satoshis.
            openChannel.PushMsat = openChannel.FundingSatoshis;

            openChannel.FundingPubkey = null;
            openChannel.RevocationBasepoint = null;
            openChannel.HtlcBasepoint = null;
            openChannel.PaymentBasepoint = null;
            openChannel.DelayedPaymentBasepoint = null;
        }
    }
}