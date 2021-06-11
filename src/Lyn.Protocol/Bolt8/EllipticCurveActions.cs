using System;
using NBitcoin;

namespace Lyn.Protocol.Bolt8
{
   public class EllipticCurveActions : IEllipticCurveActions
   {
      public ReadOnlySpan<byte> Multiply(byte[] privateKey, ReadOnlySpan<byte> publicKey) 
         => new PubKey(publicKey.ToArray())
            .GetSharedSecret(new Key(privateKey));
   }
}