using Lyn.Types;
using Lyn.Types.Fundamental;
using NBitcoin;
using Newtonsoft.Json.Serialization;

namespace Lyn.Protocol.Bolt3
{
    public class SecretStore : ISecretStore
    {
        public Secret GetSeed()
        {
            return new Secret(new Key().ToBytes()); //TODO Dan implement actual store
        }
    }
}