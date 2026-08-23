using System;

namespace Lyn.Types.Fundamental
{
    public class MiliSatoshis : IEquatable<MiliSatoshis>
    {
        private readonly ulong _value;

        public MiliSatoshis(ulong value)
        {
            _value = value;
        }

        public static implicit operator ulong(MiliSatoshis sats) => sats._value;

        public static implicit operator MiliSatoshis(ulong sats) => new MiliSatoshis(sats);

        public static implicit operator Satoshis(MiliSatoshis msats) => new Satoshis(msats._value / 1000);

        public override string ToString() => $"msats={_value}({(Satoshis)this})";

        public bool Equals(MiliSatoshis? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return _value == other._value;
        }

        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || Equals(obj as MiliSatoshis);

        public override int GetHashCode() => _value.GetHashCode();
    }
}
