using System;
using System.Buffers;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt8
{
   public class NoiseMessageTransformer : INoiseMessageTransformer
   {
      private readonly ILogger<NoiseMessageTransformer> _logger;
      
      readonly IHkdf _hkdf;
      readonly ICipherFunction _writer, _reader;
      readonly byte[] _readerChainingKey;
      readonly byte[] _writerChainingKey;
      bool _keysSet;

      public NoiseMessageTransformer(IHkdf hkdf, ICipherFunction writer, ICipherFunction reader, ILogger<NoiseMessageTransformer> logger)
      {
         _hkdf = hkdf;
         _writer = writer;
         _reader = reader;
         _logger = logger;
         _readerChainingKey = new byte[32];
         _writerChainingKey = new byte[32];
      }

      public void SetKeys(ReadOnlySpan<byte> chainingKey, ReadOnlySpan<byte> senderKey, ReadOnlySpan<byte> receiverKey)
      {
         chainingKey.CopyTo(_readerChainingKey.AsSpan());
         chainingKey.CopyTo(_writerChainingKey.AsSpan());
         _writer.SetKey(senderKey);
         _reader.SetKey(receiverKey);
         _keysSet = true;
      }

      public bool CanProcessMessages() => _keysSet;

      public int WriteEncryptedMessage(ReadOnlySequence<byte> message, IBufferWriter<byte> output)
      {
         if (message.Length + Aead.TAG_SIZE > LightningNetworkConfig.MAX_MESSAGE_LENGTH)
            throw new ArgumentException($"Noise message must be less than or equal to {LightningNetworkConfig.MAX_MESSAGE_LENGTH} bytes in length.");
         
         _logger.LogDebug($"Transforming message to lightning output");
         
         int numOfBytesRead =  _writer.EncryptWithAd(null, message.ToArray(), // TODO David here we call to array should be replaced
            output.GetSpan((int)message.Length + Aead.TAG_SIZE));
         
         output.Advance(numOfBytesRead);
         
         KeyRecycle(_writer, _writerChainingKey);
         
         return numOfBytesRead;
      }

      public int ReadEncryptedMessage(ReadOnlySequence<byte> message, IBufferWriter<byte> output)
      {
         _logger.LogDebug($"Transforming lightning input to message");
         
         int numOfBytesRead = _reader.DecryptWithAd(null, message.ToArray(), // TODO David here we call to array should be replaced 
            output.GetSpan((int)message.Length + Aead.TAG_SIZE));

         output.Advance(numOfBytesRead);
         
         KeyRecycle(_reader, _readerChainingKey);

         return numOfBytesRead;
      }
      
      private void KeyRecycle(ICipherFunction cipherFunction, Span<byte> chainingKey)
      {
         if (cipherFunction.GetNonce() < LightningNetworkConfig.NUMBER_OF_NONCE_BEFORE_KEY_RECYCLE)
            return;
         
         _logger.LogDebug($"Recycling cipher key");
         
         Span<byte> keys = stackalloc byte[Aead.KEY_SIZE * 2];
         _hkdf.ExtractAndExpand(chainingKey, cipherFunction.GetKey(), keys);

         // set new chaining key
         keys.Slice(0, Aead.KEY_SIZE)
            .CopyTo(chainingKey);

         // set new key
         cipherFunction.SetKey(keys.Slice(Aead.KEY_SIZE));
         
         _logger.LogDebug($"Cipher key recycled successfully");
      }
   }
}