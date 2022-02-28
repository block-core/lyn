using System;
using Lyn.Types.Fundamental;
using NBitcoin;

namespace Lyn.Protocol.Common.Crypto
{
    public class EllipticCurveActions : IEllipticCurveActions
    {
        public ReadOnlySpan<byte> Multiply(byte[] privateKey, ReadOnlySpan<byte> publicKey)
           => new PubKey(publicKey.ToArray())
              .GetSharedSecret(new Key(privateKey));


        public ReadOnlySpan<byte> MultiplyPubKey(byte[] privateKey, ReadOnlySpan<byte> publicKey)
            => new PubKey(publicKey.ToArray())
                .GetSharedPubkey(new Key(privateKey)).ToBytes();
    }
}