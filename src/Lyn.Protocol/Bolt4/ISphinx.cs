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
        (IEnumerable<PublicKey>, IEnumerable<byte[]>) ComputeEphemeralPublicKeysAndSharedSecrets(PrivateKey sessionKey, ICollection<PublicKey> publicKeys);
        (IEnumerable<PublicKey>, IEnumerable<byte[]>) ComputeEphemeralPublicKeysAndSharedSecrets(PrivateKey sessionKey, ICollection<PublicKey> publicKeys, IList<PublicKey> ephemeralPublicKeys, IList<byte[]> blindingFactors, IList<byte[]> sharedSecrets);
        ReadOnlySpan<byte> ComputeSharedSecret(PublicKey publicKey, PrivateKey secret);
        PrivateKey DeriveBlindedPrivateKey(PrivateKey privateKey, PublicKey blindingEphemeralKey);
        ReadOnlySpan<byte> ExclusiveOR(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right);
        ReadOnlySpan<byte> GenerateSphinxKey(byte[] keyType, ReadOnlySpan<byte> secret);
        ReadOnlySpan<byte> GenerateSphinxKey(string keyType, ReadOnlySpan<byte> secret);
        ReadOnlySpan<byte> GenerateStream(ReadOnlyMemory<byte> keyData, int streamLength);
        DecryptedOnionPacket PeelOnion(PrivateKey privateKey, byte[]? associatedData, OnionRoutingPacket packet);
    }
}