using System;
using System.Buffers;

namespace Lyn.Protocol.Bolt8
{
   public interface INoiseMessageTransformer
   {
      void SetKeys(ReadOnlySpan<byte> chainingKey, ReadOnlySpan<byte> senderKey, ReadOnlySpan<byte> receiverKey);

      bool CanProcessMessages();
      
      int WriteMessage(ReadOnlySequence<byte> message, IBufferWriter<byte> output);
      
      int ReadMessage(ReadOnlySequence<byte> message, IBufferWriter<byte> output);
   }
}