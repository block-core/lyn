using System;
using System.Collections.Generic;
using System.Linq;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt11
{
    /// <summary>
    /// A parsed BOLT 11 payment request.
    /// </summary>
    public class Bolt11Invoice
    {
        /// <summary>
        /// The chain the invoice is for, derived from the human readable prefix.
        /// </summary>
        public string Prefix { get; set; } = "lnbc";

        /// <summary>
        /// Requested amount in millisatoshi, null when the amount is unspecified.
        /// </summary>
        public MiliSatoshis? AmountMsat { get; set; }

        /// <summary>
        /// Creation timestamp of the invoice, seconds since 1970-01-01 UTC.
        /// </summary>
        public long Timestamp { get; set; }

        public UInt256 PaymentHash { get; set; } = new(new byte[32]);

        public Secret? PaymentSecret { get; set; }

        /// <summary>
        /// Short description of the payment, mutually exclusive with DescriptionHash.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Hash of a description that is transported out of band.
        /// </summary>
        public UInt256? DescriptionHash { get; set; }

        /// <summary>
        /// Payee node id, either recovered from the signature or provided by the n field.
        /// </summary>
        public PublicKey PayeeNodeId { get; set; } = new();

        /// <summary>
        /// Duration in seconds after which the invoice expires. Default 3600.
        /// </summary>
        public long Expiry { get; set; } = 3600;

        /// <summary>
        /// Minimum cltv delta for the final hop. Default 18.
        /// </summary>
        public ushort MinFinalCltvExpiry { get; set; } = 18;

        public byte[] Features { get; set; } = new byte[0];

        public byte[]? PaymentMetadata { get; set; }

        public List<Bolt11FallbackAddress> FallbackAddresses { get; set; } = new();

        public List<List<Bolt11RouteHint>> RouteHints { get; set; } = new();

        /// <summary>
        /// True when this invoice has expired relative to the given time.
        /// </summary>
        public bool IsExpired(long nowUnixSeconds) => Timestamp + Expiry < nowUnixSeconds;

        public override string ToString() => RawInvoice ?? base.ToString()!;

        /// <summary>
        /// The original bech32 string this invoice was created from or encoded to.
        /// </summary>
        public string? RawInvoice { get; set; }
    }

    public class Bolt11FallbackAddress
    {
        /// <summary>
        /// Witness version, or 17 (P2PKH) / 18 (P2SH) for legacy addresses.
        /// </summary>
        public byte Version { get; set; }

        public byte[] Address { get; set; } = new byte[0];
    }

    public class Bolt11RouteHint
    {
        public PublicKey NodeId { get; set; } = new();

        public ShortChannelId ShortChannelId { get; set; } = new(0, 0, 0);

        public uint FeeBaseMsat { get; set; }

        public uint FeeProportionalMillionths { get; set; }

        public ushort CltvExpiryDelta { get; set; }
    }
}
