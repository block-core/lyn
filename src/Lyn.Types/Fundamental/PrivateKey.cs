using System;
using System.Linq;

namespace Lyn.Types.Fundamental
{
    public class PrivateKey : IEquatable<PrivateKey>
    {
        public const ushort LENGTH = 32;

        protected readonly byte[] _value;

        public PrivateKey(byte[] value)
        {
            if (value.Length > LENGTH)
                throw new ArgumentOutOfRangeException(nameof(value));

            _value = value;
        }

        public static implicit operator ReadOnlySpan<byte>(PrivateKey hash) => hash._value;

        public static implicit operator byte[](PrivateKey hash) => hash._value;

        public static explicit operator PrivateKey(byte[] bytes) => new PrivateKey(bytes);

        public static explicit operator PrivateKey(ReadOnlySpan<byte> bytes) => new PrivateKey(bytes.ToArray());

        public override string ToString()
        {
            // WARNING should we 'ToString' this?
            return Hex.ToString(_value.AsSpan());
        }

        public virtual bool Equals(PrivateKey? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return _value.SequenceEqual(other._value);
        }

        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || Equals(obj as PrivateKey);

        public override int GetHashCode()
        {
            if (_value.Length == 0)
                return 0;

            return _value[0] | (_value[1] << 8) | (_value[2] << 16) | (_value[3] << 24);
        }
    }
}