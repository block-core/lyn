using System;
using System.Buffers;
using Lyn.Types;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt8
{
   public class HandshakeOutputTests : Bolt8InitiatedNoiseProtocolTests
   {
      private void WithInitiatorActOneCompletedSuccessfully()
      {
         IBufferWriter<byte> buffer = new ArrayBufferWriter<byte>(50);

         NoiseProtocol.StartNewInitiatorHandshake(Bolt8TestVectorParameters.Responder.PublicKey, buffer);
      }

      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActOne.END_STATE_HASH, Bolt8TestVectorParameters.ActOne.INITIATOR_OUTPUT)]
      public void ActOneInitiatorOutputFitsLightningNetworkBolt8testVector(string expectedHashHex, string expectedOutputHex)
      {
         WithInitiatorHandshakeInitiatedToKnownLocalAndRemoteKeys();

         var buffer = new ArrayBufferWriter<byte>(50);

         NoiseProtocol.StartNewInitiatorHandshake(Bolt8TestVectorParameters.Responder.PublicKey, buffer);

         var expectedOutput = Hex.FromString(expectedOutputHex);

         Assert.Equal(buffer.WrittenCount, expectedOutput.Length);
         Assert.Equal(buffer.WrittenSpan.ToArray(), expectedOutput);
         Assert.Equal(Hex.FromString(expectedHashHex), NoiseProtocol.HandshakeContext.Hash);
      }


      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActOne.INITIATOR_OUTPUT,
          Bolt8TestVectorParameters.ActTwo.END_STATE_HASH,
          Bolt8TestVectorParameters.ActTwo.RESPONDER_OUTPUT)]
      public void ActTwoResponderSide(string actOneValidInput, string expectedHashHex, string expectedOutputHex)
      {
         WithResponderHandshakeInitiatedToKnownLocalKeys();

         var buffer = new ArrayBufferWriter<byte>(50);

         NoiseProtocol.ProcessHandshakeRequest(new ReadOnlySequence<Byte>(Hex.FromString(actOneValidInput)), buffer);

         var expectedOutput = Hex.FromString(expectedOutputHex);

         Assert.Equal(expectedOutput.Length, buffer.WrittenCount);
         Assert.Equal(expectedOutput,buffer.WrittenSpan.ToArray());
         Assert.Equal(Hex.FromString(expectedHashHex), NoiseProtocol.HandshakeContext.Hash);
      }

      [Theory]
      [InlineData(Bolt8TestVectorParameters.ActTwo.RESPONDER_OUTPUT,
          Bolt8TestVectorParameters.ActThree.END_STATE_HASH,
          Bolt8TestVectorParameters.ActThree.INITIATOR_OUTPUT)]
      public void ActThreeInitiatorSide(string validInputHex, string expectedHashHex, string expectedOutputHex)
      {
         WithInitiatorHandshakeInitiatedToKnownLocalAndRemoteKeys();

         WithInitiatorActOneCompletedSuccessfully();

         var buffer = new ArrayBufferWriter<byte>();

         NoiseProtocol.ProcessHandshakeRequest(new ReadOnlySequence<Byte>(Hex.FromString(validInputHex)), buffer);
         
         var expectedOutput = Hex.FromString(expectedOutputHex);

         Assert.Equal(expectedOutput.Length, buffer.WrittenCount);
         Assert.Equal(Hex.FromString(expectedHashHex),NoiseProtocol.HandshakeContext.Hash);
         Assert.Equal(buffer.WrittenSpan.ToArray(), expectedOutput);
      }
   }
}