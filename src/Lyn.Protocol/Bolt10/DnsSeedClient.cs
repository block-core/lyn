using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Lyn.Protocol.Bolt11;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt10
{
    /// <summary>
    /// A lightning node endpoint discovered through a DNS seed.
    /// </summary>
    public class DnsNodeEndpoint
    {
        public DnsNodeEndpoint(IPAddress address, ushort port, PublicKey? nodeId = null)
        {
            Address = address;
            Port = port;
            NodeId = nodeId;
        }

        public IPAddress Address { get; set; }

        public ushort Port { get; set; }

        /// <summary>
        /// The node id when known, decoded from the SRV virtual hostname.
        /// </summary>
        public PublicKey? NodeId { get; set; }
    }

    /// <summary>
    /// Implements DNS seed queries as defined by BOLT 10.
    ///
    /// Conditions are encoded as dot separated subdomain components of the query,
    /// e.g. r0.a2.n10.lseed.bitcoinstats.com queries 10 IPv4 records of Bitcoin nodes.
    /// SRV replies contain virtual hostnames whose first label is a bech32 encoded
    /// node id with hrp "ln".
    /// </summary>
    public class DnsSeedClient
    {
        public const string NODE_ID_HRP = "ln";
        public const ushort DEFAULT_PORT = 9735;

        private readonly IDnsResolver _resolver;

        public DnsSeedClient(IDnsResolver resolver)
        {
            _resolver = resolver;
        }

        /// <summary>
        /// Queries the seed for listening nodes.
        /// </summary>
        /// <param name="seedRootDomain">the seed root domain, e.g. lseed.bitcoinstats.com</param>
        /// <param name="maxNodes">number of desired reply records</param>
        /// <param name="preferIpv6">request IPv6 instead of IPv4 addresses</param>
        /// <param name="realm">realm byte, 0 for Bitcoin</param>
        public IReadOnlyList<DnsNodeEndpoint> DiscoverNodes(
            string seedRootDomain,
            int maxNodes = 25,
            bool preferIpv6 = false,
            byte realm = 0)
        {
            string conditions = BuildConditions(maxNodes, preferIpv6, realm);
            string query = conditions + seedRootDomain;

            // Prefer SRV since it provides both node ids and ports.
            IReadOnlyList<DnsRecord> srvRecords = _resolver.Resolve(query, DnsRecordType.SRV);

            var endpoints = new List<DnsNodeEndpoint>();

            foreach (DnsRecord record in srvRecords)
            {
                int separator = record.Name.LastIndexOf(':');

                if (separator <= 0)
                    continue;

                string hostname = record.Name.Substring(0, separator);

                PublicKey? nodeId = DecodeVirtualHostname(hostname);

                if (nodeId is null)
                    continue;

                // Resolve the address of the virtual hostname when needed.
                DnsRecordType addressType = preferIpv6 ? DnsRecordType.AAAA : DnsRecordType.A;
                IReadOnlyList<DnsRecord> addresses = _resolver.Resolve(hostname, addressType);

                IPAddress? address = addresses.FirstOrDefault()?.Address;

                if (address is null)
                    continue;

                endpoints.Add(new DnsNodeEndpoint(address, record.Port == 0 ? DEFAULT_PORT : record.Port, nodeId));
            }

            if (endpoints.Count > 0)
                return endpoints;

            // Fall back to plain A/AAAA queries, these only return nodes on the default port.
            DnsRecordType fallbackType = preferIpv6 ? DnsRecordType.AAAA : DnsRecordType.A;
            IReadOnlyList<DnsRecord> addressRecords = _resolver.Resolve(query, fallbackType);

            return addressRecords
                .Where(r => r.Address != null)
                .Select(r => new DnsNodeEndpoint(r.Address!, DEFAULT_PORT))
                .ToList();
        }

        /// <summary>
        /// Builds the condition subdomain prefix per BOLT 10, e.g. "r0.a2.n25.".
        /// </summary>
        public static string BuildConditions(int maxNodes, bool preferIpv6, byte realm)
        {
            int addressType = preferIpv6 ? 4 : 2;

            return $"r{realm}.a{addressType}.n{maxNodes}.";
        }

        /// <summary>
        /// Decodes a virtual hostname like
        /// ln1qwktpe6jxltmpphyl578eax6fcjc2m807qalr76a5gfmx7k9qqfjwy4mctz.lseed...
        /// returning the bech32 encoded node id from its first label.
        /// </summary>
        public static PublicKey? DecodeVirtualHostname(string hostname)
        {
            if (string.IsNullOrEmpty(hostname))
                return null;

            int separator = hostname.IndexOf('.');
            string label = separator > 0 ? hostname.Substring(0, separator) : hostname;

            try
            {
                (string hrp, int[] data) = Bech32.Decode(label);

                if (!hrp.Equals(NODE_ID_HRP, StringComparison.OrdinalIgnoreCase))
                    return null;

                byte[] nodeId = Bech32.ConvertBitsToBytes(data, 5, 8);

                if (nodeId.Length != 33)
                    return null;

                return new PublicKey(nodeId);
            }
            catch (Bech32Exception)
            {
                return null;
            }
        }
    }
}
