using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Common.Hashing
{
    public interface ITransactionHashCalculator
    {
        UInt256 ComputeHash(Transaction transaction);

        UInt256 ComputeWitnessHash(Transaction transaction);
    }
}