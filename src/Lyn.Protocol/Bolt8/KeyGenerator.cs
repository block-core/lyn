using System;
using NBitcoin;

namespace Lyn.Protocol.Bolt8
{
   public class KeyGenerator : IKeyGenerator
   {
      public byte[] GenerateKey() => new Key().ToBytes();

      public ReadOnlySpan<byte> GetPublicKey(byte[] privateKey) => new Key(privateKey).PubKey.ToBytes();
   }
}