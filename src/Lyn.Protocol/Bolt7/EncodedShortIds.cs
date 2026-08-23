using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt7
{
    /// <summary>
    /// Implements the encoded_short_ids format from BOLT 7:
    /// an encoding byte (0 = uncompressed, 1 = zlib) followed by the
    /// bigsize-encoded short channel ids.
    /// </summary>
    public static class EncodedShortIds
    {
        public const byte ENCODING_UNCOMPRESSED = 0;
        public const byte ENCODING_ZLIB_DEFLATE = 1;

        public const ushort MAX_ENCODED_LENGTH = 65355;

        public static byte[] Encode(IEnumerable<ShortChannelId> shortChannelIds)
        {
            using var stream = new MemoryStream();
            stream.WriteByte(ENCODING_UNCOMPRESSED);

            foreach (ShortChannelId shortChannelId in shortChannelIds)
                WriteBigSize(stream, shortChannelId.ToUInt64());

            return stream.ToArray();
        }

        public static ShortChannelId[] Decode(byte[] encoded)
        {
            if (encoded == null || encoded.Length == 0)
                return Array.Empty<ShortChannelId>();

            byte encoding = encoded[0];

            ReadOnlySpan<byte> payload;

            switch (encoding)
            {
                case ENCODING_UNCOMPRESSED:
                    payload = encoded.AsSpan(1);
                    break;

                case ENCODING_ZLIB_DEFLATE:
                    using (var compressedStream = new MemoryStream(encoded, 1, encoded.Length - 1))
                    using (var decompressedStream = new MemoryStream())
                    {
                        using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
                        {
                            deflateStream.CopyTo(decompressedStream);
                        }

                        payload = decompressedStream.ToArray().AsSpan();
                    }

                    break;

                default:
                    throw new FormatException($"Unknown short_channel_id encoding {encoding}");
            }

            var result = new List<ShortChannelId>();

            int position = 0;

            while (position < payload.Length)
            {
                ulong scid = ReadBigSize(payload, ref position);
                result.Add(ShortChannelId.FromUInt64(scid));
            }

            return result.ToArray();
        }

        private static void WriteBigSize(Stream stream, ulong value)
        {
            if (value < 0xFD)
            {
                stream.WriteByte((byte)value);
            }
            else if (value < 0x10000)
            {
                stream.WriteByte(0xFD);
                stream.WriteByte((byte)(value >> 8));
                stream.WriteByte((byte)value);
            }
            else if (value < 0x100000000)
            {
                stream.WriteByte(0xFE);
                stream.WriteByte((byte)(value >> 24));
                stream.WriteByte((byte)(value >> 16));
                stream.WriteByte((byte)(value >> 8));
                stream.WriteByte((byte)value);
            }
            else
            {
                stream.WriteByte(0xFF);
                for (int shift = 56; shift >= 0; shift -= 8)
                    stream.WriteByte((byte)(value >> shift));
            }
        }

        private static ulong ReadBigSize(ReadOnlySpan<byte> data, ref int position)
        {
            byte firstByte = data[position++];

            if (firstByte < 0xFD)
                return firstByte;

            if (firstByte == 0xFD)
            {
                ushort value = (ushort)((data[position] << 8) | data[position + 1]);
                position += 2;
                return value;
            }

            if (firstByte == 0xFE)
            {
                uint value = (uint)((data[position] << 24) | (data[position + 1] << 16)
                                    | (data[position + 2] << 8) | data[position + 3]);
                position += 4;
                return value;
            }

            ulong longValue = 0;
            for (int i = 0; i < 8; i++)
                longValue = (longValue << 8) | data[position + i];
            position += 8;
            return longValue;
        }
    }
}
