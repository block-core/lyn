using System;
using System.Collections.Generic;
using System.Linq;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;
using ChannelConfig = Lyn.Protocol.Bolt2.Configuration.ChannelConfig;

namespace Lyn.Protocol.Bolt2.Entities
{
    /// <summary>
    /// A durable state holder for channel related information.
    /// </summary>
    public class ChannelState
    {
        /// <summary>
        /// The id for this channel.
        /// </summary>
        public ChannelId? ChannelId { get; set; }

        public Channel Channel { get; set; }

        /// <summary>
        /// Local configuration.
        /// </summary>
        public ChannelConfig LocalConfig { get; set; }

        /// <summary>
        /// Remote configuration.
        /// </summary>
        public ChannelConfig RemoteConfig { get; set; }

        /// <summary>
        /// The remote offered featured.
        /// </summary>
        public byte LocalFeatures { get; set; }

        /// <summary>
        /// The remote offered featured.
        /// </summary>
        public ulong RemoteFeatures { get; set; }

        /// <summary>
        /// Our local base points needed for channel establishment.
        /// </summary>
        public Basepoints LocalPoints { get; set; }

        /// <summary>
        /// The local public key in the 2-of-2 multisig script of the funding transaction output.
        /// </summary>
        public PublicKey LocalPublicKey { get; set; }

        /// <summary>
        /// The remote base points needed for channel establishment.
        /// </summary>
        public Basepoints RemotePoints { get; set; }

        /// <summary>
        /// The remote public key in the 2-of-2 multisig script of the funding transaction output.
        /// </summary>
        public PublicKey RemotePublicKey { get; set; }

        /// <summary>
        /// The local first commitment point used to derive commitment public keys.
        /// </summary>
        public PublicKey LocalFirstPerCommitmentPoint { get; set; }

        /// <summary>
        /// The remote first commitment point used to derive commitment public keys.
        /// </summary>
        public PublicKey RemoteFirstPerCommitmentPoint { get; set; }

        /// <summary>
        /// The amount the funder is putting into the channel.
        /// </summary>
        public Satoshis Funding { get; set; }

        /// <summary>
        /// An amount of initial funds that the sender is unconditionally giving to the receiver.
        /// </summary>
        public MiliSatoshis PushMsat { get; set; }

        /// <summary>
        /// indicates the initial fee rate in satoshi per 1000-weight (i.e. 1/4 the more normally-used 'satoshi per 1000 vbytes')
        /// that this side will pay for commitment and HTLC transactions, as described in BOLT #3 (this can be adjusted later with an update_fee message).
        /// </summary>
        public Satoshis FeeratePerKw { get; set; }

        /// <summary>
        /// The outpoint of the funding transaction.
        /// </summary>
        public OutPoint FundingOutpoint { get; set; }

        /// <summary>
        /// Allows a node to commit to where funds will go on mutual close,
        /// which the node should enforce even if a node is compromised later.
        /// </summary>
        public byte[] LocalUpfrontShutdownScript { get; set; }

        /// <summary>
        /// Allows a node to commit to where funds will go on mutual close,
        /// which the node should enforce even if a node is compromised later.
        /// </summary>
        public byte[] RemoteUpfrontShutdownScript { get; set; }

        public bool OptionStaticRemotekey { get; set; }
        public bool OptionAnchorOutputs { get; set; }
    }
}