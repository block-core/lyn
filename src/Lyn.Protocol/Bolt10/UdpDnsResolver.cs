using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Lyn.Protocol.Bolt10
{
    /// <summary>
    /// A single record returned by the DNS resolver.
    /// </summary>
    public class DnsRecord
    {
        public DnsRecordType Type { get; set; }

        /// <summary>
        /// The owner or target name of the record.
        /// </summary>
        public string Name { get; set; } = "";

        public IPAddress? Address { get; set; }

        public ushort Priority { get; set; }

        public ushort Weight { get; set; }

        public ushort Port { get; set; }
    }

    public enum DnsRecordType
    {
        A = 1,
        AAAA = 28,
        SRV = 33
    }

    /// <summary>
    /// Minimal UDP DNS client supporting A, AAAA and SRV lookups as required
    /// by BOLT 10 DNS seeds.
    /// </summary>
    public class UdpDnsResolver : IDnsResolver
    {
        private static readonly Dictionary<DnsRecordType, ushort> TYPE_CODES =
            new()
            {
                { DnsRecordType.A, 1 },
                { DnsRecordType.AAAA, 28 },
                { DnsRecordType.SRV, 33 }
            };

        private readonly IPAddress _server;
        private readonly int _port;

        public UdpDnsResolver() : this(new IPAddress(new byte[] { 127, 0, 0, 1 }), 53)
        {
        }

        public UdpDnsResolver(IPAddress server, int port)
        {
            _server = server;
            _port = port;
        }

        public IReadOnlyList<DnsRecord> Resolve(string name, DnsRecordType type)
        {
            byte[] query = BuildQuery(name, type);

            using var udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udp.ReceiveTimeout = 10000;

            var endPoint = new IPEndPoint(_server, _port);
            udp.SendTo(query, endPoint);

            var buffer = new byte[4096];
            EndPoint remote = endPoint;
            int received = udp.ReceiveFrom(buffer, ref remote);

            return ParseResponse(buffer.AsSpan(0, received).ToArray(), name);
        }

        public static byte[] BuildQuery(string name, DnsRecordType type)
        {
            var random = new Random();
            var query = new List<byte>
            {
                // Header: id, flags (recursion desired), one question.
                (byte)random.Next(256), (byte)random.Next(256),
                0x01, 0x00,
                0x00, 0x01,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00
            };

            foreach (string label in name.Split('.'))
            {
                if (label.Length == 0)
                    continue;

                if (label.Length > 63)
                    throw new ArgumentException("DNS label exceeds 63 characters", nameof(name));

                query.Add((byte)label.Length);
                query.AddRange(System.Text.Encoding.ASCII.GetBytes(label));
            }

            query.Add(0); // root label

            query.Add((byte)(TYPE_CODES[type] >> 8));
            query.Add((byte)(TYPE_CODES[type] & 0xFF));
            query.Add(0x00);
            query.Add(0x01); // IN class

            return query.ToArray();
        }

        public static IReadOnlyList<DnsRecord> ParseResponse(byte[] response, string queriedName)
        {
            if (response.Length < 12)
                throw new FormatException("DNS response too short");

            ushort answerCount = (ushort)((response[6] << 8) | response[7]);

            int offset = 12;

            // Skip the question section (single question).
            SkipName(response, ref offset);
            offset += 4;

            var records = new List<DnsRecord>();

            for (int i = 0; i < answerCount && offset < response.Length; i++)
            {
                string name = ParseName(response, ref offset);

                if (offset + 10 > response.Length)
                    break;

                ushort typeCode = (ushort)((response[offset] << 8) | response[offset + 1]);
                offset += 4; // type + class

                offset += 4; // ttl

                ushort rdLength = (ushort)((response[offset] << 8) | response[offset + 1]);
                offset += 2;

                int rdataStart = offset;

                switch ((DnsRecordType)typeCode)
                {
                    case DnsRecordType.A:
                        if (rdLength == 4)
                            records.Add(new DnsRecord
                            {
                                Type = DnsRecordType.A,
                                Name = name.Length > 0 ? name : queriedName,
                                Address = new IPAddress(response.Skip(rdataStart).Take(4).ToArray())
                            });

                        break;

                    case DnsRecordType.AAAA:
                        if (rdLength == 16)
                            records.Add(new DnsRecord
                            {
                                Type = DnsRecordType.AAAA,
                                Name = name.Length > 0 ? name : queriedName,
                                Address = new IPAddress(response.Skip(rdataStart).Take(16).ToArray())
                            });

                        break;

                    case DnsRecordType.SRV:
                        if (rdLength >= 7)
                        {
                            int targetOffset = rdataStart + 6;

                            var record = new DnsRecord
                            {
                                Type = DnsRecordType.SRV,
                                Name = name.Length > 0 ? name : queriedName,
                                Priority = (ushort)((response[rdataStart] << 8) | response[rdataStart + 1]),
                                Weight = (ushort)((response[rdataStart + 2] << 8) | response[rdataStart + 3]),
                                Port = (ushort)((response[rdataStart + 4] << 8) | response[rdataStart + 5])
                            };

                            int localOffset = targetOffset;
                            record.Name += ":" + ParseName(response, ref localOffset);

                            records.Add(record);
                        }

                        break;
                }

                offset = rdataStart + rdLength;
            }

            return records;
        }

        private static void SkipName(byte[] message, ref int offset)
        {
            ParseName(message, ref offset);
        }

        private static string ParseName(byte[] message, ref int offset)
        {
            var labels = new List<string>();
            int jumpCount = 0;
            bool jumped = false;
            int localOffset = offset;

            while (true)
            {
                if (localOffset >= message.Length)
                    throw new FormatException("Truncated DNS name");

                int length = message[localOffset];

                if (length == 0)
                {
                    if (!jumped)
                        offset = localOffset + 1;

                    break;
                }

                if ((length & 0xC0) == 0xC0)
                {
                    // Compression pointer.
                    int pointer = ((length & 0x3F) << 8) | message[localOffset + 1];

                    if (!jumped)
                        offset = localOffset + 2;

                    localOffset = pointer;
                    jumped = true;
                    jumpCount++;

                    if (jumpCount > 32)
                        throw new FormatException("DNS name pointer loop");

                    continue;
                }

                localOffset++;
                labels.Add(System.Text.Encoding.ASCII.GetString(message, localOffset, length));
                localOffset += length;
            }

            return string.Join(".", labels);
        }
    }

    public interface IDnsResolver
    {
        IReadOnlyList<DnsRecord> Resolve(string name, DnsRecordType type);
    }
}
