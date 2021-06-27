using Lyn.Protocol.Common.Blockchain;
using Lyn.Types.Fundamental;
using System.Threading.Tasks;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public class StartOpenChannelIn
    {
        public StartOpenChannelIn(PublicKey nodeId, UInt256 chainHash, Satoshis fundingAmount, MiliSatoshis pushOnOpen, Satoshis feeRate, bool privateChannel)
        {
            NodeId = nodeId;
            ChainHash = chainHash;
            FundingAmount = fundingAmount;
            PushOnOpen = pushOnOpen;
            FeeRate = feeRate;
            PrivateChannel = privateChannel;
        }

        public PublicKey NodeId { get; private set; }
        public UInt256 ChainHash { get; private set; }
        public Satoshis FundingAmount { get; private set; }
        public MiliSatoshis PushOnOpen { get; private set; }
        public Satoshis FeeRate { get; private set; }
        public bool PrivateChannel { get; set; }
    }

    public interface IStartOpenChannelService
    {
        Task StartOpenChannelAsync(StartOpenChannelIn startOpenChannelIn);
    }
}