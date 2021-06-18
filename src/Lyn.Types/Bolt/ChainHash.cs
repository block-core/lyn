using System;
using Lyn.Types.Bitcoin;

namespace Lyn.Types.Bolt
{
    public class ChainHash : IEquatable<ChainHash>
    {
        private readonly UInt256 _value;

        public ChainHash(byte[] value)
        {
            _value = new UInt256(value);
        }

        public ChainHash(ReadOnlySpan<byte> value)
        {
            _value = new UInt256(value);
        }

        public static implicit operator byte[](ChainHash hash) => hash._value.GetBytes().ToArray(); //TODO David validate that getbytes returns the right array

        public static explicit operator ChainHash(byte[] bytes) => new ChainHash(bytes);

        public static explicit operator ChainHash(ReadOnlySpan<byte> bytes) => new ChainHash(bytes.ToArray());

        public static implicit operator UInt256(ChainHash chainHash) => chainHash._value;

        public static implicit operator ChainHash(UInt256 uInt256) => new ChainHash(uInt256.GetBytes());

        public bool Equals(ChainHash? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _value.Equals(other._value);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ChainHash)obj);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }
    }
}