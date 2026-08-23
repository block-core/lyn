using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;
using NBitcoin.Secp256k1;

namespace Lyn.Protocol.Bolt11
{
    /// <summary>
    /// Parses and verifies BOLT 11 payment requests.
    /// </summary>
    public class Bolt11InvoiceParser
    {
        private static readonly string[] KnownPrefixes =
        {
            "lnbcrt", "lntbs", "lntb", "lnbc"
        };

        // Tagged field types.
        private const int TYPE_P = 1;   // payment hash
        private const int TYPE_D = 13;  // description
        private const int TYPE_H = 23;  // description hash
        private const int TYPE_N = 19;  // payee node id
        private const int TYPE_X = 6;   // expiry
        private const int TYPE_C = 24;  // min final cltv expiry delta
        private const int TYPE_F = 9;   // fallback on-chain address
        private const int TYPE_R = 3;   // route hint
        private const int TYPE_9 = 5;   // features
        private const int TYPE_S = 16;  // payment secret
        private const int TYPE_M = 27;  // payment metadata

        public static Bolt11Invoice Parse(string invoice)
        {
            (string hrp, int[] data) = Bech32.Decode(invoice);

            string? prefix = GetPrefix(hrp);

            if (prefix is null)
                throw new Bolt11ParseException("Unknown invoice prefix");

            MiliSatoshis? amountMsat = ParseAmount(hrp.Substring(prefix.Length));

            if (data.Length < 7 + 104)
                throw new Bolt11ParseException("Invoice data too short");

            long timestamp = ReadIntBE(data, 0, 7);

            // The signature occupies the last 104 five bit values.
            int signatureStart = data.Length - 104;
            int[] signatureValues = data.Skip(signatureStart).ToArray();

            byte[] signatureBytes = Bech32.ConvertBitsToBytes(signatureValues, 5, 8);
            byte recoveryId = signatureBytes[^1];

            if (!SecpRecoverableECDSASignature.TryCreateFromCompact(
                    signatureBytes.AsSpan(0, 64).ToArray(), recoveryId,
                    out SecpRecoverableECDSASignature? recoverableSignature) || recoverableSignature is null)
                throw new Bolt11ParseException("Invalid invoice signature");

            // The signed message is the hrp followed by the timestamp and all tagged fields
            // before the signature, converted from 5 bit groups to bytes including the zero
            // padding the writer added to reach a byte boundary.
            byte[] signedData = Encoding.UTF8.GetBytes(hrp)
                .Concat(Bech32.ConvertBitsToBytes(
                    data.Take(signatureStart).ToArray(), 5, 8, keepPartialByte: true)).ToArray();

            byte[] messageHash = System.Security.Cryptography.SHA256.HashData(signedData);

            if (!ECPubKey.TryRecover(Context.Instance, recoverableSignature, messageHash, out ECPubKey? recoveredPubKey)
                || recoveredPubKey is null)
                throw new Bolt11ParseException("Signature is not recoverable");

            var result = new Bolt11Invoice
            {
                Prefix = prefix,
                AmountMsat = amountMsat,
                Timestamp = timestamp,
                RawInvoice = invoice
            };

            bool hasExplicitPayee = ParseTaggedFields(result, data, signatureStart);

            if (hasExplicitPayee)
            {
                // When the n field is present the signature must be low-S compliant.
                if (!recoverableSignature.ToSignature().TryNormalize(out _))
                    throw new Bolt11ParseException("Invoice signature is not low-S compliant");
            }
            else
            {
                var recoveredBytes = new byte[33];
                recoveredPubKey.WriteToSpan(true, recoveredBytes, out _);
                result.PayeeNodeId = new PublicKey(recoveredBytes);
            }

            return result;
        }

