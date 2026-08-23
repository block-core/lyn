using System;
using System.Collections.Generic;
using System.Linq;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Bolt4
{
    /// <summary>
    /// A single TLV record of the BOLT 4 payload tlv_stream.
    /// </summary>
    public class HopTlvRecord
    {
        public ulong Type { get; set; }

        public byte[] Value { get; set; } = new byte[0];
    }

    public static class HopPayload
    {
        // TLV types defined by BOLT 4 payload format.
        public const ulong AMT_TO_FORWARD = 2;
        public const ulong OUTGOING_CLTV_VALUE = 4;
        public const ulong SHORT_CHANNEL_ID = 6;
        public const ulong PAYMENT_DATA = 8;
        public const ulong ENCRYPTED_RECIPIENT_DATA = 10;
        public const ulong CURRENT_PATH_KEY = 12;
        public const ulong PAYMENT_METADATA = 16;
        public const ulong TOTAL_AMOUNT_MSAT = 18;

        /// <summary>
        /// Builds the payload for a forwarding hop.
        /// </summary>
        public static byte[] BuildForwardingPayload(ulong amtToForward, uint outgoingCltvValue, ShortChannelId shortChannelId)
        {
            var records = new List<HopTlvRecord>
            {
                new() { Type = AMT_TO_FORWARD, Value = WriteTu64(amtToForward) },
                new() { Type = OUTGOING_CLTV_VALUE, Value = WriteTu32(outgoingCltvValue) },
                new() { Type = SHORT_CHANNEL_ID, Value = WriteScid(shortChannelId) }
            };

            return Encode(records);
        }

        /// <summary>
        /// Builds the payload for a forwarding hop inside a blinded route.
        /// </summary>
        public static byte[] BuildBlindedForwardingPayload(byte[] encryptedRecipientData)
        {
            var records = new List<HopTlvRecord>
            {
                new() { Type = ENCRYPTED_RECIPIENT_DATA, Value = encryptedRecipientData }
            };

            return Encode(records);
        }

        /// <summary>
        /// Builds the final payload including the payment data required by BOLT 4 when a
        /// payment secret is used.
        /// </summary>
        public static byte[] BuildFinalPayload(
            ulong amtToForward,
            uint outgoingCltvValue,
            byte[]? paymentSecret = null,
            ulong? totalMsat = null,
            byte[]? paymentMetadata = null)
        {
            if (paymentSecret != null && totalMsat == null)
                throw new ArgumentException("total_msat is required when a payment_secret is provided");

            var records = new List<HopTlvRecord>
            {
                new() { Type = AMT_TO_FORWARD, Value = WriteTu64(amtToForward) },
                new() { Type = OUTGOING_CLTV_VALUE, Value = WriteTu32(outgoingCltvValue) }
            };

            if (paymentSecret != null)
            {
                if (paymentSecret.Length != 32)
                    throw new ArgumentException("payment_secret must be 32 bytes");

                var value = new byte[32 + 8];
                paymentSecret.CopyTo(value, 0);
                WriteTu64(totalMsat!.Value).CopyTo(value, 32);

                records.Add(new HopTlvRecord { Type = PAYMENT_DATA, Value = value });
            }

            if (totalMsat != null)
                records.Add(new HopTlvRecord { Type = TOTAL_AMOUNT_MSAT, Value = WriteTu64(totalMsat.Value) });

            if (paymentMetadata != null)
                records.Add(new HopTlvRecord { Type = PAYMENT_METADATA, Value = paymentMetadata });

            return Encode(records);
        }

        /// <summary>
        /// Serializes records to the wire format: bigsize type, bigsize length, value.
        /// </summary>
        public static byte[] Encode(IReadOnlyList<HopTlvRecord> records)
        {
            int size = 0;

            foreach (HopTlvRecord record in records)
                size += OnionPacketBuilder.BigSizeLength(record.Type) +
                        OnionPacketBuilder.BigSizeLength((ulong)record.Value.Length) +
                        record.Value.Length;

            var result = new byte[size];
            int offset = 0;

            foreach (HopTlvRecord record in records.OrderBy(r => r.Type))
            {
                OnionPacketBuilder.WriteBigSize(result.AsSpan(offset), record.Type);
                offset += OnionPacketBuilder.BigSizeLength(record.Type);

                OnionPacketBuilder.WriteBigSize(result.AsSpan(offset), (ulong)record.Value.Length);
                offset += OnionPacketBuilder.BigSizeLength((ulong)record.Value.Length);

                record.Value.CopyTo(result, offset);
                offset += record.Value.Length;
            }

            return result;
        }

        /// <summary>
        /// Parses a payload into its TLV records.
        /// </summary>
        public static IReadOnlyList<HopTlvRecord> Decode(byte[] payload)
        {
            var records = new List<HopTlvRecord>();

            int position = 0;

            while (position < payload.Length)
            {
                ulong type = ReadBigSize(payload, ref position);

                if (position >= payload.Length)
                    throw new SphinxException("Truncated tlv record");

                ulong length = ReadBigSize(payload, ref position);

                if (position + (int)length > payload.Length)
                    throw new SphinxException("Truncated tlv record value");

                var value = new byte[length];
                Array.Copy(payload, position, value, 0, (int)length);
                position += (int)length;

                records.Add(new HopTlvRecord { Type = type, Value = value });
            }

            return records;
        }

        private static byte[] WriteTu64(ulong value)
        {
            var result = new byte[8];

            for (int i = 0; i < 8; i++)
                result[i] = (byte)(value >> (56 - i * 8));

            return result;
        }

        private static byte[] WriteTu32(uint value)
        {
            return new[]
            {
                (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value
            };
        }

        private static byte[] WriteScid(ShortChannelId shortChannelId)
        {
            ulong value = shortChannelId.ToUInt64();

            return new[]
            {
                (byte)(value >> 56), (byte)(value >> 48), (byte)(value >> 40), (byte)(value >> 32),
                (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value
            };
        }

        private static ulong ReadBigSize(byte[] data, ref int position)
        {
            byte firstByte = data[position++];

            switch (firstByte)
            {
                case 0xFD:
                    ushort u16 = (ushort)((data[position] << 8) | data[position + 1]);
                    position += 2;
                    return u16;

                case 0xFE:
                    uint u32 = (uint)((data[position] << 24) | (data[position + 1] << 16)
                                      | (data[position + 2] << 8) | data[position + 3]);
                    position += 4;
                    return u32;

                case 0xFF:
                    ulong value = 0;
                    for (int i = 0; i < 8; i++)
                        value = (value << 8) | data[position + i];
                    position += 8;
                    return value;

                default:
                    return firstByte;
            }
        }
    }
}
