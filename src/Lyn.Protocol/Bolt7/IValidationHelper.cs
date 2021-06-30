using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt7
{
    public interface IValidationHelper
    {
        bool VerifySignature(PublicKey publicKey, CompressedSignature signature, UInt256 doubleHash);

        bool VerifyPublicKey(PublicKey publicKey);
    }
}