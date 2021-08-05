using System;

namespace Lyn.Types.Fundamental
{
    /// <summary>
    /// A wrapper for EC signatures that include the Bitcoin Sighash encoding appended at the end of the payload.
    /// </summary>
    public class BitcoinSignature
    {
        protected readonly byte[] _value;

        public BitcoinSignature(byte[] value)
        {
            if (value.Length > 74)
                throw new ArgumentOutOfRangeException(nameof(value));

            _value = value;
        }

        public ReadOnlySpan<byte> GetSpan()
        {
            return _value.AsSpan();
        }

        public static implicit operator ReadOnlySpan<byte>(BitcoinSignature hash) => hash._value;

        public static implicit operator byte[](BitcoinSignature hash) => hash._value;

        public static explicit operator BitcoinSignature(byte[] bytes) => new BitcoinSignature(bytes);

        public static explicit operator BitcoinSignature(ReadOnlySpan<byte> bytes) => new BitcoinSignature(bytes.ToArray());

        public override string ToString()
        {
            return Hex.ToString(_value.AsSpan());
        }
    }
}