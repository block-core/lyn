using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Common.Blockchain
{
    public interface IFeeEstimation
    {
        Satoshis GetFeeRate(UInt256 chainid);

        Satoshis GetFee(UInt256 chainid, int virtualSize);
    }
}