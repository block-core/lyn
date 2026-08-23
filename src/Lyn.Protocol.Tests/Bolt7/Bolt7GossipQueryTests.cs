using System;
using System.Linq;
using FluentAssertions;
using Lyn.Protocol.Bolt7;
using Lyn.Protocol.Bolt7.Entities;
using Lyn.Protocol.Bolt7.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt7
{
    public class Bolt7GossipQueryTests
    {
        private readonly ISerializationFactory _serializationFactory = new SerializationFactory(new TestServiceProvider());

        private class TestServiceProvider : IServiceProvider
        {
            public object? GetService(Type serviceType)
            {
                if (!serviceType.IsGenericType || serviceType.GetGenericTypeDefinition() != typeof(IProtocolTypeSerializer<>))
                    return null;

                var serializerTypes = new[]
                {
                    typeof(QueryShortChannelIdsSerializer), typeof(QueryChannelRangeSerializer),
                    typeof(ReplyChannelRangeSerializer), typeof(ReplyShortChannelIdsEndSerializer)
                };

                var serializerType = serializerTypes.FirstOrDefault(t =>
                    t.GetInterfaces().Any(i => i == serviceType));

                return serializerType is null ? null : Activator.CreateInstance(serializerType);
            }
        }

        [Fact]
        public void QueryChannelRangeRoundTrips()
        {
            var original = new QueryChannelRange(
                ChainHashes.BitcoinRegTest, 700000, 1000);

            byte[] bytes = _serializationFactory.Serialize(original);
            bytes.Length.Should().Be(40); // 32 + 4 + 4

            QueryChannelRange deserialized = _serializationFactory.Deserialize<QueryChannelRange>(bytes);

            deserialized.ChainHash.Should().Be(original.ChainHash);
            deserialized.FirstBlocknum.Should().Be(700000U);
            deserialized.NumberOfBlocks.Should().Be(1000U);
        }

        [Fact]
        public void ReplyChannelRangeRoundTrips()
        {
            byte[] encoded = EncodedShortIds.Encode(new[]
            {
                ShortChannelId.FromUInt64(1),
                ShortChannelId.FromUInt64(0xABCDEF123456)
            });

            var original = new ReplyChannelRange(
                ChainHashes.Bitcoin, 700000, 1000, true, encoded);

            ReplyChannelRange deserialized =
                _serializationFactory.Deserialize<ReplyChannelRange>(_serializationFactory.Serialize(original));

            deserialized.Complete.Should().BeTrue();
            deserialized.FirstBlocknum.Should().Be(700000U);
            deserialized.EncodedShortIds.Should().Equal(encoded);
        }

        [Fact]
        public void EncodedShortIdsRoundTripsThroughBigSizeEncoding()
        {
            var scids = new[]
            {
                ShortChannelId.FromUInt64(5),                          // single byte bigsize
                ShortChannelId.FromUInt64(1000),                       // three byte bigsize
                ShortChannelId.FromUInt64(70000),                      // five byte bigsize
                ShortChannelId.FromUInt64(0x0102030405060708)          // nine byte bigsize
            };

            byte[] encoded = EncodedShortIds.Encode(scids);

            // encoding byte + 1 + 3 + 5 + 9
            encoded.Length.Should().Be(1 + 1 + 3 + 5 + 9);

            ShortChannelId[] decoded = EncodedShortIds.Decode(encoded);

            decoded.Select(s => s.ToUInt64()).Should().Equal(scids.Select(s => s.ToUInt64()));
        }

        [Fact]
        public void QueryShortChannelIdsServiceRepliesWithAnnouncementsAndEnd()
        {
            var knownScid = ShortChannelId.FromUInt64(42);
            var unknownScid = ShortChannelId.FromUInt64(43);

            var channelAnnouncement = new ChannelAnnouncement
            {
                ShortChannelId = knownScid,
                ChainHash = ChainHashes.BitcoinRegTest
            };

            var repository = new Mock<IGossipRepository>();
            repository.Setup(r => r.GetGossipChannelAsync(knownScid))
                .ReturnsAsync(new GossipChannel(channelAnnouncement));
            repository.Setup(r => r.GetGossipChannelAsync(unknownScid))
                .ReturnsAsync((GossipChannel?)null);

            var service = new QueryShortChannelIdsService(repository.Object);

            byte[] encoded = EncodedShortIds.Encode(new[] { knownScid, unknownScid });
            var query = new QueryShortChannelIds(ChainHashes.BitcoinRegTest, (ushort)encoded.Length, encoded);

            MessageProcessingOutput result = service.ProcessMessageAsync(
                new PeerMessage<QueryShortChannelIds>(RandomMessages.NewRandomPublicKey(),
                    new BoltMessage { Payload = query })).Result;

            result.Success.Should().BeTrue();
            result.ResponseMessages.Should().NotBeNull();

            // One announcement for the known channel plus the end message.
            result.ResponseMessages!.Length.Should().Be(2);
            result.ResponseMessages[0].Payload.MessageType.Should().Be(MessageType.ChannelAnnouncement);
            result.ResponseMessages[1].Payload.Should().BeOfType<ReplyShortChannelIdsEnd>();
            ((ReplyShortChannelIdsEnd)result.ResponseMessages[1].Payload).Complete.Should().BeFalse();
        }

        [Fact]
        public void QueryChannelRangeServiceReturnsKnownChannels()
        {
            var inRange = ShortChannelId.FromUInt64(((ulong)700_500 << 40));
            var outOfRange = ShortChannelId.FromUInt64(((ulong)900_000 << 40));

            var repository = new Mock<IGossipRepository>();
            repository.Setup(r => r.GetGossipChannelsAsync())
                .ReturnsAsync(new[]
                {
                    new GossipChannel(new ChannelAnnouncement { ShortChannelId = inRange }),
                    new GossipChannel(new ChannelAnnouncement { ShortChannelId = outOfRange })
                });

            var service = new QueryChannelRangeService(repository.Object);

            var query = new QueryChannelRange(ChainHashes.Bitcoin, 700_000, 10_000);

            MessageProcessingOutput result = service.ProcessMessageAsync(
                new PeerMessage<QueryChannelRange>(RandomMessages.NewRandomPublicKey(),
                    new BoltMessage { Payload = query })).Result;

            result.Success.Should().BeTrue();

            var reply = (ReplyChannelRange)result.ResponseMessages![0].Payload;
            reply.Complete.Should().BeTrue();

            ShortChannelId[] decoded = EncodedShortIds.Decode(reply.EncodedShortIds);
            decoded.Should().HaveCount(1);
            decoded[0].ToUInt64().Should().Be(inRange.ToUInt64());
        }
    }
}
