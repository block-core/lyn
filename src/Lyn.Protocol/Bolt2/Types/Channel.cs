using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.Types
{
    public class Channel
    {
        /// <summary>
        /// The id for this channel.
        /// </summary>
        public ChannelId? ChannelId { get; set; }

        /// <summary>
        /// Funding txid and output.
        /// </summary>

        public OutPoint? FundingOutpoint { get; set; }

        /// <summary>
        ///  Keys used to spend funding tx.
        /// </summary>
        public PublicKey? FundingPubkey { get; set; }

        /// <summary>
        ///  satoshis in from commitment tx
        /// </summary>
        public Satoshis? Funding { get; set; }

        /// <summary>
        ///  confirmations needed for locking funding
        /// </summary>
        public uint MinimumDepth { get; set; }

        /// <summary>
        ///  Who is paying fees.
        /// </summary>
        public ChannelSide Opener { get; set; }

        /// <summary>
        ///  Limits and settings on this channel.
        /// </summary>
        public ChannelConfig Config { get; set; }

        /// <summary>
        ///  Basepoints for deriving keys.
        /// </summary>

        public PublicKey Basepoints { get; set; }

        /// <summary>
        ///  Mask for obscuring the encoding of the commitment number.
        /// </summary>
        public ulong CommitmentNumberObscurer { get; set; }

        /// <summary>
        ///  All live HTLCs for this channel
        /// </summary>
        public Dictionary<OutPoint, Htlc> Htlcs { get; set; }

        /// <summary>
        ///  Fee changes, some which may be in transit
        /// </summary>
        public Satoshis FeeStates { get; set; }

        /// <summary>
        ///  What it looks like to each side.
        /// </summary>
        public ChannelView ChannelView { get; set; }

        /// <summary>
        ///  Is this using option_static_remotekey?
        /// </summary>
        public bool OptionStaticRemotekey { get; set; }

        /// <summary>
        ///  Is this using option_anchor_outputs?
        /// </summary>
        public bool OptionAnchorOutputs { get; set; }
    }
}