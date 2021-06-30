namespace Lyn.Types.Fundamental
{
    public class Satoshis
    {
        private ulong _value;

        public Satoshis(ulong value)
        {
            _value = value;
        }

        public static implicit operator ulong(Satoshis sats) => sats._value;

        public static implicit operator Satoshis(ulong sats) => new Satoshis(sats);

        public static implicit operator long(Satoshis sats) => (long)sats._value;

        public static implicit operator Satoshis(long sats) => new Satoshis((ulong)sats);

        public static implicit operator int(Satoshis sats) => (int)sats._value;

        public static implicit operator Satoshis(int sats) => new Satoshis((ulong)sats);

        public static implicit operator uint(Satoshis sats) => (uint)sats._value;

        public static implicit operator Satoshis(uint sats) => new Satoshis((ulong)sats);

        public static implicit operator MiliSatoshis(Satoshis sats) => new MiliSatoshis(sats._value * 1000);

        public override string ToString() => $@"sats={_value}";
    }
}