        /// <returns>true when an explicit n field was present</returns>
        private static bool ParseTaggedFields(Bolt11Invoice invoice, int[] values, int signatureStart)
        {
            bool hasDescription = false, hasDescriptionHash = false, hasExplicitPayee = false;

            // The tagged fields region ends at the start of the signature, writers may have
            // appended zero padding to reach a byte boundary after the last tag.
            const int MIN_TAG_VALUES = 3;

            int position = 7;

            while (position < signatureStart)
            {
                if (position + MIN_TAG_VALUES > signatureStart)
                {
                    // Only trailing padding can remain, it must be all zeros.
                    for (int i = position; i < signatureStart; i++)
                    {
                        if (values[i] != 0)
                            throw new Bolt11ParseException("Invalid trailing data before signature");
                    }

                    break;
                }

                int type = values[position];
                int length = (values[position + 1] << 5) | values[position + 2];
                position += 3;

                if (position + length > signatureStart)
                    throw new Bolt11ParseException("Tagged field extends beyond invoice");

                int[] data = values.Skip(position).Take(length).ToArray();
                position += length;

                switch (type)
                {                    case TYPE_P:
                        byte[] paymentHash = Bech32.ConvertBitsToBytes(data, 5, 8);

                        if (paymentHash.Length != 32)
                            throw new Bolt11ParseException("Invalid payment_hash length");

                        invoice.PaymentHash = new UInt256(paymentHash);
                        break;

                    case TYPE_S:
                        byte[] secret = Bech32.ConvertBitsToBytes(data, 5, 8);

                        if (secret.Length != 32)
                            throw new Bolt11ParseException("Invalid payment_secret length");

                        invoice.PaymentSecret = new Secret(secret);
                        break;

                    case TYPE_D:
                        invoice.Description = Encoding.UTF8.GetString(
                            Bech32.ConvertBitsToBytes(data, 5, 8));
                        hasDescription = true;
                        break;

                    case TYPE_H:
                        byte[] hash = Bech32.ConvertBitsToBytes(data, 5, 8);

                        if (hash.Length != 32)
                            throw new Bolt11ParseException("Invalid description hash length");

                        invoice.DescriptionHash = new UInt256(hash);
                        hasDescriptionHash = true;
                        break;

                    case TYPE_N:
                        byte[] nodeId = Bech32.ConvertBitsToBytes(data, 5, 8);

                        if (nodeId.Length != 33)
                            throw new Bolt11ParseException("Invalid payee node id length");

                        invoice.PayeeNodeId = new PublicKey(nodeId);
                        hasExplicitPayee = true;
                        break;

                    case TYPE_X:
                        invoice.Expiry = ReadIntBE(data, 0, data.Length);
                        break;

                    case TYPE_C:
                        invoice.MinFinalCltvExpiry = (ushort)ReadIntBE(data, 0, data.Length);
                        break;

                    case TYPE_F:
                        ParseFallbackAddress(invoice, data);
                        break;

                    case TYPE_R:
                        ParseRouteHint(invoice, data);
                        break;

                    case TYPE_9:
                        invoice.Features = WriteMinimalBigEndian(ReadIntBE(data, 0, data.Length));
                        break;

                    case TYPE_M:
                        invoice.PaymentMetadata = Bech32.ConvertBitsToBytes(data, 5, 8);
                        break;

                    default:
                        // Unknown fields MUST be skipped.
                        break;
                }
            }

            if (hasDescription == hasDescriptionHash)
                throw new Bolt11ParseException("Exactly one of description or description hash must be present");

            return hasExplicitPayee;
        }

        private static void ParseFallbackAddress(Bolt11Invoice invoice, int[] data)
        {
            byte version = (byte)data[0];
            byte[] address = Bech32.ConvertBitsToBytes(data.Skip(1).ToArray(), 5, 8);

            switch (version)
            {
                case 0:
                    if (address.Length != 20 && address.Length != 32)
                        throw new Bolt11ParseException("Invalid witness program length");

                    break;

                case 17:
                    if (address.Length != 20)
                        throw new Bolt11ParseException("Invalid P2PKH address");

                    break;

                case 18:
                    if (address.Length != 20)
                        throw new Bolt11ParseException("Invalid P2SH address");

                    break;

                default:
                    // Unknown versions must be skipped by readers.
                    return;
            }

            invoice.FallbackAddresses.Add(new Bolt11FallbackAddress { Version = version, Address = address });
        }

