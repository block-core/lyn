using Lyn.Types;
using Lyn.Types.Fundamental;
using NBitcoin;
using Newtonsoft.Json.Serialization;

namespace Lyn.Protocol.Bolt3
{
    public class SecretStore : ISecretStore
    {
        private Secret secret;

        public SecretStore()
        {
            //secret = new Secret(new Key().ToBytes());

            secret = new Secret(Hex.FromString("0x5e46094b865e688419c3bec96de09da2f1e40fd71f79588c34502a12332ef074"));
        }

        public Secret GetSeed()
        {
            return secret; //TODO Dan implement actual store
        }
    }
}