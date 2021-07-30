using System;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Entities;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Types;
using System.Buffers;
using FluentAssertions;
using FluentAssertions.Equivalency;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt2.ChannelEstablishment
{
    public class ChannelCandidateSerializerTests
    {
        private ChannelCandidateSerializer _sut;

        public ChannelCandidateSerializerTests()
        {
            _sut = new ChannelCandidateSerializer(new OpenChannelSerializer(), new AcceptChannelSerializer());
        }

        [Fact]
        public void StartOpenChannelSuccess()
        {
            ChannelCandidate candidate = new ChannelCandidate
            {
                OpenChannel = new OpenChannel
                {
                    FundingSatoshis = 100000,
                    DustLimitSatoshis = 100,
                    ChannelReserveSatoshis = 1000,
                    HtlcMinimumMsat = 1,
                    MaxAcceptedHtlcs = 2,
                    MaxHtlcValueInFlightMsat = 10,
                    ToSelfDelay = 100,
                    ChannelFlags = 1,
                    FeeratePerKw = 200,
                    PushMsat = 10,
                    TemporaryChannelId = RandomMessages.NewRandomUint256(),
                    FundingPubkey = RandomMessages.NewRandomPublicKey(),
                    PaymentBasepoint = RandomMessages.NewRandomPublicKey(),
                    DelayedPaymentBasepoint = RandomMessages.NewRandomPublicKey(),
                    HtlcBasepoint = RandomMessages.NewRandomPublicKey(),
                    RevocationBasepoint = RandomMessages.NewRandomPublicKey(),
                    FirstPerCommitmentPoint = RandomMessages.NewRandomPublicKey(),
                    ChainHash = RandomMessages.NewRandomUint256(),
                },
                OpenChannelUpfrontShutdownScript = new byte[] { 0x0001 },
                AcceptChannel = new AcceptChannel
                {
                    ToSelfDelay = 300,
                    MaxHtlcValueInFlightMsat = 10,
                    ChannelReserveSatoshis = 500,
                    DustLimitSatoshis = 200,
                    HtlcMinimumMsat = 10,
                    MaxAcceptedHtlcs = 20,
                    MinimumDepth = 6,
                    TemporaryChannelId = RandomMessages.NewRandomUint256(),
                    FundingPubkey = RandomMessages.NewRandomPublicKey(),
                    PaymentBasepoint = RandomMessages.NewRandomPublicKey(),
                    DelayedPaymentBasepoint = RandomMessages.NewRandomPublicKey(),
                    HtlcBasepoint = RandomMessages.NewRandomPublicKey(),
                    RevocationBasepoint = RandomMessages.NewRandomPublicKey(),
                    FirstPerCommitmentPoint = RandomMessages.NewRandomPublicKey(),
                },
                AcceptChannelUpfrontShutdownScript = new byte[] { 0x0002 },
            };

            var buffer = _sut.SerializeHelper(candidate);
            var result = _sut.DeserializeHelper(buffer);
            result.Should().BeEquivalentTo(candidate);
        }
    }
}