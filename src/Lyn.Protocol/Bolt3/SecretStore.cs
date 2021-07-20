using Lyn.Types;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt3
{
    public class SecretStore : ISecretStore
    {
        public Secret GetSeed()
        {
            return new Secret(Hex.FromString("0x1111111111111111111111111111111111111111111111111111111111111111"));
        }
    }
}