using System.Text;

namespace Lyn.Protocol.Bolt8
{
   public static class LightningNetworkConfig
   {
      const string PROTOCOL_NAME = "Noise_XK_secp256k1_ChaChaPoly_SHA256";
      private const string PROLOGUE = "lightning";

      public static byte[] ProtocolNameByteArray()
      {
         var byteArray = new byte[PROTOCOL_NAME.Length];
         Encoding.ASCII.GetBytes(PROTOCOL_NAME, 0, PROTOCOL_NAME.Length, byteArray, 0);
         return byteArray;
      }

      public static byte[] PrologueByteArray()
      {
         var byteArray = new byte[PROLOGUE.Length];
         Encoding.ASCII.GetBytes(PROLOGUE, 0, PROLOGUE.Length, byteArray, 0);
         return byteArray;
      }

      public static readonly byte[] NoiseProtocolVersionPrefix = {0x00};

      public const ulong NUMBER_OF_NONCE_BEFORE_KEY_RECYCLE = 1000;

      public const long MAX_MESSAGE_LENGTH = 65535;
   }
}