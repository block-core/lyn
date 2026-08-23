using System;
using System.Linq;
using FluentAssertions;
using Lyn.Protocol.Bolt2.ChannelClose.Messages;
using Lyn.Protocol.Bolt2.MessageRetransmission.Messages;
using Lyn.Protocol.Bolt2.NormalOperations.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;
using Xunit;
using static Lyn.Protocol.Bolt2.NormalOperations.Messages.UpdateAddHtlcSerializer;

namespace Lyn.Protocol.Tests.Bolt2
{
    /// <summary>
    /// Round trip serialization tests for the BOLT 2 normal operations, channel close and
    /// message retransmission messages.
    /// </summary>
    public class Bolt2MessageSerializationTests
    {
        private readonly ISerializationFactory _serializationFactory = new SerializationFactory(new TestServiceProvider());

        private class TestServiceProvider : IServiceProvider
        {
            public object? GetService(Type serviceType)
            {
                if (!serviceType.IsGenericType || serviceType.GetGenericTypeDefinition() != typeof(IProtocolTypeSerializer<>))
                    return null;

                var messageType = serviceType.GetGenericArguments()[0];
                var serializerTypes = new[]
                {
                    typeof(UpdateAddHtlcSerializer), typeof(UpdateFulfillHtlcSerializer),
                    typeof(UpdateFailHtlcSerializer), typeof(UpdateFailMalformedHtlcSerializer),
                    typeof(UpdateFeeSerializer), typeof(CommitmentSignedSerializer),
                    typeof(RevokeAndAckSerializer), typeof(ShutdownSerializer),
                    typeof(ClosingSignedSerializer), typeof(ChannelReestablishSerializer)
                };

                var serializerType = serializerTypes.FirstOrDefault(t =>
                    t.GetInterfaces().Any(i => i == serviceType));

                return serializerType is null ? null : Activator.CreateInstance(serializerType);
            }
        }

        [Fact]
        public void UpdateAddHtlcRoundTrips()
        {
            var original = new UpdateAddHtlc
            {
                ChannelId = new UInt256(RandomMessages.GetRandomByteArray(32)),
                Id = 12345,
                AmountMsat = new MiliSatoshis(9999999),
                PaymentHash = new UInt256(RandomMessages.GetRandomByteArray(32)),
                CltvExpiry = 700123,
                OnionRoutingPacket = RandomMessages.GetRandomByteArray(ONION_ROUTING_PACKET_LENGTH)
            };

            UpdateAddHtlc deserialized =
                _serializationFactory.Deserialize<UpdateAddHtlc>(_serializationFactory.Serialize(original));

            deserialized.Id.Should().Be(original.Id);
            deserialized.AmountMsat.Should().Be(original.AmountMsat);
            deserialized.CltvExpiry.Should().Be(original.CltvExpiry);
            deserialized.PaymentHash.Should().Be(original.PaymentHash);
            deserialized.OnionRoutingPacket.Should().Equal(original.OnionRoutingPacket!);

            _serializationFactory.Serialize(deserialized).Should().Equal(_serializationFactory.Serialize(original));
        }

        [Fact]
        public void UpdateFulfillHtlcRoundTrips()
        {
            var original = new UpdateFulfillHtlc
            {
                ChannelId = new UInt256(RandomMessages.GetRandomByteArray(32)),
                Id = 42,
                PaymentPreimage = new Preimage(RandomMessages.GetRandomByteArray(32))
            };

            byte[] bytes = _serializationFactory.Serialize(original);
            bytes.Length.Should().Be(72); // 32 + 8 + 32

            UpdateFulfillHtlc deserialized = _serializationFactory.Deserialize<UpdateFulfillHtlc>(bytes);

            deserialized.Id.Should().Be(42UL);
            ((byte[])deserialized.PaymentPreimage!).Should().Equal((byte[])original.PaymentPreimage!);
        }

        [Fact]
        public void UpdateFailMalformedHtlcRoundTrips()
        {
            var original = new UpdateFailMalformedHtlc
            {
                ChannelId = new UInt256(RandomMessages.GetRandomByteArray(32)),
                Id = 7,
                Sha256OfOnion = new UInt256(RandomMessages.GetRandomByteArray(32)),
                FailureCode = 0x8000
            };

            byte[] bytes = _serializationFactory.Serialize(original);
            bytes.Length.Should().Be(74); // 32 + 8 + 32 + 2

            UpdateFailMalformedHtlc deserialized = _serializationFactory.Deserialize<UpdateFailMalformedHtlc>(bytes);

            deserialized.FailureCode.Should().Be((ushort)0x8000);
            deserialized.Sha256OfOnion.Should().Be(original.Sha256OfOnion);
        }

        [Fact]
        public void CommitmentSignedWithTwoHtlcsRoundTrips()
        {
            var original = new CommitmentSigned
            {
                ChannelId = new UInt256(RandomMessages.GetRandomByteArray(32)),
                NumHtlcs = 2,
                Signature = new CompressedSignature(RandomMessages.GetRandomByteArray(64)),
                HtlcSignature = RandomMessages.GetRandomByteArray(128)
            };

            byte[] bytes = _serializationFactory.Serialize(original);

            // 32 channel id + 2 num htlcs + 64 signature + 128 htlc signatures
            bytes.Length.Should().Be(226);

            CommitmentSigned deserialized = _serializationFactory.Deserialize<CommitmentSigned>(bytes);

            deserialized.NumHtlcs.Should().Be((ushort)2);
            deserialized.HtlcSignature.Should().NotBeNull();
            deserialized.HtlcSignature!.Length.Should().Be(128);
        }

        [Fact]
        public void ShutdownAndClosingSignedRoundTrip()
        {
            var shutdown = new Shutdown
            {
                ChannelId = new UInt256(RandomMessages.GetRandomByteArray(32)),
                ScriptPubkey = RandomMessages.GetRandomByteArray(22) // P2WPKH sized
            };

            Shutdown deserializedShutdown = _serializationFactory.Deserialize<Shutdown>(_serializationFactory.Serialize(shutdown));
            deserializedShutdown.ScriptPubkey.Should().Equal(shutdown.ScriptPubkey!);
            deserializedShutdown.Length.Should().Be((ushort)22);

            var closingSigned = new ClosingSigned
            {
                ChannelId = new UInt256(RandomMessages.GetRandomByteArray(32)),
                FeeSatoshis = new Satoshis(5000),
                Signature = new CompressedSignature(RandomMessages.GetRandomByteArray(64))
            };

            byte[] closingBytes = _serializationFactory.Serialize(closingSigned);
            closingBytes.Length.Should().Be(104); // 32 + 8 + 64

            ClosingSigned deserializedClosing = _serializationFactory.Deserialize<ClosingSigned>(closingBytes);
            deserializedClosing.FeeSatoshis.Should().Be(new Satoshis(5000));
        }
    }
}
