using System;
using System.Linq;
using FluentAssertions;
using Lyn.Protocol.Bolt10;
using Lyn.Protocol.Bolt11;
using Lyn.Types.Fundamental;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt10
{
    public class Bolt10Tests
    {
        [Fact]
        public void BuildQueryProducesValidDnsPacket()
        {
            byte[] query = UdpDnsResolver.BuildQuery("r0.a2.n25.lseed.example.com", DnsRecordType.SRV);

            // Header is 12 bytes, question has name labels + root + type + class.
            query.Length.Should().BeGreaterThan(12 + 4);

            // Flags: recursion desired.
            query[2].Should().Be(0x01);
            query[3].Should().Be(0x00);

            // One question.
            ((query[4] << 8) | query[5]).Should().Be(1);

            // The qtype for SRV is 33.
            string ascii = System.Text.Encoding.ASCII.GetString(query);
            ascii.Should().Contain("lseed");
        }

        [Fact]
        public void ParsesSrvResponseWithCompressionPointers()
        {
            // Handcrafted DNS response: one question and one SRV answer where the
            // target name uses a compression pointer back to the question name.
            byte[] response = BuildExampleSrvResponse();

            var records = UdpDnsResolver.ParseResponse(response, "lseed.example.com");

            records.Should().HaveCount(1);
            records[0].Type.Should().Be(DnsRecordType.SRV);
            records[0].Port.Should().Be(9735);
            records[0].Priority.Should().Be(10);
            records[0].Name.Should().EndWith("ln1qexample.lseed.example.com");
        }

        [Fact]
        public void ParsesARecordResponse()
        {
            byte[] response = BuildExampleAResponse();

            var records = UdpDnsResolver.ParseResponse(response, "lseed.example.com");

            records.Should().HaveCount(1);
            records[0].Address.Should().NotBeNull();
            string ip = records[0].Address!.ToString();
            ip.Should().Be("139.59.143.87");
        }

        [Fact]
        public void DecodesVirtualHostnameNodeId()
        {
            const string hostname =
                "ln1qwktpe6jxltmpphyl578eax6fcjc2m807qalr76a5gfmx7k9qqfjwy4mctz.lseed.bitcoinstats.com";

            PublicKey? nodeId = DnsSeedClient.DecodeVirtualHostname(hostname);

            nodeId.Should().NotBeNull();
            ((byte[])nodeId!).Length.Should().Be(33);
        }

        [Fact]
        public void VirtualHostnameRoundTripsThroughBech32()
        {
            // Encode an arbitrary node id into a virtual hostname label then decode it.
            byte[] nodeId = Convert.FromHexString(
                "03e7156ae33b0a208d0744199163177e909e80176e55d97a2f221ede0f934dd9ad");

            int[] data = Bech32.ConvertBytesToBits(nodeId, 8, 5);
            string label = Bech32.Encode(DnsSeedClient.NODE_ID_HRP, data);

            PublicKey? decoded = DnsSeedClient.DecodeVirtualHostname(label + ".lseed.example.com");

            decoded.Should().NotBeNull();
            Convert.ToHexString((byte[])decoded!).ToLowerInvariant().Should()
                .Be("03e7156ae33b0a208d0744199163177e909e80176e55d97a2f221ede0f934dd9ad");
        }

        [Fact]
        public void ConditionsBuildPerBolt10()
        {
            DnsSeedClient.BuildConditions(25, false, 0).Should().Be("r0.a2.n25.");
            DnsSeedClient.BuildConditions(10, true, 0).Should().Be("r0.a4.n10.");
            DnsSeedClient.BuildConditions(5, false, 1).Should().Be("r1.a2.n5.");
        }

        /// <summary>
        /// Builds a minimal but valid DNS SRV response:
        /// question for lseed.example.com SRV plus one answer whose target is
        /// ln1qexample.lseed.example.com encoded with a compression pointer.
        /// </summary>
        private static byte[] BuildExampleSrvResponse()
        {
            var message = new System.Collections.Generic.List<byte>
            {
                // header
                0x12, 0x34, // id
                0x81, 0x80, // response, recursion available
                0x00, 0x01, // questions
                0x00, 0x01, // answers
                0x00, 0x00,
                0x00, 0x00
            };

            int questionStart = message.Count;

            foreach (string label in new[] { "lseed", "example", "com" })
            {
                message.Add((byte)label.Length);
                message.AddRange(System.Text.Encoding.ASCII.GetBytes(label));
            }

            message.Add(0);
            message.Add(0);
            message.Add(33); // SRV
            message.Add(0);
            message.Add(1); // IN

            // Answer: pointer to question name (0xC0, offset questionStart).
            message.Add(0xC0);
            message.Add((byte)questionStart);
            message.Add(0);
            message.Add(33); // SRV
            message.Add(0);
            message.Add(1); // IN
            message.Add(0);
            message.Add(0);
            message.Add(0);
            message.Add(60); // ttl

            message.Add(0);
            message.Add(24); // rdlength: 2+2+2 + target(3 labels compressed = 18 bytes)

            message.Add(0);
            message.Add(10); // priority
            message.Add(0);
            message.Add(10); // weight
            message.Add(0x26);
            message.Add(0x07); // port 9735

            // Target name: ln1qexample (11) . lseed(5) . example(7) . com(3) . root
            // Use compression pointers for lseed.example.com which sits at questionStart.
            message.Add(11);
            message.AddRange(System.Text.Encoding.ASCII.GetBytes("ln1qexample"));
            message.Add(0xC0);
            message.Add((byte)questionStart);

            return message.ToArray();
        }

        private static byte[] BuildExampleAResponse()
        {
            var message = new System.Collections.Generic.List<byte>
            {
                0x12, 0x34,
                0x81, 0x80,
                0x00, 0x01,
                0x00, 0x01,
                0x00, 0x00,
                0x00, 0x00
            };

            int questionStart = message.Count;

            foreach (string label in new[] { "lseed", "example", "com" })
            {
                message.Add((byte)label.Length);
                message.AddRange(System.Text.Encoding.ASCII.GetBytes(label));
            }

            message.Add(0);
            message.Add(0);
            message.Add(1); // A
            message.Add(0);
            message.Add(1);

            message.Add(0xC0);
            message.Add((byte)questionStart);
            message.Add(0);
            message.Add(1); // A
            message.Add(0);
            message.Add(1);
            message.Add(0);
            message.Add(0);
            message.Add(0);
            message.Add(60);
            message.Add(0);
            message.Add(4);
            message.AddRange(new byte[] { 139, 59, 143, 87 }); // 139.59.143.87

            return message.ToArray();
        }
    }
}
