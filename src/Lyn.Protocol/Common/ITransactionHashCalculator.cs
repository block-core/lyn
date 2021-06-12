using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Common
{
    public interface ITransactionHashCalculator
    {
        UInt256 ComputeHash(Transaction transaction, int protocolVersion);

        UInt256 ComputeWitnessHash(Transaction transaction, int protocolVersion);
    }
}