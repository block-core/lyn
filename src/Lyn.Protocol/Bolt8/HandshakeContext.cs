using System;

namespace Lyn.Protocol.Bolt8
{
   public class HandshakeContext
   {
      public HandshakeContext(byte[] privateKey)
      {
         PrivateKey = privateKey;
         Hash = new byte[32];
         ChainingKey = new byte[32];
         EphemeralPrivateKey = new byte[0];
      }

      public byte[] Hash { get; }
      public byte[] ChainingKey { get; }
      public byte[] EphemeralPrivateKey { get; set; }
      public byte[] PrivateKey { get; }
      public byte[] RemotePublicKey { get; set; } = new byte[0];

      public void SetRemotePublicKey(byte[] remotePublicKey)
      {
         RemotePublicKey = new byte[33];
         
         remotePublicKey.CopyTo(RemotePublicKey.AsSpan());
      }
      
      public bool HasRemotePublic => RemotePublicKey.Length != 0;
   }
}