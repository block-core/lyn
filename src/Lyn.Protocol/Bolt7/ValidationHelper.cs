using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;
using NBitcoin;
using NBitcoin.Crypto;

namespace Lyn.Protocol.Bolt7
{
   public class ValidationHelper : IValidationHelper 
   {
      public bool VerifySignature(PublicKey publicKey, CompressedSignature signature, UInt256 doubleHash)
      {
         var keyVerifier = new PubKey(publicKey);

         return !ECDSASignature.TryParseFromCompact(signature, out var ecdsaSignature) || 
                keyVerifier.Verify(new uint256(doubleHash.GetBytes()), ecdsaSignature);
      }

      public bool VerifyPublicKey(PublicKey publicKey)
      {
         return PubKey.Check(publicKey, true);
      }
   }
}