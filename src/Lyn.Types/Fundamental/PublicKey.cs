using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Lyn.Types.Fundamental
{
    public class PublicKey : IEquatable<PublicKey>
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

        public override int GetHashCode()
        {
            return MemoryMarshal.Cast<byte,int>(_value)[0]; //we only need to identify if they are NOT equal at this point so returning the first 4 bytes can do that (if hash codes are equal the method Equal is called).
        }
    }
}