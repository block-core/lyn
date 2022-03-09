using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Bolt4.Entities;
using Lyn.Protocol.Bolt4.Messages;
using Lyn.Protocol.Bolt8;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Fundamental;
using NaCl.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NBitcoin.Secp256k1;
using Lyn.Protocol.Common.Crypto;

namespace Lyn.Protocol.Bolt4
{
    public class OnionMessageService : IBoltMessageService<OnionMessage>
    {
        private readonly IBoltFeatures _boltFeatures;
        private readonly IEllipticCurveActions _ellipticCurveActions;
        private readonly ICipherFunction _cipherFunctions;
        private readonly ISphinx _sphinx;

        // todo: need an actual way to surface this!
        private PrivateKey _nodePrivatekey;

        public OnionMessageService(IBoltFeatures boltFeatures, 
                                   IEllipticCurveActions ellipticCurveActions, 
                                   ICipherFunction cipherFunctions,
                                   ISphinx sphinx)
        {
            _boltFeatures = boltFeatures;
            _ellipticCurveActions = ellipticCurveActions;
            _cipherFunctions = cipherFunctions;
            _sphinx = sphinx;
        }


        // TODO: return DecryptedOnionPacket?
        private void PeelOnion(PrivateKey privateKey, byte[]? associatedData, OnionRoutingPacket packet)
        {
            
            var sharedSecret = _sphinx.ComputeSharedSecret(packet.EphemeralKey, privateKey);
            var mu = _sphinx.GenerateSphinxKey("mu", sharedSecret);
            var payloadToSign = associatedData != null ? packet.PayloadData.Concat(associatedData).ToArray() : packet.PayloadData;
            var computedHmac = HashGenerator.HmacSha256(mu.ToArray(), payloadToSign);

            if (computedHmac == packet.Hmac)
            {
                var rho = _sphinx.GenerateSphinxKey("rho", sharedSecret);
                var cipherStream = _sphinx.GenerateStream(rho.ToArray(), 2 * packet.PayloadData.Length);
                // todo: better variable name here
                var paddedPayload = packet.PayloadData.Concat(Enumerable.Range(0, packet.PayloadData.Length).Select<int, byte>(x => 0x00)).ToArray();
                var binData = _sphinx.ExclusiveOR(paddedPayload, cipherStream);

                // todo: peek payload length
                
                // todo: extract payload bytes from xor'd byte stream using payload length and hmac
            }
            else
            {
                throw new Exception("bad hmac");
            }

        }

        public Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<OnionMessage> message)
        {
            if (_boltFeatures.SupportedFeatures.HasFlag(Features.OptionOnionMessagesRequired))
            {
                // todo: verify payload bytes aren't too large

                // else, if payload is correct sized
                var blindedPrivateKey = _sphinx.DeriveBlindedPrivateKey(_nodePrivatekey, message.MessagePayload.BlindingKey);

                // peel the onion
                PeelOnion(blindedPrivateKey, null, message.MessagePayload.OnionPacket);
            }

            throw new NotImplementedException();
        }
    }
}
