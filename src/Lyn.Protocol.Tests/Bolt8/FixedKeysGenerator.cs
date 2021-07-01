using System;
using System.Collections.Generic;
using Lyn.Protocol.Bolt8;
using Lyn.Types;

namespace Lyn.Protocol.Tests.Bolt8
{
   internal class FixedKeysGenerator : IKeyGenerator
   {
      readonly byte[] _privateKey;
      readonly Dictionary<string, byte[]> _keys;

      public FixedKeysGenerator(byte[] privateKey, byte[] publicKey)
      {
         _privateKey = privateKey;
         _keys = new Dictionary<string, byte[]> {{Hex.ToString(privateKey), publicKey}};
      }

      public FixedKeysGenerator AddKeys(byte[] privateKey, byte[] publicKey)
      {
         _keys.Add(Hex.ToString(privateKey),publicKey);
         return this;
      }

      public byte[] GenerateKey() => _privateKey;

      public ReadOnlySpan<byte> GetPublicKey(byte[] privateKey) =>
         _keys[Hex.ToString(privateKey)];
   }
}