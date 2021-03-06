using System;

namespace Lyn.Protocol.Bolt8
{
   public interface IEllipticCurveActions
   {
      ReadOnlySpan<byte> Multiply(byte[] privateKey, ReadOnlySpan<byte> publicKey);
   }
}