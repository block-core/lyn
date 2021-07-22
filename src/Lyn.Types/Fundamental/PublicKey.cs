using System;
using System.Collections.Generic;
using System.Linq;

namespace Lyn.Types.Fundamental
{
    public class PublicKey : IEquatable<PublicKey>, IEqualityComparer<PublicKey>
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

        public static implicit operator PublicKey(byte[] bytes) => new(bytes);

        public static implicit operator PublicKey(ReadOnlySpan<byte> bytes) => new(bytes.ToArray());

        public override string ToString()
        {
            return Hex.ToString(_value.AsSpan());
        }

        public bool Equals(PublicKey? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _value.SequenceEqual(other._value);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PublicKey)obj);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public bool Equals(PublicKey x, PublicKey y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x._value.Equals(y._value);
        }

        public int GetHashCode(PublicKey obj)
        {
            return obj._value.GetHashCode();
        }
    }
}