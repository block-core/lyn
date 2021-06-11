using System;

namespace Lyn.Protocol.Bolt8
{
   public interface IKeyGenerator
   {
      byte[] GenerateKey();
      ReadOnlySpan<byte> GetPublicKey(byte[] privateKey);
   }
}