        private static void ParseRouteHint(Bolt11Invoice invoice, int[] data)
        {
            byte[] bytes = Bech32.ConvertBitsToBytes(data, 5, 8);

            const int entrySize = 51;

            if (bytes.Length == 0 || bytes.Length % entrySize != 0)
                throw new Bolt11ParseException("Invalid route hint length");

            for (int offset = 0; offset < bytes.Length; offset += entrySize)
            {
                var hint = new Bolt11RouteHint
                {
                    NodeId = new PublicKey(bytes.AsSpan(offset, 33).ToArray()),
                    ShortChannelId = ShortChannelId.FromUInt64(ReadUInt64BE(bytes, offset + 33)),
                    FeeBaseMsat = ReadUInt32BE(bytes, offset + 41),
                    FeeProportionalMillionths = ReadUInt32BE(bytes, offset + 45),
                    CltvExpiryDelta = (ushort)((bytes[offset + 49] << 8) | bytes[offset + 50])
                };

                invoice.RouteHints.Add(new List<Bolt11RouteHint> { hint });
            }
        }

        private static string? GetPrefix(string hrp)
        {
            foreach (string prefix in KnownPrefixes.OrderByDescending(k => k.Length))
            {
                if (hrp.StartsWith(prefix))
                    return prefix;
            }

            return null;
        }

        public static MiliSatoshis? ParseAmount(string amount)
        {
            if (string.IsNullOrEmpty(amount))
                return null;

            char multiplierChar = amount[^1];

            // Exponent applied to the digits to obtain picosatoshi (1e-12 BTC).
            int picosatExponent = multiplierChar switch
            {
                'm' => 9,
                'u' => 6,
                'n' => 3,
                'p' => 0,
                _ => char.IsDigit(multiplierChar) ? 12 : -1
            };

            if (picosatExponent < 0)
                throw new Bolt11ParseException("Invalid invoice amount multiplier");

            string digits = char.IsDigit(multiplierChar)
                ? amount
                : amount.Substring(0, amount.Length - 1);

            if (digits.Length == 0 || digits.Any(c => c < '0' || c > '9'))
                throw new Bolt11ParseException("Invalid invoice amount");

            // Sub-millisatoshi precision is not possible, the p multiplier requires a trailing zero.
            if (multiplierChar == 'p' && digits[^1] != '0')
                throw new Bolt11ParseException("Sub-millisatoshi precision not supported");

            var picosatoshis = System.Numerics.BigInteger.Parse(digits);

            for (int i = 0; i < picosatExponent; i++)
                picosatoshis *= 10;

            // One millisatoshi equals ten picosatoshi.
            if (picosatoshis % 10 != 0)
                throw new Bolt11ParseException("Sub-millisatoshi precision not supported");

            return new MiliSatoshis((ulong)(picosatoshis / 10));
        }

        private static long ReadIntBE(int[] data, int offset, int count)
        {
            long result = 0;

            for (int i = 0; i < count; i++)
                result = (result << 5) | data[offset + i];

            return result;
        }

        private static ulong ReadUInt64BE(byte[] data, int offset)
        {
            ulong result = 0;

            for (int i = 0; i < 8; i++)
                result = (result << 8) | data[offset + i];

            return result;
        }

        private static uint ReadUInt32BE(byte[] data, int offset)
        {
            return (uint)((data[offset] << 24) | (data[offset + 1] << 16)
                          | (data[offset + 2] << 8) | data[offset + 3]);
        }

        private static byte[] WriteMinimalBigEndian(long value)
        {
            var temp = new List<byte>();

            do
            {
                temp.Insert(0, (byte)(value & 0xFF));
                value >>= 8;
            } while (value > 0);

            return temp.ToArray();
        }
    }

    public class Bolt11ParseException : Exception
    {
        public Bolt11ParseException(string message) : base(message)
        {
        }
    }
}
