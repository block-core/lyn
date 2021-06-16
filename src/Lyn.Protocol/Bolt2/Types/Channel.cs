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
        public ChannelId? ChannelId { get; set; }

        public OutPoint? FundingOutpoint { get; set; }

        /* Keys used to spend funding tx. */
        public PublicKey? FundingPubkey { get; set; }

        /* satoshis in from commitment tx */
        public Satoshis Funding { get; set; }

        /* confirmations needed for locking funding */
        public uint MinimumDepth { get; set; }

        /* Who is paying fees. */
        public ChannelSide Opener { get; set; }

        /* Limits and settings on this channel. */
        public ChannelConfig Config { get; set; }

        /* Basepoints for deriving keys. */

        public PublicKey Basepoints { get; set; }

        /* Mask for obscuring the encoding of the commitment number. */
        public ulong CommitmentNumberObscurer { get; set; }

        /* All live HTLCs for this channel */
        public Dictionary<OutPoint, Htlc> Htlcs { get; set; }

        /* Fee changes, some which may be in transit */
        public Satoshis FeeStates { get; set; }

        /* What it looks like to each side. */
        public ChannelView ChannelView { get; set; }

        /* Is this using option_static_remotekey? */
        public bool OptionStaticRemotekey { get; set; }

        /* Is this using option_anchor_outputs? */
        public bool OptionAnchorOutputs { get; set; }
    }
}