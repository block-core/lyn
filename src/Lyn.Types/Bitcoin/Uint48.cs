using System;
using System.Runtime.InteropServices;

namespace Lyn.Types.Bitcoin
{
    [StructLayout(LayoutKind.Sequential)]
    public partial class Uint48 : IEquatable<Uint48>
    {
        protected const int EXPECTED_SIZE = 6;

        protected ushort part1;
        protected ushort part2;
        protected ushort part3;

        protected Uint48()
        {
        }

        public Uint48(ReadOnlySpan<byte> input)
        {
            if (input.Length != EXPECTED_SIZE)
            {
                ThrowHelper.ThrowFormatException("the byte array should be 32 bytes long");
            }

            // TODO: fix when moving to dotnet5
            // Span<byte> dst = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, byte>(ref part1), EXPECTED_SIZE);
            //input.CopyTo(dst);

            var uints = MemoryMarshal.Cast<byte, ushort>(input);
            part1 = uints[0];
            part2 = uints[1];
            part3 = uints[2];
        }

        /// <summary>
        /// Converts to string in hexadecimal format.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Hex.ToString(GetBytes());

            //return string.Create(EXPECTED_SIZE * 2, this, (dst, src) =>
            //{
            //   ReadOnlySpan<byte> rawData = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref src.part1, EXPECTED_SIZE / sizeof(ushort)));

            //   const string hexValues = "0123456789ABCDEF";

            //   int i = rawData.Length - 1;
            //   int j = 0;

            //   while (i >= 0)
            //   {
            //      byte b = rawData[i--];
            //      dst[j++] = hexValues[b >> 4];
            //      dst[j++] = hexValues[b & 0xF];
            //   }
            //});
        }

        public ReadOnlySpan<byte> GetBytes()
        {
            // TODO: fix when moving to dotnet5
            // return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, byte>(ref part1), EXPECTED_SIZE);

            Span<ushort> temp = stackalloc ushort[3];
            temp[0] = part1;
            temp[1] = part2;
            temp[2] = part3;
            Span<byte> temp2 = MemoryMarshal.Cast<ushort, byte>(temp);

            return temp2.ToArray();
        }

        public override int GetHashCode()
        {
            return (int)part1;
        }

        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || Equals(obj as Uint48);

        public static bool operator !=(Uint48? a, Uint48? b) => !(a == b);

        public static bool operator ==(Uint48? a, Uint48? b) => ReferenceEquals(a, b) || (a?.Equals(b) ?? false);

        public bool Equals(Uint48? other)
        {
            if (other is null) return false;

            return part1 == other.part1
                   && part2 == other.part2
                   && part3 == other.part3;
        }

        public static implicit operator Uint48(uint num)
        {
            Span<byte> output = stackalloc byte[6];
            var conv = BitConverter.GetBytes(num).AsSpan();
            conv.CopyTo(output);
            output.Reverse();
            return new Uint48(output);
        }

        public static implicit operator uint(Uint48 num)
        {
            if (num is null) throw new ArgumentNullException(nameof(num));

            ReadOnlySpan<byte> outputnum = num.GetBytes();
            Span<byte> output = stackalloc byte[6];
            outputnum.CopyTo(output);
            output.Reverse();
            uint conv = BitConverter.ToUInt32(output.Slice(0, 4));

            return conv;
        }

        public static Uint48 operator ^(Uint48? a, Uint48? b)
        {
            if (a is null) throw new ArgumentNullException(nameof(a));
            if (b is null) throw new ArgumentNullException(nameof(b));

            Uint48 ret = new();
            ret.part1 = (ushort)(a.part1 ^ b.part1);
            ret.part2 = (ushort)(a.part2 ^ b.part2);
            ret.part3 = (ushort)(a.part3 ^ b.part3);

            return ret;
        }

        public static Uint48 operator &(Uint48? a, Uint48? b)
        {
            if (a is null) throw new ArgumentNullException(nameof(a));
            if (b is null) throw new ArgumentNullException(nameof(b));

            Uint48 ret = new();
            ret.part1 = (ushort)(a.part1 & b.part1);
            ret.part2 = (ushort)(a.part2 & b.part2);
            ret.part3 = (ushort)(a.part3 & b.part3);

            return ret;
        }

        //public static Uint48 operator >>(Uint48? a, Uint48? b)
        //{
        //   if (a is null) throw new ArgumentNullException(nameof(a));
        //   if (b is null) throw new ArgumentNullException(nameof(b));

        //   Uint48 ret = new();
        //   ret.part1 = (ushort)(a.part1 ^ b.part1);
        //   ret.part2 = (ushort)(a.part2 ^ b.part2);
        //   ret.part3 = (ushort)(a.part3 ^ b.part3);

        //   return ret;
        //}

        public static bool operator <(Uint48? a, Uint48? b)
        {
            return Compare(a, b) < 0;
        }

        public static bool operator >(Uint48? a, Uint48? b)
        {
            return Compare(a, b) > 0;
        }

        public static bool operator <=(Uint48? a, Uint48? b)
        {
            return Compare(a, b) <= 0;
        }

        public static bool operator >=(Uint48? a, Uint48? b)
        {
            return Compare(a, b) >= 0;
        }

        public static int Compare(Uint48? a, Uint48? b)
        {
            if (a is null) throw new ArgumentNullException(nameof(a));
            if (b is null) throw new ArgumentNullException(nameof(b));

            if (a.part3 < b.part3)
                return -1;
            if (a.part3 > b.part3)
                return 1;
            if (a.part2 < b.part2)
                return -1;
            if (a.part2 > b.part2)
                return 1;
            if (a.part1 < b.part1)
                return -1;
            if (a.part1 > b.part1)
                return 1;

            return 0;
        }
    }
}