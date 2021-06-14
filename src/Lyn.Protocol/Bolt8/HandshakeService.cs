using System;
using System.Buffers;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt8
{
   public class HandshakeService : IHandshakeService
   {
      private readonly ILogger<HandshakeService> _logger;
      
      private readonly IEllipticCurveActions _curveActions;
      private readonly IHkdf _hkdf;
      private readonly ICipherFunction _aeadConstruction;
      private readonly INoiseHashFunction _hasher;
      private readonly INoiseMessageTransformer _messageTransformer;
      private readonly IKeyGenerator _keyGenerator;

      public HandshakeContext HandshakeContext { get; set; }

      private int _sessionId;

      public HandshakeService(IEllipticCurveActions curveActions, IHkdf hkdf, 
         ICipherFunction aeadConstruction, IKeyGenerator keyGenerator, INoiseHashFunction hasher,
         INoiseMessageTransformer messageTransformer, ILogger<HandshakeService> logger)
      {
         _curveActions = curveActions;
         _hkdf = hkdf;
         _aeadConstruction = aeadConstruction;
         _keyGenerator = keyGenerator;
         _hasher = hasher;
         _messageTransformer = messageTransformer;
         _logger = logger;
      }
      
      public void InitiateHandShake(byte[] privateKey)
      {
         HandshakeContext = new HandshakeContext(privateKey);

         _sessionId = HandshakeContext.GetHashCode();
         
         _logger.LogInformation("{0} Initiating handshake hash",_sessionId);
         
         _hasher.Hash(LightningNetworkConfig.ProtocolNameByteArray(), HandshakeContext.ChainingKey);

         _hasher.Hash(HandshakeContext.ChainingKey, LightningNetworkConfig.PrologueByteArray(), HandshakeContext.Hash);
      }

      // initiator act one
      public void StartNewInitiatorHandshake(byte[] remotePublicKey, IBufferWriter<byte> output)
      {
         _logger.LogDebug("{0} Initiator handshake: starting act one with remote {1}", _sessionId, 
            BitConverter.ToString(remotePublicKey));
         
         _hasher.Hash(HandshakeContext.Hash, remotePublicKey, HandshakeContext.Hash);

         HandshakeContext.SetRemotePublicKey(remotePublicKey);
         
         GenerateLocalEphemeralAndProcessRemotePublicKey(remotePublicKey, output);
         
         _logger.LogDebug("{0} Completed act one", _sessionId);
      }
      
      public void ProcessHandshakeRequest(ReadOnlySequence<byte> handshakeRequest, IBufferWriter<byte> output)
      {
         if (!HandshakeContext.HasRemotePublic)
         {
            _logger.LogDebug("{0} Responder handshake: starting act one", _sessionId);

            var localStaticPublic = _keyGenerator.GetPublicKey(HandshakeContext.PrivateKey); 
            
            _hasher.Hash(HandshakeContext.Hash, localStaticPublic, HandshakeContext.Hash);
            
            ReadOnlySpan<byte> re = HandleReceivedHandshakeRequest(HandshakeContext.PrivateKey, handshakeRequest);

            _logger.LogDebug("{0} Completed act one, starting act two", _sessionId);

            GenerateLocalEphemeralAndProcessRemotePublicKey(re, output);
            
            _logger.LogDebug("{0} Completed act two", _sessionId);
         }
         else
         {
            _logger.LogDebug("{0} Initiator handshake: started act two", _sessionId);
            ReadOnlySpan<byte> re = HandleReceivedHandshakeRequest(HandshakeContext.EphemeralPrivateKey, handshakeRequest);

            _logger.LogDebug("{0} Completed act two, starting act three", _sessionId);

            CompleteInitiatorHandshake(re, output);
            
            _logger.LogDebug("{0} Completed act three", _sessionId);
         }
      }

      // responder act three
      public void CompleteResponderHandshake(ReadOnlySequence<byte> handshakeRequest)
      {
         _logger.LogInformation("{0} Responder handshake: started act three", _sessionId);
         
         if (!handshakeRequest.FirstSpan.StartsWith(LightningNetworkConfig.NoiseProtocolVersionPrefix))
            throw new AggregateException("Unsupported version in request");
         
         var cipher = handshakeRequest.Slice(1, 49);

         HandshakeContext.RemotePublicKey = new byte[33];
         
         _aeadConstruction.DecryptWithAd(HandshakeContext.Hash, cipher.FirstSpan, HandshakeContext.RemotePublicKey);
         
         _hasher.Hash(HandshakeContext.Hash,cipher.FirstSpan,HandshakeContext.Hash);
         
         var se = _curveActions.Multiply(HandshakeContext.EphemeralPrivateKey, HandshakeContext.RemotePublicKey);
         
         ExtractNextKeys(se);
         
         _aeadConstruction.DecryptWithAd(HandshakeContext.Hash, handshakeRequest.FirstSpan.Slice(50), stackalloc byte[16]);
         
         ExtractFinalChannelKeysForResponder();

         _logger.LogInformation("{0} Completed act three with remote {1}", _sessionId,
            BitConverter.ToString(HandshakeContext.RemotePublicKey));
      }

      public INoiseMessageTransformer GetMessageTransformer()
      {
         if(!_messageTransformer.CanProcessMessages())
            throw new InvalidOperationException("The handshake did not complete");

         return _messageTransformer;
      }

      private void ExtractFinalChannelKeysForInitiator()
      {
         Span<byte> skAndRk = stackalloc byte[64];
         
         _hkdf.ExtractAndExpand(HandshakeContext.ChainingKey, new byte[0].AsSpan(), skAndRk);

         _messageTransformer.SetKeys(HandshakeContext.ChainingKey,skAndRk.Slice(0, 32),
            skAndRk.Slice(32));
      }
      
      private void ExtractFinalChannelKeysForResponder()
      {
         Span<byte> rkAndSk = stackalloc byte[64];
         
         _hkdf.ExtractAndExpand(HandshakeContext.ChainingKey, new byte[0].AsSpan(), rkAndSk);

         _messageTransformer.SetKeys(HandshakeContext.ChainingKey,rkAndSk.Slice(32),
            rkAndSk.Slice(0, 32));
      }

      private void CompleteInitiatorHandshake(ReadOnlySpan<byte> re, IBufferWriter<byte> output)
      {
         //act three initiator
         Span<byte> outputText = output.GetSpan(66);
         
         Span<byte> cipher = outputText.Slice(1, 49);

         _aeadConstruction.EncryptWithAd(HandshakeContext.Hash, 
            _keyGenerator.GetPublicKey(HandshakeContext.PrivateKey).ToArray(), cipher);

         _hasher.Hash(HandshakeContext.Hash, cipher, HandshakeContext.Hash);

         var se = _curveActions.Multiply(HandshakeContext.PrivateKey, re);

         ExtractNextKeys(se);

         _aeadConstruction.EncryptWithAd(HandshakeContext.Hash, new byte[0], 
            outputText.Slice(50, 16));

         ExtractFinalChannelKeysForInitiator();

         output.Advance(66);
      }

      private ReadOnlySpan<byte> HandleReceivedHandshakeRequest(ReadOnlySpan<byte> privateKey,
         ReadOnlySequence<byte> handshakeRequest)
      {
         if (!handshakeRequest.FirstSpan.StartsWith(LightningNetworkConfig.NoiseProtocolVersionPrefix))
            throw new AggregateException("Unsupported version in request");

         var re = handshakeRequest.Slice(1, 33);

         _hasher.Hash(HandshakeContext.Hash, re.ToArray(), HandshakeContext.Hash);

         var secret = _curveActions.Multiply(privateKey.ToArray(), re.FirstSpan); //es and ee

         ExtractNextKeys(secret);

         var c = handshakeRequest.Slice(34, 16);
         
         _aeadConstruction.DecryptWithAd(HandshakeContext.Hash, c.FirstSpan, stackalloc byte[16]);

         _hasher.Hash(HandshakeContext.Hash, c.ToArray(), HandshakeContext.Hash);
         
         return re.FirstSpan;
      }

      private void GenerateLocalEphemeralAndProcessRemotePublicKey(ReadOnlySpan<byte> publicKey, IBufferWriter<byte> output)
      {
         Span<byte> outputText = output.GetSpan(50);
         
         Span<byte> ephemeralPublicKey = outputText.Slice(1, 33);
         Span<byte> cipher = outputText.Slice(34, 16);
         
         HandshakeContext.EphemeralPrivateKey = _keyGenerator.GenerateKey();

         _keyGenerator.GetPublicKey(HandshakeContext.EphemeralPrivateKey)
            .CopyTo(ephemeralPublicKey);

         _hasher.Hash(HandshakeContext.Hash, ephemeralPublicKey, HandshakeContext.Hash);

         var es = _curveActions.Multiply(HandshakeContext.EphemeralPrivateKey, publicKey);

         ExtractNextKeys(es);
         
         _aeadConstruction.EncryptWithAd(HandshakeContext.Hash, null, cipher);

         _hasher.Hash(HandshakeContext.Hash, cipher, HandshakeContext.Hash);
         
         output.Advance(50);
      }
      
      private void ExtractNextKeys(ReadOnlySpan<byte> se)
      {
         Span<byte> ckAndTempKey = stackalloc byte[64];
      
         _hkdf.ExtractAndExpand(HandshakeContext.ChainingKey, se, ckAndTempKey);
      
         ckAndTempKey.Slice(0, 32).CopyTo(HandshakeContext.ChainingKey);
      
         _aeadConstruction.SetKey(ckAndTempKey.Slice(32));
      }
   }
}