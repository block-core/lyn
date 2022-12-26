using System;

namespace Lyn.Protocol.Common.Crypto
{
    public interface IEllipticCurveActions
    {
        ReadOnlySpan<byte> Multiply(byte[] privateKey, ReadOnlySpan<byte> publicKey);
        ReadOnlySpan<byte> MultiplyWithPrivateKey(byte[] privateKey, ReadOnlySpan<byte> blindingKey);
        ReadOnlySpan<byte> MultiplyPubKey(byte[] privateKey, ReadOnlySpan<byte> publicKey);
    }
}