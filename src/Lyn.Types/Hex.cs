using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lyn.Types
{
    public static class Hex
    {
        public static byte[] FromString(string hex)
        {
            if (string.IsNullOrEmpty(hex)) 
                return Array.Empty<byte>();

            var startIndex = hex.StartsWith("0x") || hex.StartsWith("0X") ? 2 : 0;

            return Convert.FromHexString(hex[startIndex..]);
        }

        public static string ToString(ReadOnlySpan<byte> span)
        {
            return Convert.ToHexString(span).ToLower();
        }

        public static string ToString(Span<byte> span)
        {
            return Convert.ToHexString(span).ToLower();
        }

        public static string ToString(IEnumerable<byte> arr)
        {
            return Convert.ToHexString(arr.ToArray()).ToLower();
        }
        
        public static string ToString(byte[] arr)
        {
            return Convert.ToHexString(arr).ToLower();
        }
    }
}