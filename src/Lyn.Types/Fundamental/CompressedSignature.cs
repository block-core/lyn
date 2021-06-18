using System;

namespace Lyn.Types.Fundamental
{
    public class CompressedSignature
    {
        public const ushort LENGTH = 64;

        private readonly byte[] _value;

        // public Span<byte> R => _value.AsSpan(0, 32);
        // public Span<byte> S => _value.AsSpan(32);

        public CompressedSignature()
        {
            _value = new byte[0];
        }

        public CompressedSignature(byte[] value)
        {
            if (value.Length != 64)
                throw new ArgumentOutOfRangeException(nameof(value));

            _value = value;
        }

        public bool HasValue => _value.Length > 0;

        public static implicit operator byte[](CompressedSignature hash) => hash._value;

        public static implicit operator CompressedSignature(byte[] bytes) => new (bytes);

        public static implicit operator CompressedSignature(ReadOnlySpan<byte> bytes) => new (bytes.ToArray());
    }
}