namespace Lyn.Types.Fundamental
{
    public class Preimage : PrivateKey
    {
        public Preimage(byte[] value) : base(value)
        {
        }

        public static implicit operator byte[](Preimage hash) => hash._value;
    }
}