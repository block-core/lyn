using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt7
{
    public interface IValidationHelper
    {
        bool VerifySignature(PublicKey publicKey, CompressedSignature signature, byte[] doubleHash);

        bool VerifyPublicKey(PublicKey publicKey);
    }
}