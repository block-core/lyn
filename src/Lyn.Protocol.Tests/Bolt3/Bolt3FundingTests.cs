using System.Linq;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Messages;
using Lyn.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using NBitcoin.DataEncoders;
using Xunit;
using Block = NBitcoin.Block;
using Transaction = NBitcoin.Transaction;

#pragma warning disable IDE1006 // Naming Styles

namespace Lyn.Protocol.Tests.Bolt3
{
    /// <summary>
    /// Tests for https://github.com/lightningnetwork/lightning-rfc/blob/master/03-transactions.md#appendix-b-funding-transaction-test-vectors
    /// </summary>
    public class Bolt3FundingTests
    {
        [Fact]
        public void AppendixBFundingWitnessScriptTest()
        {
            var lightningScripts = new LightningScripts();

            byte[] localFundingPubkey = Hex.FromString("023da092f6980e58d2c037173180e9a465476026ee50f96695963e8efe436f54eb");
            byte[] remoteFundingPubkey = Hex.FromString("030e9f7b623d2ccc7c9bd44d66d5ce21ce504c0acf6385a132cec6d3c39fa711c1");

            byte[] script = lightningScripts.FundingRedeemScript(new PublicKey(localFundingPubkey), new PublicKey(remoteFundingPubkey));

            Assert.Equal(script, Hex.FromString("5221023da092f6980e58d2c037173180e9a465476026ee50f96695963e8efe436f54eb21030e9f7b623d2ccc7c9bd44d66d5ce21ce504c0acf6385a132cec6d3c39fa711c152ae"));
        }

        [Fact]
        public void AppendixBCreateFundingTransactionTest()
        {
            var block0 = Block.Load(Hex.FromString("0100000000000000000000000000000000000000000000000000000000000000000000003ba3edfd7a7b12b27ac72c3e67768f617fc81bc3888a51323a9fb8aa4b1e5e4adae5494dffff7f20020000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4d04ffff001d0104455468652054696d65732030332f4a616e2f32303039204368616e63656c6c6f72206f6e206272696e6b206f66207365636f6e64206261696c6f757420666f722062616e6b73ffffffff0100f2052a01000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000"), Consensus.RegTest);
            var block1 = Block.Load(Hex.FromString("0000002006226e46111a0b59caaf126043eb5bbf28c34f3a5e332a1fc7b2b73cf188910fadbb20ea41a8423ea937e76e8151636bf6093b70eaff942930d20576600521fdc30f9858ffff7f20000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff03510101ffffffff0100f2052a010000001976a9143ca33c2e4446f4a305f23c80df8ad1afdcf652f988ac00000000"), Consensus.RegTest);

            var block1Privkey = new Key(Hex.FromString("6bd078650fcee8444e4e09825227b801a1ca928debb750eb36e6d56124bb20e801").Take(32).ToArray());

            var lightningScripts = new LightningScripts();

            byte[] localFundingPubkey = Hex.FromString("023da092f6980e58d2c037173180e9a465476026ee50f96695963e8efe436f54eb");
            byte[] remoteFundingPubkey = Hex.FromString("030e9f7b623d2ccc7c9bd44d66d5ce21ce504c0acf6385a132cec6d3c39fa711c1");

            byte[] script = lightningScripts.FundingWitnessScript(new PublicKey(localFundingPubkey), new PublicKey(remoteFundingPubkey));

            Script scriptWit = new Script(script);

            var trx = Transaction.Parse(
                "0200000001adbb20ea41a8423ea937e76e8151636bf6093b70eaff942930d20576600521fd000000006b48304502210090587b6201e166ad6af0227d3036a9454223d49a1f11839c1a362184340ef0240220577f7cd5cca78719405cbf1de7414ac027f0239ef6e214c90fcaab0454d84b3b012103535b32d5eb0a6ed0982a0479bbadc9868d9836f6ba94dd5a63be16d875069184ffffffff028096980000000000220020c015c4a6be010e21657068fc2e6a9d02b27ebe4d490a25846f7237f104d1a3cd20256d29010000001600143ca33c2e4446f4a305f23c80df8ad1afdcf652f900000000", NBitcoin.Network.RegTest);

            var ci = new ServiceCollection().AddSerializationComponents().BuildServiceProvider();
            var serializationFactory = new SerializationFactory(ci);

            Types.Bitcoin.Transaction newtrx = new Types.Bitcoin.Transaction
            {
                Version = 2,
                LockTime = 0,
                Inputs = new TransactionInput[1]
                {
                    new TransactionInput
                    {
                        PreviousOutput = new Types.Bitcoin.OutPoint
                        {
                            Hash = new UInt256("fd2105607605d2302994ffea703b09f66b6351816ee737a93e42a841ea20bbad"),
                            Index = 0,
                        },
                        Sequence = 4294967295,
                    },
                },
                Outputs = new TransactionOutput[]
                {
                    new TransactionOutput
                    {
                        PublicKeyScript = script,
                        Value = 10000000
                    },
                    new TransactionOutput
                    {
                        PublicKeyScript = new BitcoinWitPubKeyAddress("bcrt1q8j3nctjygm62xp0j8jqdlzk34lw0v5hejct6md", NBitcoin.Network.RegTest).ScriptPubKey.ToBytes(),//  new Script("0 3ca33c2e4446f4a305f23c80df8ad1afdcf652f9").ToBytes(),
                        Value = 4989986080
                    }
                }
            };

            var newtrxToSign = Transaction.Parse(Hex.ToString(serializationFactory.Serialize(newtrx)), NBitcoin.Network.RegTest);

            uint256? newtrxToSignHash = newtrxToSign.GetSignatureHash(
                block1.Transactions[0].Outputs[0].ScriptPubKey,
                0,
                SigHash.All,
                block1.Transactions[0].Outputs[0],
                HashVersion.Original);

            TransactionSignature? newtrxToSignSig = block1Privkey.Sign(newtrxToSignHash, SigHash.All, useLowR: false);

            newtrx.Inputs[0].SignatureScript = new Script(
                Op.GetPushOp(newtrxToSignSig.ToBytes()),
                Op.GetPushOp(block1Privkey.PubKey.ToBytes()))
                .ToBytes();

            Assert.Equal(trx.ToHex(), Hex.ToString(serializationFactory.Serialize(newtrx)));

            // Check that the funding transaction scripts are equal.
            Assert.Equal(trx.Outputs[0].ScriptPubKey, scriptWit);
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles