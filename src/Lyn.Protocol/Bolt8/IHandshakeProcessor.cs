using System.Buffers;

namespace Lyn.Protocol.Bolt8
{
   public interface IHandshakeProcessor
   {
      void InitiateHandShake(byte[] privateKey);

      void StartNewInitiatorHandshake(byte[] remotePublicKey, IBufferWriter<byte> output);

      void ProcessHandshakeRequest(ReadOnlySequence<byte> handshakeRequest, IBufferWriter<byte> output);

      void CompleteResponderHandshake(ReadOnlySequence<byte> handshakeRequest);

      INoiseMessageTransformer GetMessageTransformer();
   }
}