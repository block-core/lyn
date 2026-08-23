using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Lyn.Protocol.Bolt2.ChannelClose;
using Lyn.Protocol.Bolt2.ChannelClose.Messages;
using Lyn.Protocol.Bolt2.Entities;
using Lyn.Protocol.Bolt2.NormalOperations;
using Lyn.Protocol.Bolt2.NormalOperations.Messages;
using Lyn.Protocol.Bolt2.Wallet;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Lyn.Protocol.Bolt3.Types;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt2
{
    public class Bolt2ChannelCloseAndCommitmentTests
    {
        private static readonly UInt256 ChannelId = new(RandomByteArray());

        private static byte[] RandomByteArray()
        {
            var bytes = new byte[32];
            new Random(1234).NextBytes(bytes);
            return bytes;
        }

        private static PeerMessage<T> Message<T>(T payload) where T : MessagePayload
        {
            return new PeerMessage<T>(RandomMessages.NewRandomPublicKey(),
                new BoltMessage { Payload = payload });
        }

        private static PaymentChannel CreateChannel()
        {
            return new PaymentChannel(
                ChannelId,
                new Types.Bolt.ShortChannelId(0, 0, 0),
                new CompressedSignature(),
                new PublicKey(),
                Array.Empty<PublicKey>(),
                new Satoshis(100000),
                new OutPoint { Hash = new UInt256(RandomByteArray()), Index = 0 },
                new Satoshis(546),
                new Satoshis(546),
                new Satoshis(1000),
                new PublicKey(),
                new PublicKey(),
                new MiliSatoshis(0),
                new Basepoints { DelayedPayment = new PublicKey(), Htlc = new PublicKey(), Payment = new PublicKey(), Revocation = new PublicKey() },
                new Basepoints { DelayedPayment = new PublicKey(), Htlc = new PublicKey(), Payment = new PublicKey(), Revocation = new PublicKey() })
            {
                WasChannelInitiatedLocally = true
            };
        }

        [Fact]
        public async Task ShutdownStoresScriptAndRepliesWithOwnShutdown()
        {
            var repository = new Mock<IPaymentChannelRepository>();
            repository.Setup(r => r.TryGetPaymentChannelAsync(ChannelId))
                .ReturnsAsync(CreateChannel());

            var wallet = new Mock<IWalletTransactions>();
            wallet.Setup(w => w.GenerateTransactionForOutputAsync(It.IsAny<TransactionOutput>()))
                .ReturnsAsync(new Transaction
                {
                    Outputs = new[]
                    {
                        new TransactionOutput { PublicKeyScript = new byte[] { 0x00, 20 } }
                    }
                });

            var service = new ShutdownMessageService(
                NullLogger<ShutdownMessageService>.Instance, repository.Object, wallet.Object);

            var shutdown = new Shutdown
            {
                ChannelId = ChannelId,
                Length = 22,
                ScriptPubkey = new byte[] { 0x00, 20 }.Concat(new byte[20]).ToArray()
            };

            MessageProcessingOutput result = await service.ProcessMessageAsync(Message(shutdown));

            result.Success.Should().BeTrue();
            result.ResponseMessages.Should().HaveCount(1);

            var ourShutdown = (Shutdown)result.ResponseMessages![0].Payload;
            ourShutdown.ChannelId.Should().Be(ChannelId);
            ourShutdown.ScriptPubkey.Should().NotBeNull();

            repository.Verify(r => r.UpdatePaymentChannelAsync(It.Is<PaymentChannel>(c =>
                c.ShutdownReceived && c.RemoteShutdownScriptPubkey!.Length == 22)), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ShutdownRejectsUnknownScriptTypes()
        {
            var repository = new Mock<IPaymentChannelRepository>();
            repository.Setup(r => r.TryGetPaymentChannelAsync(ChannelId))
                .ReturnsAsync(CreateChannel());

            var service = new ShutdownMessageService(
                NullLogger<ShutdownMessageService>.Instance, repository.Object,
                Mock.Of<IWalletTransactions>());

            var shutdown = new Shutdown
            {
                ChannelId = ChannelId,
                ScriptPubkey = new byte[10] // unsupported length
            };

            MessageProcessingOutput result = await service.ProcessMessageAsync(Message(shutdown));

            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAddHtlcIsRecordedOnChannel()
        {
            var channel = CreateChannel();
            channel.WasChannelInitiatedLocally = false;

            var repository = new Mock<IPaymentChannelRepository>();
            repository.Setup(r => r.TryGetPaymentChannelAsync(ChannelId)).ReturnsAsync(channel);

            var chainConfig = new Mock<IChainConfigProvider>();
            chainConfig.Setup(c => c.GetConfiguration(It.IsAny<UInt256>()))
                .Returns((ChainParameters?)null);

            var service = new UpdateAddHtlcMessageService(
                NullLogger<UpdateAddHtlcMessageService>.Instance, repository.Object, chainConfig.Object);

            var htlc = new UpdateAddHtlc
            {
                ChannelId = ChannelId,
                Id = 0,
                AmountMsat = new MiliSatoshis(50000),
                PaymentHash = new UInt256(RandomByteArray()),
                CltvExpiry = 700000,
                OnionRoutingPacket = new byte[1366]
            };

            MessageProcessingOutput result = await service.ProcessMessageAsync(Message(htlc));

            result.Success.Should().BeTrue();
            channel.PendingRemoteHtlcs.Should().ContainSingle();
            channel.NextRemoteHtlcId.Should().Be(1);

            // A second HTLC with the same id must be rejected.
            MessageProcessingOutput duplicate = await service.ProcessMessageAsync(Message(htlc));
            duplicate.Success.Should().BeFalse();
        }

        [Fact]
        public void UpdateFeeRejectedFromNonOpener()
        {
            var channel = CreateChannel(); // WasChannelInitiatedLocally = true

            var repository = new Mock<IPaymentChannelRepository>();
            repository.Setup(r => r.TryGetPaymentChannelAsync(ChannelId)).ReturnsAsync(channel);

            var chainConfig = new Mock<IChainConfigProvider>();
            chainConfig.Setup(c => c.GetConfiguration(It.IsAny<UInt256>()))
                .Returns((ChainParameters?)null);

            var service = new UpdateFeeMessageService(
                NullLogger<UpdateFeeMessageService>.Instance, repository.Object, chainConfig.Object);

            MessageProcessingOutput result = service.ProcessMessageAsync(Message(new UpdateFee
            {
                ChannelId = ChannelId,
                FeeratePerKw = 2000
            })).Result;

            // We opened this channel so receiving update_fee from the remote peer is invalid.
            result.Success.Should().BeFalse();
        }
    }
}
