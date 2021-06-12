using System;

namespace Lyn.Types.Fundamental
{
    public class PublicKey
    {
        public const ushort LENGTH = 33;

        private readonly byte[] _value;

        public PublicKey()
        {
            _value = new byte[0];
        }

        public PublicKey(byte[] value)
        {
            if (value.Length != LENGTH)
                throw new ArgumentOutOfRangeException(nameof(value));

            _value = value;
        }

        public ReadOnlySpan<byte> GetSpan()
        {
            return _value.AsSpan();
        }

        public static implicit operator ReadOnlySpan<byte>(PublicKey hash) => hash._value;

        public static implicit operator byte[](PublicKey hash) => hash._value;

        public static explicit operator PublicKey(byte[] bytes) => new PublicKey(bytes);

        public static explicit operator PublicKey(ReadOnlySpan<byte> bytes) => new PublicKey(bytes.ToArray());

        public override string ToString()
        {
            return _value.AsSpan().ToHexString();
        }
    }
}