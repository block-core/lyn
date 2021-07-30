using System;
using System.Runtime.InteropServices;

namespace Lyn.Types.Bitcoin
{
    [StructLayout(LayoutKind.Sequential)]
    public partial class UInt256 : IEquatable<UInt256>
    {
        protected const int EXPECTED_SIZE = 32;

        public static UInt256 Zero { get; } = new UInt256("0".PadRight(EXPECTED_SIZE * 2, '0'));

        protected ulong part1;
        protected ulong part2;
        protected ulong part3;
        protected ulong part4;

        /// <summary>
        /// Initializes a new instance of the <see cref="UInt256"/> class.
        /// Used by derived classes.
        /// </summary>
        protected UInt256() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UInt256"/>, expect data in Little Endian.
        /// </summary>
        /// <param name="data">The data.</param>
        public UInt256(ReadOnlySpan<byte> input)
        {
            if (input.Length != EXPECTED_SIZE)
            {
                ThrowHelper.ThrowFormatException("the byte array should be 32 bytes long");
            }

            // TODO: fix when moving to dotnet5
            // Span<byte> dst = MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, byte>(ref part1), EXPECTED_SIZE);
            //input.CopyTo(dst);

            var uints = MemoryMarshal.Cast<byte, ulong>(input);
            part1 = uints[0];
            part2 = uints[1];
            part3 = uints[2];
            part4 = uints[3];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UInt256"/> class.
        /// Passed hex string must be a valid hex string with 64 char length, or 66 if prefix 0x is used, otherwise an exception is thrown.
        /// Input data is considered in big endian.
        /// </summary>
        public UInt256(string hexString)
        {
            if (hexString is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(hexString));
            }

            //account for 0x prefix
            if (hexString.Length < EXPECTED_SIZE * 2)
            {
                ThrowHelper.ThrowFormatException($"the hex string should be {EXPECTED_SIZE * 2} chars long or {(EXPECTED_SIZE * 2) + 4} if prefixed with 0x.");
            }

            ReadOnlySpan<char> hexAsSpan = (hexString[0] == '0' && (hexString[1] == 'X' || hexString[1] == 'x')) ? hexString.AsSpan(2) : hexString.AsSpan();

            if (hexAsSpan.Length != EXPECTED_SIZE * 2)
            {
                ThrowHelper.ThrowFormatException($"the hex string should be {EXPECTED_SIZE * 2} chars long or {(EXPECTED_SIZE * 2) + 4} if prefixed with 0x.");
            }

            Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref part1, EXPECTED_SIZE / sizeof(ulong)));

            int i = hexAsSpan.Length - 1;
            int j = 0;

            while (i > 0)
            {
                char c = hexAsSpan[i--];
                if (c >= '0' && c <= '9')
                {
                    dst[j] = (byte)(c - '0');
                }
                else if (c >= 'a' && c <= 'f')
                {
                    dst[j] = (byte)(c - ('a' - 10));
                }
                else if (c >= 'A' && c <= 'F')
                {
                    dst[j] = (byte)(c - ('A' - 10));
                }
                else
                {
                    ThrowHelper.ThrowArgumentException("Invalid nibble: " + c);
                }

                c = hexAsSpan[i--];
                if (c >= '0' && c <= '9')
                {
                    dst[j] |= (byte)((c - '0') << 4);
                }
                else if (c >= 'a' && c <= 'f')
                {
                    dst[j] |= (byte)((c - ('a' - 10)) << 4);
                }
                else if (c >= 'A' && c <= 'F')
                {
                    dst[j] |= (byte)((c - ('A' - 10)) << 4);
                }
                else
                {
                    ThrowHelper.ThrowArgumentException("Invalid nibble: " + c);
                }

                j++;
            }
        }

        /// <summary>
        /// Converts to string in hexadecimal format.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Create(EXPECTED_SIZE * 2, this, (dst, src) =>
            {
                ReadOnlySpan<byte> rawData = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref src.part1, EXPECTED_SIZE / sizeof(ulong)));

                const string hexValues = "0123456789abcdef"; // "0123456789ABCDEF";

                int i = rawData.Length - 1;
                int j = 0;

                while (i >= 0)
                {
                    byte b = rawData[i--];
                    dst[j++] = hexValues[b >> 4];
                    dst[j++] = hexValues[b & 0xF];
                }
            });
        }

        /// <summary>
        /// Try to parse from an hex string to UInt256.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public static bool TryParse(string hexString, out UInt256? result)
        {
            try
            {
                result = new UInt256(hexString);
                return true;
            }
            catch (Exception)
            {
                result = null;
            }

            return false;
        }

        /// <summary>
        /// Try to parse from an hex string to UInt256.
        /// </summary>
        /// <param name="hexString">The hexadecimal to parse.</param>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public static UInt256 Parse(string hexString)
        {
            return new UInt256(hexString);
        }

        public ReadOnlySpan<byte> GetBytes()
        {
            // TODO: fix when moving to dotnet5
            // return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ulong, byte>(ref part1), EXPECTED_SIZE);

            Span<ulong> temp = stackalloc ulong[4];
            temp[0] = part1;
            temp[1] = part2;
            temp[2] = part3;
            temp[3] = part4;

            Span<byte> arrAsBytes = MemoryMarshal.Cast<ulong, byte>(temp);

            return arrAsBytes.ToArray();
        }

        public override int GetHashCode()
        {
            return (int)part1;
        }

        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || Equals(obj as UInt256);

        public static bool operator !=(UInt256? a, UInt256? b) => !(a == b);

        public static bool operator ==(UInt256? a, UInt256? b) => ReferenceEquals(a, b) || (a?.Equals(b) ?? false);

        public bool Equals(UInt256? other)
        {
            if (other is null) return false;

            return part1 == other.part1
                   && part2 == other.part2
                   && part3 == other.part3
                   && part4 == other.part4;
        }

        public static bool operator <(UInt256? a, UInt256? b)
        {
            return Compare(a, b) < 0;
        }

        public static bool operator >(UInt256? a, UInt256? b)
        {
            return Compare(a, b) > 0;
        }

        public static bool operator <=(UInt256? a, UInt256? b)
        {
            return Compare(a, b) <= 0;
        }

        public static bool operator >=(UInt256? a, UInt256? b)
        {
            return Compare(a, b) >= 0;
        }

        public static int Compare(UInt256? a, UInt256? b)
        {
            if (a is null) throw new ArgumentNullException(nameof(a));
            if (b is null) throw new ArgumentNullException(nameof(b));

            if (a.part4 < b.part4)
                return -1;
            if (a.part4 > b.part4)
                return 1;
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