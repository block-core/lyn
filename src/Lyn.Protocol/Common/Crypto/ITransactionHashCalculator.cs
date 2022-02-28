using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Common.Crypto
{
    public interface ITransactionHashCalculator
    {
        UInt256 ComputeHash(Transaction transaction);

        UInt256 ComputeWitnessHash(Transaction transaction);
    }
}