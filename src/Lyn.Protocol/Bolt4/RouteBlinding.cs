using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Common.Crypto;
using Lyn.Types.Fundamental;
using NaCl.Core;

namespace Lyn.Protocol.Bolt4
{

    public class RouteBlinding
    {

        public record IntroductionNode(PublicKey PublicKey, PublicKey BlindedPublicKey, PublicKey BlindingEphemeralKey, byte[] EncryptedPayload);

        public record BlindedNode(PublicKey BlindedPublicKey, byte[] EncryptedPayload);

        public record BlindedRoute
        {

            // todo: Are these two properties needed? The other properties contain the same information?
            public PublicKey IntroductionNodeId { get; init; }
            public PublicKey BlindingKey { get; init; }
            
            public IntroductionNode IntroductionNode { get; init; }
            public IEnumerable<BlindedNode> SubsequentNodes { get; private set; }
            public IEnumerable<PublicKey> BlindedNodeIds { get; private set; }
            public IEnumerable<byte[]> EncryptedPayloads { get; private set; }


            public BlindedNode[] BlindedNodes { get; init; }

            public BlindedRoute(PublicKey introductionNodeId, PublicKey blindingKey, BlindedNode[] blindedNodes)
            {
                if (blindedNodes.Length == 0)
                {
                    throw new ArgumentException(nameof(blindedNodes), "blinded route must not be empty");
                }

                IntroductionNodeId = introductionNodeId;
                BlindingKey = blindingKey;
                BlindedNodes = blindedNodes;

                IntroductionNode = new IntroductionNode(IntroductionNodeId, BlindedNodes.First().BlindedPublicKey, BlindingKey, BlindedNodes.First().EncryptedPayload);
                SubsequentNodes = BlindedNodes.Skip(1);
                BlindedNodeIds = BlindedNodes.Select(x => x.BlindedPublicKey);
                EncryptedPayloads = BlindedNodes.Select(x => x.EncryptedPayload);
            }
        }

        public record BlindedRouteDetails(BlindedRoute Route, PublicKey LastBlinding);

        private readonly ISphinx _sphinx = null;
        private readonly ILightningKeyDerivation _lightningKeyDerivation = null;
        private readonly IEllipticCurveActions _ellipticCureActions = null;

        public RouteBlinding(ISphinx sphinx, ILightningKeyDerivation lightningKeyDerivation, IEllipticCurveActions ellipticCurveActions)
        {
            this._sphinx = sphinx;
            this._lightningKeyDerivation = lightningKeyDerivation;
            this._ellipticCureActions = ellipticCurveActions;
        }

        // note: eclair has separate lists of public keys and encrypted payloads, yet asserts that they are the same length
        // todo: should we do the same? i feel this enumerable of tuples is more elegant but i wonder if there's a 'muh security' reason for the separate lists
        public BlindedRouteDetails Create(PrivateKey sessionKey, IEnumerable<(PublicKey PublicKey, byte[] Payload)> hops)        
        {
            var e = sessionKey;
            var blindedHopsAndKeys = hops.Select((hop) => {
                var (publicKey, payload) = hop;
                var blindingKey = _lightningKeyDerivation.PublicKeyFromPrivateKey(e);
                var sharedSecret = _sphinx.ComputeSharedSecret(publicKey, sessionKey);
                var blindedPublicKey = _sphinx.BlindKey(publicKey, _sphinx.GenerateSphinxKey("blinded_node_id", sharedSecret));
                var rho = _sphinx.GenerateSphinxKey("rho", sharedSecret);

                // is this right? who knows! i hsould ask chatgpt later
                // note: i didn't use the ChaCha20Poly1305CipherFunction bc the nonce increments by 1 for each call, and this requires a nonce of 0(?)
                var cipher = new ChaCha20Poly1305(rho.ToArray());
                Span<byte> encryptedPayload = stackalloc byte[payload.Length];
                Span<byte> mac = stackalloc byte[16];
                cipher.Encrypt(new byte[12], payload.AsSpan(), encryptedPayload, mac, new byte[0]);

                // broken: the value for e is not being calculated correctly
                // broken: this is also likely massively inefficient
                var keyBytes = blindingKey.GetSpan().ToArray();
                var sharedSecretArr = sharedSecret.ToArray();
                var hash = HashGenerator.Sha256(keyBytes.Concat(sharedSecretArr).ToArray());
                Debug.WriteLine($"hash: {Convert.ToHexString(hash.ToArray())}");
                var newPrivKey = new PrivateKey(hash.ToArray());
                e = new PrivateKey(_ellipticCureActions.Multiply(newPrivKey, blindingKey).ToArray());
                Debug.WriteLine($"e*PrivKey(hash): {Convert.ToHexString(e)}");
                return (blindedHop: new BlindedNode(blindedPublicKey, encryptedPayload.ToArray()), blindingKey: blindingKey);
            }).ToList();

            return new BlindedRouteDetails(new BlindedRoute(hops.First().PublicKey, 
                                                            blindedHopsAndKeys.First().blindingKey, 
                                                            blindedHopsAndKeys.Select(x => x.blindedHop).ToArray()), 
                                                blindedHopsAndKeys.Last().blindingKey
                                            );
        }
        
        public PrivateKey DerivePrivateKey(PrivateKey privateKey, PublicKey blindingEphemeralKey)
        {
            // todo: i need some test coverage to verify the multiply logic here
            // note: my guess? way off like in Create ¯\_(ツ)_/¯
            var sharedSecret = _sphinx.ComputeSharedSecret(blindingEphemeralKey, privateKey);
            var generatedKey = new PrivateKey(_sphinx.GenerateSphinxKey("blinded_node_id", sharedSecret).ToArray());
            var pubKeyForPriv = _lightningKeyDerivation.PublicKeyFromPrivateKey(privateKey);
            return new PrivateKey(_ellipticCureActions.Multiply(generatedKey, pubKeyForPriv).ToArray());
        }

        public (byte[], PublicKey) DecryptPayload(PrivateKey privateKey, PublicKey blindingEphemeralKey, byte[] encryptedPayload)
        {
            var sharedSecret = _sphinx.ComputeSharedSecret(blindingEphemeralKey, privateKey);
            var rho = _sphinx.GenerateSphinxKey("rho", sharedSecret);
            var cipher = new ChaCha20Poly1305(rho.ToArray());
            Span<byte> decryptedPayload = stackalloc byte[encryptedPayload.Length - 16];
            Span<byte> mac = stackalloc byte[16];
            cipher.Decrypt(new byte[12], encryptedPayload.AsSpan(), decryptedPayload, mac, new byte[0]);
            var nextBlindingEphemeralKey = _sphinx.BlindKey(blindingEphemeralKey, _sphinx.ComputeBlindingFactor(blindingEphemeralKey, sharedSecret));
            return (decryptedPayload.ToArray(), nextBlindingEphemeralKey);
        }
    }
}
