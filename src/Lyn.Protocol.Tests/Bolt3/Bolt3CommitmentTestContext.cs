using System;
using System.Buffers;
using System.Linq;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Messages;
using Lyn.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization;
using Lyn.Types.Serialization.Serializers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt3
{
    public class Bolt3CommitmentTestContext
    {
        public ulong FundingAmount;
        public ulong DustLimit;
        public ushort ToSelfDelay;
        public PrivateKey LocalFundingPrivkey;
        public PrivateKey RemoteFundingPrivkey;
        public Secret LocalPaymentBasepointSecret;
        public Secret RemotePaymentBasepointSecret;
        public Secret LocalHtlcBasepointSecret;
        public Secret RemoteHtlcBasepointSecret;
        public Secret LocalPerCommitmentSecret;
        public Secret LocalDelayedPaymentBasepointSecret;
        public Secret RemoteRevocationBasepointSecret;
        public PrivateKey LocalHtlcsecretkey;
        public PrivateKey RemoteHtlcsecretkey;
        public PrivateKey LocalDelayedSecretkey;
        public PublicKey LocalFundingPubkey;
        public PublicKey RemoteFundingPubkey;
        public PublicKey LocalPaymentBasepoint;
        public PublicKey RemotePaymentBasepoint;
        public PublicKey LocalHtlcBasepoint;
        public PublicKey RemoteHtlcBasepoint;
        public PublicKey LocalDelayedPaymentBasepoint;
        public PublicKey RemoteRevocationBasepoint;
        public PublicKey LocalPerCommitmentPoint;
        public PublicKey Localkey;
        public PublicKey Remotekey;
        public PublicKey LocalHtlckey;
        public PublicKey RemoteHtlckey;
        public PublicKey LocalDelayedkey;
        public PublicKey RemoteRevocationKey;
        public Keyset Keyset;
        public uint FundingOutputIndex;
        public ulong CommitmentNumber;
        public ulong CnObscurer;

        public bool OptionAnchorOutputs;

        public UInt256 FundingTxid;
        public OutPoint FundingTxOutpoint;

        public ILightningTransactions LightningTransactions;
        public ILightningScripts LightningScripts;
        public ILightningKeyDerivation KeyDerivation;
        public ITransactionHashCalculator TransactionHashCalculator;
        public ISerializationFactory SerializationFactory;

        public Bolt3CommitmentTestContext()
        {
            var provider = new ServiceCollection().AddSerializationComponents().BuildServiceProvider();
            SerializationFactory = new SerializationFactory(provider);

            LightningScripts = new LightningScripts();
            LightningTransactions = new LightningTransactions(new Mock<ILogger<LightningTransactions>>().Object, SerializationFactory, LightningScripts);
            KeyDerivation = new LightningKeyDerivation();

            TransactionHashCalculator = new TransactionHashCalculator(provider.GetService<IProtocolTypeSerializer<Transaction>>());

            OptionAnchorOutputs = false;

            FundingOutputIndex = 0;
            FundingAmount = 10000000;
            FundingTxid = UInt256.Parse("8984484a580b825b9972d7adb15050b3ab624ccd731946b3eeddb92f4e7ef6be");
            FundingTxOutpoint = new OutPoint { TxId = FundingTxid, Index = FundingOutputIndex };

            CommitmentNumber = 42;
            ToSelfDelay = 144;
            DustLimit = 546;

            LocalFundingPrivkey = new Secret(Hex.FromString("30ff4956bbdd3222d44cc5e8a1261dab1e07957bdac5ae88fe3261ef321f374901").Take(32).ToArray());
            RemoteFundingPrivkey = new Secret(Hex.FromString("1552dfba4f6cf29a62a0af13c8d6981d36d0ef8d61ba10fb0fe90da7634d7e1301").Take(32).ToArray());

            LocalPerCommitmentSecret = new Secret(Hex.FromString("1f1e1d1c1b1a191817161514131211100f0e0d0c0b0a09080706050403020100"));
            LocalPaymentBasepointSecret = new Secret(Hex.FromString("1111111111111111111111111111111111111111111111111111111111111111"));
            RemoteRevocationBasepointSecret = new Secret(Hex.FromString("2222222222222222222222222222222222222222222222222222222222222222"));
            LocalDelayedPaymentBasepointSecret = new Secret(Hex.FromString("3333333333333333333333333333333333333333333333333333333333333333"));
            RemotePaymentBasepointSecret = new Secret(Hex.FromString("4444444444444444444444444444444444444444444444444444444444444444"));

            LocalDelayedPaymentBasepoint = KeyDerivation.PublicKeyFromPrivateKey(LocalDelayedPaymentBasepointSecret);
            LocalPerCommitmentPoint = KeyDerivation.PublicKeyFromPrivateKey(LocalPerCommitmentSecret);

            LocalDelayedSecretkey = KeyDerivation.DerivePrivatekey(LocalDelayedPaymentBasepointSecret, LocalDelayedPaymentBasepoint, LocalPerCommitmentPoint);

            RemoteRevocationBasepoint = KeyDerivation.PublicKeyFromPrivateKey(RemoteRevocationBasepointSecret);
            RemoteRevocationKey = KeyDerivation.DeriveRevocationPublicKey(RemoteRevocationBasepoint, LocalPerCommitmentPoint);

            LocalDelayedkey = KeyDerivation.PublicKeyFromPrivateKey(LocalDelayedSecretkey);
            LocalPaymentBasepoint = KeyDerivation.PublicKeyFromPrivateKey(LocalPaymentBasepointSecret);

            RemotePaymentBasepoint = KeyDerivation.PublicKeyFromPrivateKey(RemotePaymentBasepointSecret);

            // TODO: thjis comment comes from c-lightning dan to investigate:
            /* FIXME: BOLT should include separate HTLC keys */
            LocalHtlcBasepoint = LocalPaymentBasepoint;
            RemoteHtlcBasepoint = RemotePaymentBasepoint;
            LocalHtlcBasepointSecret = LocalPaymentBasepointSecret;
            RemoteHtlcBasepointSecret = RemotePaymentBasepointSecret;

            RemoteHtlcsecretkey = KeyDerivation.DerivePrivatekey(RemoteHtlcBasepointSecret, RemoteHtlcBasepoint, LocalPerCommitmentPoint);

            Localkey = KeyDerivation.DerivePublickey(LocalPaymentBasepoint, LocalPerCommitmentPoint);

            Remotekey = KeyDerivation.DerivePublickey(RemotePaymentBasepoint, LocalPerCommitmentPoint);

            LocalHtlcsecretkey = KeyDerivation.DerivePrivatekey(LocalHtlcBasepointSecret, LocalPaymentBasepoint, LocalPerCommitmentPoint);

            LocalHtlckey = KeyDerivation.PublicKeyFromPrivateKey(LocalHtlcsecretkey);
            RemoteHtlckey = KeyDerivation.DerivePublickey(RemoteHtlcBasepoint, LocalPerCommitmentPoint);

            LocalFundingPubkey = KeyDerivation.PublicKeyFromPrivateKey(LocalFundingPrivkey);

            RemoteFundingPubkey = KeyDerivation.PublicKeyFromPrivateKey(RemoteFundingPrivkey);

            CnObscurer = LightningScripts.CommitNumberObscurer(LocalPaymentBasepoint, RemotePaymentBasepoint);

            // dotnet has no uint48 types so we use ulong instead, however ulong (which is uint64) has two
            // more bytes in the array then just drop the last to bytes form the array to compute the hex
            Assert.Equal("0x2bb038521914", Hex.ToString(BitConverter.GetBytes(CnObscurer).Reverse().ToArray().AsSpan().Slice(2)));

            Keyset.LocalRevocationKey = RemoteRevocationKey;
            Keyset.LocalDelayedPaymentKey = LocalDelayedkey;
            Keyset.LocalPaymentKey = Localkey;
            Keyset.RemotePaymentKey = Remotekey;
            Keyset.LocalHtlcKey = LocalHtlckey;
            Keyset.RemoteHtlcKey = RemoteHtlckey;
        }
    }
}