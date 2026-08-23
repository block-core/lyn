using System;

namespace Lyn.Protocol.Bolt11
{
    /// <summary>
    /// Bech32 encoding as defined by BIP-173, used by the BOLT 11 invoice format.
    /// Note: invoices may exceed the 90 character limit of BIP-173 and mixed case is rejected.
    /// </summary>
    public static class Bech32
    {
        public const string CHARSET = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";

        private static readonly int[] Generator =
        {
            0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3
        };

        private static int Polymod(ReadOnlySpan<int> values)
        {
            int chk = 1;

            foreach (int value in values)
            {
                int top = chk >> 25;
                chk = (chk & 0x1ffffff) << 5 ^ value;

                for (int i = 0; i < 5; i++)
                {
                    if (((top >> i) & 1) == 1)
                        chk ^= Generator[i];
                }
            }

            return chk;
        }

        private static int[] HrpExpand(string hrp)
        {
            var result = new int[hrp.Length * 2 + 1];

            for (int i = 0; i < hrp.Length; i++)
                result[i] = hrp[i] >> 5;

            result[hrp.Length] = 0;

            for (int i = 0; i < hrp.Length; i++)
                result[hrp.Length + 1 + i] = hrp[i] & 31;

            return result;
        }

        private static int[] CreateChecksum(string hrp, ReadOnlySpan<int> data)
        {
            var values = new int[HrpExpand(hrp).Length + data.Length + 6];

            HrpExpand(hrp).CopyTo(values, 0);

            int offset = HrpExpand(hrp).Length;
            for (int i = 0; i < data.Length; i++)
                values[offset + i] = data[i];
            offset += data.Length;

            for (int i = 0; i < 6; i++)
                values[offset + i] = 0;

            int polymod = Polymod(values) ^ 1;

            var result = new int[6];
            for (int i = 0; i < 6; i++)
                result[i] = polymod >> 5 * (5 - i) & 31;

            return result;
        }

        public static bool VerifyChecksum(string hrp, ReadOnlySpan<int> data)
        {
            var expand = HrpExpand(hrp);
            var values = new int[expand.Length + data.Length];

            expand.CopyTo(values, 0);
            for (int i = 0; i < data.Length; i++)
                values[expand.Length + i] = data[i];

            return Polymod(values) == 1;
        }

        /// <summary>
        /// Encodes 5-bit data with the human readable part into a bech32 string.
        /// </summary>
        public static string Encode(string hrp, int[] data)
        {
            int[] combined = new int[data.Length + 6];

            data.CopyTo(combined, 0);
            CreateChecksum(hrp, data).CopyTo(combined, data.Length);

            var result = new System.Text.StringBuilder(hrp);
            result.Append('1');

            foreach (int value in combined)
                result.Append(CHARSET[value]);

            return result.ToString();
        }

        /// <summary>
        /// Decodes a bech32 string into the hrp and 5-bit data values.
        /// </summary>
        /// <exception cref="Bech32Exception">thrown when the string is malformed</exception>
        public static (string Hrp, int[] Data) Decode(string bechString)
        {
            if (string.IsNullOrEmpty(bechString))
                throw new Bech32Exception("Empty bech32 string");

            bool hasLower = false, hasUpper = false;

            foreach (char c in bechString)
            {
                if (c < 33 || c > 126)
                    throw new Bech32Exception($"Invalid character in bech32 string: {(int)c}");

                hasLower |= char.IsLower(c);
                hasUpper |= char.IsUpper(c);
            }

            if (hasLower && hasUpper)
                throw new Bech32Exception("Mixed case bech32 string");

            bechString = bechString.ToLowerInvariant();

            int position = bechString.LastIndexOf('1');

            if (position < 1 || position + 7 > bechString.Length)
                throw new Bech32Exception("Invalid bech32 separator position");

            string hrp = bechString.Substring(0, position);
            string dataPart = bechString.Substring(position + 1);

            var data = new int[dataPart.Length];

            for (int i = 0; i < dataPart.Length; i++)
            {
                int value = CHARSET.IndexOf(dataPart[i]);

                if (value == -1)
                    throw new Bech32Exception($"Invalid character in bech32 data: {dataPart[i]}");

                data[i] = value;
            }

            if (!VerifyChecksum(hrp, data))
                throw new Bech32Exception("Invalid bech32 checksum");

            return (hrp, data.AsSpan(0, data.Length - 6).ToArray());
        }

        /// <summary>
        /// General power-of-two base conversion between 5-bit and 8-bit groups.
        /// </summary>
        /// <summary>
        /// Converts 5 bit groups to bytes. Tagged field data is truncated at the last whole
        /// byte; set <paramref name="keepPartialByte"/> to include a final zero padded byte,
        /// which is required when rebuilding the signed message of an invoice.
        /// </summary>
        public static byte[] ConvertBitsToBytes(int[] data, int fromBits, int toBits, bool keepPartialByte = false)
        {
            int acc = 0, bits = 0;
            int maxv = (1 << toBits) - 1;
            var result = new System.Collections.Generic.List<byte>();

            foreach (int value in data)
            {
                if (value < 0 || (value >> fromBits) != 0)
                    throw new Bech32Exception("Invalid value in conversion");

                acc = (acc << fromBits) | value;
                bits += fromBits;

                while (bits >= toBits)
                {
                    bits -= toBits;
                    result.Add((byte)(acc >> bits & maxv));
                }
            }

            // Any remaining bits are padding, they must be zero. They are only emitted
            // as a final zero padded byte when requested.
            if ((acc & (1 << bits) - 1) != 0)
                throw new Bech32Exception("Invalid padding in conversion");

            if (bits > 0 && keepPartialByte)
                result.Add((byte)(acc << toBits - bits));

            return result.ToArray();
        }

        public static int[] ConvertBytesToBits(byte[] data, int fromBits, int toBits)
        {
            int acc = 0, bits = 0;
            int maxv = (1 << toBits) - 1;
            var result = new System.Collections.Generic.List<int>();

            foreach (byte value in data)
            {
                acc = (acc << fromBits) | value;
                bits += fromBits;

                while (bits >= toBits)
                {
                    bits -= toBits;
                    result.Add(acc >> bits & maxv);
                }
            }

            // Append zero padded bits to fill the final group.
            if (bits > 0)
                result.Add(acc << toBits - bits & maxv);

            return result.ToArray();
        }
    }

    public class Bech32Exception : Exception
    {
        public Bech32Exception(string message) : base(message)
        {
        }
    }
}
