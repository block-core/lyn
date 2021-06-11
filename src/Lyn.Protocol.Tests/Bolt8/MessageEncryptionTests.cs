using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Linq;
using Lyn.Protocol.Bolt8;
using Lyn.Types;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt8
{
   public class MessageEncryptionTests : Bolt8InitiatedNoiseProtocolTests
   {
      private HandshakeProcessor? _initiatorHandshakeState;
      private HandshakeProcessor? _responderHandshakeState;
      

      private new void WithInitiatorHandshakeInitiatedToKnownLocalAndRemoteKeys()
      {
         _initiatorHandshakeState = InitiateNoiseProtocol(new FixedKeysGenerator(
                  Bolt8TestVectorParameters.InitiatorEphemeralKeyPair.PrivateKey,
                  Bolt8TestVectorParameters.InitiatorEphemeralKeyPair.PublicKey)
               .AddKeys(Bolt8TestVectorParameters.Initiator.PrivateKey,
                  Bolt8TestVectorParameters.Initiator.PublicKey),
            Bolt8TestVectorParameters.Initiator.PrivateKey);
         
         _initiatorHandshakeState.InitiateHandShake(Bolt8TestVectorParameters.Initiator.PrivateKey);;
      }

      private new void WithResponderHandshakeInitiatedToKnownLocalKeys()
      {
         _responderHandshakeState = InitiateNoiseProtocol(new FixedKeysGenerator(
                  Bolt8TestVectorParameters.ResponderEphemeralKeyPair.PrivateKey,
                  Bolt8TestVectorParameters.ResponderEphemeralKeyPair.PublicKey)
               .AddKeys(Bolt8TestVectorParameters.Responder.PrivateKey,
                  Bolt8TestVectorParameters.Responder.PublicKey),
            Bolt8TestVectorParameters.Responder.PrivateKey);
         
         _responderHandshakeState.InitiateHandShake(Bolt8TestVectorParameters.Responder.PrivateKey);
      }

      [Theory]
      [InlineData("0xcf2b30ddf0cf3f80e7c35a6e6730b59fe802473180f396d88a8fb0db8cbcf25d2f214cf9ea1d95")]
      public void TestMessageEncryptionIterationZero(string expectedOutputHex)
      {
         var (initiatorTransport, _) = WithTheHandshakeCompletedSuccessfully();

         var expectedOutput = EncryptMessage(GetMessage, initiatorTransport);

         Assert.Equal(expectedOutput, expectedOutputHex.ToByteArray());
      }

      [Theory]
      [InlineData("0x72887022101f0b6753e0c7de21657d35a4cb2a1f5cde2650528bbc8f837d0f0d7ad833b1a256a1")]
      public void TestMessageEncryptionIterationOne(string expectedOutputHex)
      {
         var (initiatorTransport, responderTransport) = 
            WithTheHandshakeCompletedSuccessfully();
         
         var input = EncryptMessage(GetMessage, initiatorTransport);

         var decryptedMessage = DecryptAndValidateMessage(new ReadOnlySequence<byte>(input),
            responderTransport);

         var output = EncryptMessage(GetMessage, initiatorTransport);

         Assert.Equal(GetMessage.FirstSpan.ToArray(),decryptedMessage.ToArray());
         Assert.Equal(output, expectedOutputHex.ToByteArray());
      }

      [Theory]
      [InlineData(500, "0x178cb9d7387190fa34db9c2d50027d21793c9bc2d40b1e14dcf30ebeeeb220f48364f7a4c68bf8")]
      [InlineData(501, "0x1b186c57d44eb6de4c057c49940d79bb838a145cb528d6e8fd26dbe50a60ca2c104b56b60e45bd")]
      [InlineData(1000, "0x4a2f3cc3b5e78ddb83dcb426d9863d9d9a723b0337c89dd0b005d89f8d3c05c52b76b29b740f09")]
      [InlineData(1001, "0x2ecd8c8a5629d0d02ab457a0fdd0f7b90a192cd46be5ecb6ca570bfc5e268338b1a16cf4ef2d36")]
      public void TestMessageEncryptionIterationN(int iterationTarget, string expectedOutputHex)
      {
         var (initiatorTransport, responderTransport) = WithTheHandshakeCompletedSuccessfully();

         const string message = "0x68656c6c6f";

         var encryptedMessage = GetArray(39); //the message that is encrypted is 39 bytes should be protocol max length
         ReadOnlySpan<byte> decryptedMessage = null;

         for (int i = 0; i <= iterationTarget; i++)
         {
            encryptedMessage.Clear();
            
            EncryptMessage(GetMessage, initiatorTransport, encryptedMessage);

            decryptedMessage = DecryptAndValidateMessage(new ReadOnlySequence<byte>(encryptedMessage.WrittenMemory),
               responderTransport);
         }

         Assert.Equal(decryptedMessage.ToArray(), message.ToByteArray());
         Assert.Equal(encryptedMessage.WrittenMemory.ToArray(), expectedOutputHex.ToByteArray());
      }

      private static byte[] EncryptMessage(ReadOnlySequence<byte> m, INoiseMessageTransformer transport)
      {
         var outputBuffer = GetArray((int)LightningNetworkConfig.MAX_MESSAGE_LENGTH);

         var l = BitConverter.GetBytes(Convert.ToInt16(m.Length))
             .Reverse().ToArray(); //from little endian

         transport.WriteMessage(new ReadOnlySequence<byte>(l), outputBuffer);

         transport.WriteMessage(m, outputBuffer);

         return outputBuffer.WrittenSpan.ToArray();
      }

      private static void EncryptMessage(ReadOnlySequence<byte> m, INoiseMessageTransformer transport, 
         ArrayBufferWriter<byte> outputBuffer)
      {
         var l = BitConverter.GetBytes(Convert.ToInt16(m.Length))
             .Reverse().ToArray(); //from little endian

         transport.WriteMessage(new ReadOnlySequence<byte>(l), outputBuffer);

         transport.WriteMessage(m, outputBuffer);
      }

      private static ReadOnlySpan<byte> DecryptAndValidateMessage(ReadOnlySequence<byte> message, 
         INoiseMessageTransformer transport)
      {
         var header = GetArray(2);

         transport.ReadMessage(message.Slice(0, 18), header);

         var body = GetArray(BinaryPrimitives.ReadUInt16BigEndian(header.WrittenSpan));

         int bodyLength = transport.ReadMessage(message.Slice(18), body);

         return body.WrittenSpan.Slice(0, bodyLength);
      }

      private (INoiseMessageTransformer, INoiseMessageTransformer) WithTheHandshakeCompletedSuccessfully()
      {
         WithInitiatorHandshakeInitiatedToKnownLocalAndRemoteKeys();
         WithResponderHandshakeInitiatedToKnownLocalKeys();

         var actOneBuffer = GetArray(50);
         var actTwoBuffer = GetArray(50);
         var actThreeBuffer = GetArray(66);
         
         _initiatorHandshakeState.StartNewInitiatorHandshake(Bolt8TestVectorParameters.Responder.PublicKey, actOneBuffer);
         _responderHandshakeState.ProcessHandshakeRequest(new ReadOnlySequence<Byte>(actOneBuffer.WrittenMemory) , actTwoBuffer);
         _initiatorHandshakeState.ProcessHandshakeRequest(new ReadOnlySequence<Byte>(actTwoBuffer.WrittenMemory), actThreeBuffer);
         _responderHandshakeState.CompleteResponderHandshake(new ReadOnlySequence<Byte>(actThreeBuffer.WrittenMemory));

         return (_initiatorHandshakeState.GetMessageTransformer(), _responderHandshakeState.GetMessageTransformer());
      }

      private static ArrayBufferWriter<byte> GetArray(int size) => new ArrayBufferWriter<byte>(size);
      
      private static ReadOnlySequence<byte> GetMessage => new ReadOnlySequence<byte>("0x68656c6c6f".ToByteArray());
   }
}