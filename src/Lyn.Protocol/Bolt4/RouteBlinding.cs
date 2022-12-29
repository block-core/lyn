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

    public record IntroductionNode(PublicKey PublicKey, PublicKey BlindedPublicKey, PublicKey BlindingEphemeralKey, byte[] EncryptedPayload);

    public record BlindedNode(PublicKey BlindedPublicKey, byte[] EncryptedPayload);

    public record BlindedRoute
    {

        // todo: Are these two properties needed? The other properties contain the same information?
        public PublicKey IntroductionNodeId { get; init; }
        public PublicKey BlindingKey { get; init; }

        public IntroductionNode IntroductionNode { get; init; }
        public BlindedNode[] SubsequentNodes { get; private set; }
        public PublicKey[] BlindedNodeIds { get; private set; }
        public byte[][] EncryptedPayloads { get; private set; }


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
            SubsequentNodes = BlindedNodes.Skip(1).ToArray();
            BlindedNodeIds = BlindedNodes.Select(x => x.BlindedPublicKey).ToArray();
            EncryptedPayloads = BlindedNodes.Select(x => x.EncryptedPayload).ToArray();
        }
    }

    public record BlindedRouteDetails(BlindedRoute Route, PublicKey LastBlinding);

    public class RouteBlinding
    {

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
            var blindedHopsAndKeys = hops.Select((hop) =>
            {
                var (publicKey, payload) = hop;

                // Compute a shared secret, use it to derive ablinding key and rho for the ChaCha20Poly1305 cipher
                var sharedSecret = _sphinx.ComputeSharedSecret(publicKey, e);
                var blindingKey = _lightningKeyDerivation.PublicKeyFromPrivateKey(e);
                var blindedPublicKey = _sphinx.BlindKey(publicKey, _sphinx.GenerateSphinxKey("blinded_node_id", sharedSecret));
                var rho = _sphinx.GenerateSphinxKey("rho", sharedSecret);
                var cipher = new ChaCha20Poly1305(rho.ToArray());

                // Next, allocate some buffers and encrypt the payload for this hop 
                // note: i didn't use the ChaCha20Poly1305CipherFunction bc the nonce increments by 1 for each call, and this requires a nonce of 0(?)
                Span<byte> encryptedPayload = stackalloc byte[payload.Length + 16];
                Span<byte> ciphertext = stackalloc byte[payload.Length];
                Span<byte> mac = stackalloc byte[16];

                cipher.Encrypt(new byte[12], payload.AsSpan(), ciphertext, mac, new byte[0]);

                ciphertext.CopyTo(encryptedPayload);
                mac.CopyTo(encryptedPayload.Slice(payload.Length));

                // Before we move onto the next hop we need to derive the next hop's e value
                // todo: is blindingKey length a fixed 32 bytes?
                Span<byte> bytesToHash = stackalloc byte[blindingKey.GetSpan().Length + sharedSecret.Length];
                blindingKey.GetSpan().CopyTo(bytesToHash);
                sharedSecret.CopyTo(bytesToHash.Slice(blindingKey.GetSpan().Length));
                var newPrivKey = new PrivateKey(HashGenerator.Sha256(bytesToHash).ToArray());
                e = new PrivateKey(_ellipticCureActions.MultiplyWithPrivateKey(newPrivKey, e).ToArray());
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
            var sharedSecret = _sphinx.ComputeSharedSecret(blindingEphemeralKey, privateKey);
            var generatedKey = new PrivateKey(_sphinx.GenerateSphinxKey("blinded_node_id", sharedSecret).ToArray());
            return new PrivateKey(_ellipticCureActions.MultiplyWithPrivateKey(generatedKey, privateKey).ToArray());
        }

        public (byte[], PublicKey) DecryptPayload(PrivateKey privateKey, PublicKey blindingEphemeralKey, byte[] encryptedPayload)
        {
            // Derive rho from our shared secret and instantiate a ChaCha20Poly1305 cipher
            var sharedSecret = _sphinx.ComputeSharedSecret(blindingEphemeralKey, privateKey);
            var rho = _sphinx.GenerateSphinxKey("rho", sharedSecret);
            var cipher = new ChaCha20Poly1305(rho.ToArray());

            // Create some buffers to handle the decryption
            Span<byte> decryptedPayload = stackalloc byte[encryptedPayload.Length - 16];
            Span<byte> ciphertext = stackalloc byte[encryptedPayload.Length - 16];
            Span<byte> mac = stackalloc byte[16];

            // Split the encrypted payload into the ciphertext and the MAC for decryption
            encryptedPayload.AsSpan().Slice(0, encryptedPayload.Length - 16).CopyTo(ciphertext);
            encryptedPayload.AsSpan().Slice(encryptedPayload.Length - 16).CopyTo(mac);

            // Decrypt the payload
            // Note: Should figure out what the right error handling is here
            cipher.Decrypt(new byte[12], ciphertext, mac, decryptedPayload, new byte[0]);
            var nextBlindingEphemeralKey = _sphinx.BlindKey(blindingEphemeralKey, _sphinx.ComputeBlindingFactor(blindingEphemeralKey, sharedSecret));
            return (decryptedPayload.ToArray(), nextBlindingEphemeralKey);
        }
    }
}
