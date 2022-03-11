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
using System.Buffers;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt4
{
    public class OnionMessageService : IBoltMessageService<OnionMessage>
    {
        //todo: move somewhere better
        private const int MAC_LENGTH = 32;

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
        private DecryptedOnionPacket PeelOnion(PrivateKey privateKey, byte[]? associatedData, OnionRoutingPacket packet)
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
                var binData = _sphinx.ExclusiveOR(paddedPayload, cipherStream).ToArray();

                var sequence = new ReadOnlySequence<byte>(new ReadOnlyMemory<byte>(binData));
                var binReader = new SequenceReader<byte>(sequence);

                // todo: peek payload length
                if (binReader.TryPeek(out var payloadLength))
                {
                    int perHopPayloadLength = 0;

                    if (payloadLength == 0x00)
                    {
                        // todo: this might be deprecated? do we need to support legacy payloads in Lyn?
                        perHopPayloadLength = 65;
                    }
                    else
                    {
                        // safe to truncate because a packet will never be larger than 64KB
                        perHopPayloadLength = (int)binReader.ReadVarInt();
                    }

                    // todo: extract payload bytes from xor'd byte stream using payload length and hmac
                    var perHopPayload = binReader.ReadBytes(perHopPayloadLength);
                    var hopHMAC = binReader.ReadBytes(MAC_LENGTH);

                    // truncated'd again but its safe?
                    var nextOnionPayload = binReader.ReadBytes((int)binReader.Remaining);
                    var nextPublicKey = _sphinx.BlindKey(packet.EphemeralKey, _sphinx.ComputeBlindingFactor(packet.EphemeralKey, sharedSecret));

                    return new DecryptedOnionPacket()
                    {
                        Payload = perHopPayload.ToArray(),
                        NextPacket = new OnionRoutingPacket()
                        {
                            Version = 0x01,
                            EphemeralKey = nextPublicKey,
                            PayloadData = nextOnionPayload.ToArray(),
                            Hmac = hopHMAC.ToArray()
                        },
                        SharedSecret = sharedSecret.ToArray(),
                    };
                }
            }
            else
            {
                throw new Exception("bad hmac");
            }

            throw new Exception("Bah! Humbug!");
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
