using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public interface ICommitmentTransactionBuilder
    {
        ICommitmentTransactionBuilder WithOpenChannel(OpenChannel openChannel);
        ICommitmentTransactionBuilder WithAcceptChannel(AcceptChannel acceptChannel);
        ICommitmentTransactionBuilder WithAnchorOutputs();
        ICommitmentTransactionBuilder WithStaticRemoteKey();
        ICommitmentTransactionBuilder WithFundingSide(ChannelSide side);
        ICommitmentTransactionBuilder WithFundingOutpoint(OutPoint point);
        CommitmenTransactionOut BuildRemoteCommitmentTransaction();
        CommitmenTransactionOut BuildLocalCommitmentTransaction();
    }
}