using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.Entities
{
    public class CloseChannelDetails
    {
        public Satoshis? FeeSatoshis { get; set; }
        public ulong MinFeeRange { get; set; }
        public ulong MaxFeeRange { get; set; }
        public CompressedSignature RemoteNodeScriptPubKeySignature { get; set; }
    }
}