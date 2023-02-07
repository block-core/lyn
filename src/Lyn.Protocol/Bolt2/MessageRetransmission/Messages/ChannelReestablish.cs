using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.MessageRetransmission.Messages
{
    public class ChannelReestablish : MessagePayload
    {
        public ChannelReestablish(){ }
        
        public ChannelReestablish(UInt256 channelId, ulong nextCommitmentNumber, ulong nextRevocationNumber, 
            Secret yourLastPerCommitmentSecret, PublicKey myCurrentPerCommitmentPoint)
        {
            ChannelId = channelId;
            NextCommitmentNumber = nextCommitmentNumber;
            NextRevocationNumber = nextRevocationNumber;
            YourLastPerCommitmentSecret = yourLastPerCommitmentSecret;
            MyCurrentPerCommitmentPoint = myCurrentPerCommitmentPoint;
        }

        public override MessageType MessageType => MessageType.ChannelReestablish;
        public UInt256 ChannelId { get; set; }
        public ulong NextCommitmentNumber { get; set; }
        public ulong NextRevocationNumber { get; set; }
        public Secret YourLastPerCommitmentSecret { get; set; }
        public PublicKey MyCurrentPerCommitmentPoint { get; set; }
    }
}