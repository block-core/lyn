using Lyn.Types.Bitcoin;

namespace Lyn.Types.Bolt
{
    public class ChainHash : UInt256
    {
        public ChainHash(byte[] bytes) : base(bytes){ }
    }
}