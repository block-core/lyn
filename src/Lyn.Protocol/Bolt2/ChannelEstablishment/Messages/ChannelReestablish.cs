using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment.Messages
{
    public class ChannelReestablish : MessagePayload
    {
        public ChannelReestablish(UInt256 channelId,ulong nextCommitmentNumber,ulong nextRevocationNumber, PublicKey lastPerCommitmentSecret, PublicKey currentPerCommitmentPoint)
        {
            ChannelId = channelId;
            NextCommitmentNumber = nextCommitmentNumber;
            NextRevocationNumber = nextRevocationNumber;
            LastPerCommitmentSecret = lastPerCommitmentSecret;
            CurrentPerCommitmentPoint = currentPerCommitmentPoint;
        }

        public override MessageType MessageType => MessageType.ChannelReestablish;


        public UInt256 ChannelId { get; set; }

        public ulong NextCommitmentNumber { get; set; }

        public ulong NextRevocationNumber { get; set; }

        public PublicKey LastPerCommitmentSecret { get; set; }

        public PublicKey CurrentPerCommitmentPoint { get; set; }

    }
}