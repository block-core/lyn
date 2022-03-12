using Lyn.Protocol.Bolt4.Entities;
using Lyn.Types.Fundamental;
using System;
using System.Collections.Generic;

namespace Lyn.Protocol.Bolt4
{
    public interface ISphinx
    {
        PublicKey BlindKey(PublicKey pubKey, IEnumerable<byte[]> blindingFactors);
        PublicKey BlindKey(PublicKey pubKey, ReadOnlySpan<byte> blindingFactor);
        ReadOnlySpan<byte> ComputeBlindingFactor(PublicKey pubKey, ReadOnlySpan<byte> secret);
        (IList<PublicKey>, IList<byte[]>) ComputeEphemeralPublicKeysAndSharedSecrets(PrivateKey sessionKey, ICollection<PublicKey> publicKeys);
        (IList<PublicKey>, IList<byte[]>) ComputeEphemeralPublicKeysAndSharedSecrets(PrivateKey sessionKey, ICollection<PublicKey> publicKeys, IList<PublicKey> ephemeralPublicKeys, IList<byte[]> blindingFactors, IList<byte[]> sharedSecrets);
        ReadOnlySpan<byte> ComputeSharedSecret(PublicKey publicKey, PrivateKey secret);
        PrivateKey DeriveBlindedPrivateKey(PrivateKey privateKey, PublicKey blindingEphemeralKey);
        ReadOnlySpan<byte> ExclusiveOR(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right);
        ReadOnlySpan<byte> GenerateSphinxKey(byte[] keyType, ReadOnlySpan<byte> secret);
        ReadOnlySpan<byte> GenerateSphinxKey(string keyType, ReadOnlySpan<byte> secret);
        ReadOnlySpan<byte> GenerateStream(ReadOnlySpan<byte> keyData, int streamLength);
        ReadOnlySpan<byte> GenerateFiller(string keyType, int packetPayloadLength, IEnumerable<byte[]> sharedSecrets, IEnumerable<byte[]> payloads);
        DecryptedOnionPacket PeelOnion(PrivateKey privateKey, byte[]? associatedData, OnionRoutingPacket packet);
        PacketAndSecrets CreateOnion(PrivateKey sessionKey, int packetPayloadLength, IEnumerable<PublicKey> publicKeys, IEnumerable<byte[]> payloads, byte[]? associatedData);
    }
}