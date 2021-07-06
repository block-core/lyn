using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt3.Shachain
{
    public class ShachainItem
    {
        public ShachainItem(UInt256 secret, ulong index)
        {
            Secret = secret;
            Index = index;
        }

        public ulong Index { get; set; }
        public UInt256 Secret { get; set; }
    }
}