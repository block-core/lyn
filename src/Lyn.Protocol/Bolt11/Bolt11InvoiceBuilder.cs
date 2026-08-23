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
    /// Creates and signs BOLT 11 payment requests.
    /// </summary>
    public class Bolt11InvoiceBuilder
    {
        private const int TYPE_P = 1;
        private const int TYPE_D = 13;
        private const int TYPE_H = 23;
        private const int TYPE_N = 19;
        private const int TYPE_X = 6;
        private const int TYPE_C = 24;
        private const int TYPE_F = 9;
        private const int TYPE_R = 3;
        private const int TYPE_9 = 5;
        private const int TYPE_S = 16;
        private const int TYPE_M = 27;

        public static Bolt11Invoice Build(Bolt11InvoiceRequest request)
        {
            if (!ECPrivKey.TryCreate(request.PayeePrivateKey, Context.Instance, out ECPrivKey? privateKey) ||
                privateKey is null)
                throw new ArgumentException("Invalid payee private key", nameof(request.PayeePrivateKey));

            string hrp = request.Prefix + EncodeAmount(request.AmountMsat);

            var data = new List<int>(7);
            data.AddRange(WriteIntBE(request.Timestamp, 35));

            // Tag order follows the reference implementation: s, p, d or h, extras.
            AddTag(data, TYPE_S, ConvertBytesTo5Bit((byte[])request.PaymentSecret));
            AddTag(data, TYPE_P, ConvertBytesTo5Bit(request.PaymentHash.GetBytes().ToArray()));

            if (request.Description != null)
                AddTag(data, TYPE_D, ConvertBytesTo5Bit(Encoding.UTF8.GetBytes(request.Description)));
            else if (request.DescriptionHash != null)
                AddTag(data, TYPE_H, ConvertBytesTo5Bit(request.DescriptionHash.GetBytes().ToArray()));
            else
                throw new ArgumentException("Either a description or description hash is required");

            foreach (var fallback in request.FallbackAddresses ?? new List<Bolt11FallbackAddress>())
            {
                var value = new[] { (int)fallback.Version }
                    .Concat(ConvertBytesTo5Bit(fallback.Address));

                AddTag(data, TYPE_F, value.ToArray());
            }

            foreach (List<Bolt11RouteHint> routeHint in request.RouteHints ?? new List<List<Bolt11RouteHint>>())
                AddTag(data, TYPE_R, ConvertBytesTo5Bit(EncodeRouteHint(routeHint)));

            if (request.MinFinalCltvExpiry.HasValue && request.MinFinalCltvExpiry != 18)
                AddTag(data, TYPE_C, WriteIntBEMinimal(request.MinFinalCltvExpiry.Value));

            if (request.Expiry.HasValue && request.Expiry != 3600)
                AddTag(data, TYPE_X, WriteIntBEMinimal(request.Expiry.Value));

            if (request.PaymentMetadata != null)
                AddTag(data, TYPE_M, ConvertBytesTo5Bit(request.PaymentMetadata));

            bool includePayeeNodeKey = request.IncludePayeeNodeId == true;

            if (includePayeeNodeKey)
            {
                var payeePubKey = privateKey.CreatePubKey();
                var nodeId = new byte[33];
                payeePubKey.WriteToSpan(true, nodeId, out _);

                AddTag(data, TYPE_N, ConvertBytesTo5Bit(nodeId));
            }

            if (request.Features != null && request.Features.Length > 0 &&
                request.Features.Any(b => b != 0))
            {
                long features = 0;

                foreach (byte featureByte in request.Features)
                    features = (features << 8) | featureByte;

                AddTag(data, TYPE_9, WriteIntBEMinimal(features));
            }

            // Sign sha256(utf8(hrp) || timestamp and tagged fields), keeping the zero
            // padded final byte exactly like the reference implementation.
            byte[] signedData = Encoding.UTF8.GetBytes(hrp)
                .Concat(Bech32.ConvertBitsToBytes(data.ToArray(), 5, 8, keepPartialByte: true)).ToArray();

            byte[] messageHash = System.Security.Cryptography.SHA256.HashData(signedData);

            privateKey.TrySignRecoverable(messageHash, out SecpRecoverableECDSASignature? recoverable);

            Span<byte> compactSignature = stackalloc byte[64];
            recoverable!.WriteToSpanCompact(compactSignature, out int recoveryId);

            byte[] signature = new byte[65];
            compactSignature.CopyTo(signature);
            signature[64] = (byte)recoveryId;

            data.AddRange(ConvertBytesTo5Bit(signature));

            string encoded = Bech32.Encode(hrp, data.ToArray());

            return new Bolt11Invoice
            {
                Prefix = request.Prefix,
                AmountMsat = request.AmountMsat,
                Timestamp = request.Timestamp,
                PaymentHash = request.PaymentHash,
                PaymentSecret = request.PaymentSecret,
                Description = request.Description,
                DescriptionHash = request.DescriptionHash,
                Expiry = request.Expiry ?? 3600,
                MinFinalCltvExpiry = request.MinFinalCltvExpiry ?? 18,
                Features = request.Features ?? new byte[0],
                PaymentMetadata = request.PaymentMetadata,
                FallbackAddresses = request.FallbackAddresses ?? new List<Bolt11FallbackAddress>(),
                RouteHints = request.RouteHints ?? new List<List<Bolt11RouteHint>>(),
                PayeeNodeId = new PublicKey(privateKey.CreatePubKey().ToBytes()),
                RawInvoice = encoded
            };
        }

        /// <summary>
        /// Returns the human readable amount section, using the largest possible multiplier.
        /// </summary>
        public static string? EncodeAmount(MiliSatoshis? amountMsat)
        {
            if (amountMsat is null || (ulong)amountMsat == 0)
                return null;

            ulong msat = amountMsat;

            (string suffix, ulong divisor)[] candidates =
            {
                ("", 100_000_000_000UL), // bitcoin
                ("m", 100_000_000UL),    // millibitcoin
                ("u", 100_000UL),        // microbitcoin
                ("n", 100UL)             // nanobitcoin
            };

            foreach ((string suffix, ulong divisor) in candidates)
            {
                if (msat % divisor == 0)
                    return (msat / divisor).ToString() + suffix;
            }

            // Picobitcoin is the final fallback: 1 msat is 10 picobitcoin and the trailing
            // zero required by the p multiplier is always present in this representation.
            return (msat * 10).ToString() + "p";
        }

        private static void AddTag(List<int> data, int type, IEnumerable<int> tagData)
        {
            var tagDataArray = tagData as int[] ?? tagData.ToArray();

            data.Add(type);
            data.Add(tagDataArray.Length >> 5 & 31);
            data.Add(tagDataArray.Length & 31);
            data.AddRange(tagDataArray);
        }

        private static int[] ConvertBytesTo5Bit(byte[] bytes)
        {
            return Bech32.ConvertBytesToBits(bytes, 8, 5);
        }

        private static IEnumerable<int> WriteIntBE(long value, int bits)
        {
            var result = new List<int>();

            for (int i = bits - 5; i >= 0; i -= 5)
                result.Add((int)(value >> i & 31));

            return result;
        }

        private static IEnumerable<int> WriteIntBEMinimal(long value)
        {
            var groups = new List<int>();
            var temp = new List<int>();

            while (value > 0)
            {
                temp.Insert(0, (int)(value & 31));
                value >>= 5;
            }

            groups.AddRange(temp);
            return groups;
        }

        private static byte[] EncodeRouteHint(IReadOnlyList<Bolt11RouteHint> routeHint)
        {
            var result = new List<byte>();

            foreach (Bolt11RouteHint hint in routeHint)
            {
                result.AddRange((byte[])hint.NodeId);

                ulong scid = hint.ShortChannelId.ToUInt64();

                for (int shift = 56; shift >= 0; shift -= 8)
                    result.Add((byte)(scid >> shift));

                for (int shift = 24; shift >= 0; shift -= 8)
                    result.Add((byte)(hint.FeeBaseMsat >> shift));

                for (int shift = 24; shift >= 0; shift -= 8)
                    result.Add((byte)(hint.FeeProportionalMillionths >> shift));

                result.Add((byte)(hint.CltvExpiryDelta >> 8));
                result.Add((byte)hint.CltvExpiryDelta);
            }

            return result.ToArray();
        }
    }

    public class Bolt11InvoiceRequest
    {
        /// <summary>
        /// Human readable prefix, e.g. lnbc for mainnet, lntb for testnet,
        /// lntbs for signet and lnbcrt for regtest.
        /// </summary>
        public string Prefix { get; set; } = "lnbc";

        public MiliSatoshis? AmountMsat { get; set; }

        public long Timestamp { get; set; } = 0;

        public PrivateKey PayeePrivateKey { get; set; } = new(new byte[32]);

        public UInt256 PaymentHash { get; set; } = new(new byte[32]);

        public Secret PaymentSecret { get; set; } = new(new byte[32]);

        public string? Description { get; set; }

        public UInt256? DescriptionHash { get; set; }

        public long? Expiry { get; set; }

        public ushort? MinFinalCltvExpiry { get; set; }

        public byte[]? Features { get; set; }

        public byte[]? PaymentMetadata { get; set; }

        public bool? IncludePayeeNodeId { get; set; }

        public List<Bolt11FallbackAddress>? FallbackAddresses { get; set; }

        public List<List<Bolt11RouteHint>>? RouteHints { get; set; }
    }
}
