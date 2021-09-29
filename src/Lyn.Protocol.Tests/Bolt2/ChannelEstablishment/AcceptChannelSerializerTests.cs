using System.Buffers;
using FluentAssertions;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt2.ChannelEstablishment
{
    public class AcceptChannelSerializerTests
    {
        private AcceptChannelSerializer _sut;

        public AcceptChannelSerializerTests()
        {
            _sut = new AcceptChannelSerializer();
        }

        [Fact]
        public void SerializingAndDeserializingNetworkMessageGetsTheSameResult() //Test from real message TODO add proper test from generated random data
        {
            var acceptChannel = new AcceptChannel
            {
                FundingPubkey = Hex.FromString("0x03100a0e62903ae4a67902241f41590bce4f925e3676a41667388c8b2da446b25f"),
                HtlcBasepoint = Hex.FromString("0x02615c1607d46592c11c3dc7e3215cf05fbf1c417702ae88be500c22a30c084de0"),
                MinimumDepth = 3,
                PaymentBasepoint =
                    Hex.FromString("0x024e50f1f547d9cb1150fd19b252e2bc56966f3000b331f6ff12c9736f041e9e02"),
                RevocationBasepoint =
                    Hex.FromString("0x02a6ea134b2fda74bdb23de5b1f060c96d8c6d0bf405a102ed65af5e854cda1879"),
                ChannelReserveSatoshis = 160000,
                DelayedPaymentBasepoint =
                    Hex.FromString("0x025b646fef398cffd0a2a4584ae604a788b14ff57350a0ce548d1825af3d762db7"),
                DustLimitSatoshis = 546,
                HtlcMinimumMsat = 1,
                MaxAcceptedHtlcs = 30,
                TemporaryChannelId =
                    new UInt256(Hex.FromString("431edbcc612dffd3ede22b84beca344a810cc360b94b22601d165f584159c085")),
                ToSelfDelay = 720,
                FirstPerCommitmentPoint =
                    Hex.FromString("0x03cf020f4341d3ef7af94b49f13d5d234ed82887529f122bb6d27d2ba645ac4340"),
                MaxHtlcValueInFlightMsat = 5000000000
            };

            var buffer = new ArrayBufferWriter<byte>();
            
            _sut.Serialize(acceptChannel,buffer);

            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(buffer.WrittenMemory));

            var deserialized = _sut.Deserialize(ref reader);
            
            deserialized.Should()
                .BeEquivalentTo(acceptChannel);
        }
    }
}