using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Common
{
    public interface IValidationHelper
    {
        bool VerifySignature(PublicKey publicKey, CompressedSignature signature, UInt256 doubleHash);

        bool VerifyPublicKey(PublicKey publicKey);

        bool ValidateScriptPubKeyP2WSHOrP2WPKH(byte[] scriptPubKey);
    }
}