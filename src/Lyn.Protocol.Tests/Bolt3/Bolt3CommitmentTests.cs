using System;
using System.Collections.Generic;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Hashing;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using NBitcoin;
using Xunit;
using OutPoint = Lyn.Types.Bitcoin.OutPoint;
using Transaction = Lyn.Types.Bitcoin.Transaction;

#pragma warning disable IDE1006 // Naming Styles

namespace Lyn.Protocol.Tests.Bolt3
{
    /// <summary>
    /// Tests for https://github.com/lightningnetwork/lightning-rfc/blob/master/03-transactions.md#appendix-b-funding-transaction-test-vectors
    /// </summary>
    public class Bolt3CommitmentTests : IClassFixture<Bolt3CommitmentTestContext>
    {
        public Bolt3CommitmentTestContext Context { get; set; }

        public Bolt3CommitmentTests(Bolt3CommitmentTestContext context)
        {
            Context = context;
        }

        [Theory]
        [ClassData(typeof(Bolt3AppendixCTestDataStaticRemotekey))]
        public void AppendixC_CommitmentAndHTLCTransactionStaticRemotekeyTest(Bolt3CommitmentTestVectors vectors)
        {
            Context.Keyset.OtherPaymentKey = Context.RemotePaymentBasepoint;
            Context.OptionAnchorOutputs = false;

            Bolt3CommitmentAndHtlcTransactionTest(vectors);
        }

        [Theory]
        [ClassData(typeof(Bolt3AppendixCTestDataNoAnchors))]
        public void AppendixC_CommitmentAndHTLCTransactionNoAnchorsTest(Bolt3CommitmentTestVectors vectors)
        {
            Context.Keyset.OtherPaymentKey = Context.Remotekey;
            Context.OptionAnchorOutputs = false;

            Bolt3CommitmentAndHtlcTransactionTest(vectors);
        }

        [Theory]
        [ClassData(typeof(Bolt3AppendixFTestDataAnchors))]
        public void AppendixF_CommitmentAndHTLCTransactionAnchorsTest(Bolt3CommitmentTestVectors vectors)
        {
            Context.Keyset.OtherPaymentKey = Context.RemotePaymentBasepoint;
            Context.OptionAnchorOutputs = true;

            Bolt3CommitmentAndHtlcTransactionTest(vectors);
        }

        public void Bolt3CommitmentAndHtlcTransactionTest(Bolt3CommitmentTestVectors vectors)
        {
            CommitmenTransactionOut localCommitmenTransactionOut = Context.LightningTransactions.CommitmentTransaction(
               new CommitmentTransactionIn
               {
                   FundingTxout = Context.FundingTxOutpoint,
                   Funding = Context.FundingAmount,
                   LocalFundingKey = Context.LocalFundingPubkey,
                   RemoteFundingKey = Context.RemoteFundingPubkey,
                   Opener = ChannelSide.Local,
                   ToSelfDelay = Context.ToSelfDelay,
                   Keyset = Context.Keyset,
                   FeeratePerKw = vectors.FeeratePerKw,
                   DustLimitSatoshis = Context.DustLimit,
                   SelfPayMsat = vectors.ToLocalMsat,
                   OtherPayMsat = vectors.ToRemoteMsat,
                   Htlcs = vectors.Htlcs.htlcs,
                   CommitmentNumber = Context.CommitmentNumber,
                   CnObscurer = Context.CnObscurer,
                   OptionAnchorOutputs = Context.OptionAnchorOutputs,
                   Side = ChannelSide.Local
               });

            CommitmenTransactionOut remoteCommitmenTransactionOut = Context.LightningTransactions.CommitmentTransaction(
               new CommitmentTransactionIn
               {
                   FundingTxout = Context.FundingTxOutpoint,
                   Funding = Context.FundingAmount,
                   LocalFundingKey = Context.LocalFundingPubkey,
                   RemoteFundingKey = Context.RemoteFundingPubkey,
                   Opener = ChannelSide.Remote,
                   ToSelfDelay = Context.ToSelfDelay,
                   Keyset = Context.Keyset,
                   FeeratePerKw = vectors.FeeratePerKw,
                   DustLimitSatoshis = Context.DustLimit,
                   SelfPayMsat = vectors.ToLocalMsat,
                   OtherPayMsat = vectors.ToRemoteMsat,
                   Htlcs = vectors.Htlcs.invertedhtlcs,
                   CommitmentNumber = Context.CommitmentNumber,
                   CnObscurer = Context.CnObscurer,
                   OptionAnchorOutputs = Context.OptionAnchorOutputs,
                   Side = ChannelSide.Remote
               });

            Transaction? localTransaction = localCommitmenTransactionOut.Transaction;
            Transaction? remoteTransaction = remoteCommitmenTransactionOut.Transaction;

            localTransaction.Hash = Context.TransactionHashCalculator.ComputeHash(localTransaction, 1);
            remoteTransaction.Hash = Context.TransactionHashCalculator.ComputeHash(remoteTransaction, 1);

            Assert.Equal(localTransaction.Hash, remoteTransaction.Hash);

            // == helper code==
            var expectedCommitTx = TransactionHelper.SeriaizeTransaction(Context.TransactionSerializer, Hex.FromString(vectors.OutputCommitTx));
            var newtrx = TransactionHelper.ParseToString(localTransaction);
            var exptrx = TransactionHelper.ParseToString(expectedCommitTx);

            byte[]? fundingWscript = Context.LightningScripts.FundingRedeemScript(Context.LocalFundingPubkey, Context.RemoteFundingPubkey);

            var remoteSignature = Context.LightningTransactions.SignInput(Context.TransactionSerializer, localTransaction, Context.RemoteFundingPrivkey, inputIndex: 0, redeemScript: fundingWscript, Context.FundingAmount);
            var expectedRemoteSignature = Hex.ToString(expectedCommitTx.Inputs[0].ScriptWitness.Components[2].RawData.AsSpan());
            var actualRemoteSignature = Hex.ToString(remoteSignature.GetSpan());
            Assert.Equal(expectedRemoteSignature, actualRemoteSignature);

            var localSignature = Context.LightningTransactions.SignInput(Context.TransactionSerializer, localTransaction, Context.LocalFundingPrivkey, inputIndex: 0, redeemScript: fundingWscript, Context.FundingAmount);
            var expectedLocalSignature = Hex.ToString(expectedCommitTx.Inputs[0].ScriptWitness.Components[1].RawData.AsSpan());
            var actualLocalSignature = Hex.ToString(localSignature.GetSpan());
            Assert.Equal(expectedLocalSignature, actualLocalSignature);

            Context.LightningScripts.SetCommitmentInputWitness(localTransaction.Inputs[0], localSignature, remoteSignature, fundingWscript);

            byte[] localTransactionBytes = TransactionHelper.DeseriaizeTransaction(Context.TransactionSerializer, localTransaction);

            Assert.Equal(vectors.OutputCommitTx, Hex.ToString(localTransactionBytes.AsSpan()).Substring(2));

            /* FIXME: naming here is kind of backwards: local revocation key
             * is derived from remote revocation basepoint, but it's local */

            Keyset keyset = new Keyset
            {
                SelfRevocationKey = Context.RemoteRevocationKey,
                SelfDelayedPaymentKey = Context.LocalDelayedkey,
                SelfPaymentKey = Context.Localkey,
                OtherPaymentKey = Context.Remotekey,
                SelfHtlcKey = Context.LocalHtlckey,
                OtherHtlcKey = Context.RemoteHtlckey,
            };

            int htlcOutputIndex = 0;
            for (int htlcIndex = 0; htlcIndex < localCommitmenTransactionOut.Htlcs.Count; htlcIndex++)
            {
                HtlcToOutputMaping htlc = localCommitmenTransactionOut.Htlcs[htlcIndex];

                if (htlc.Htlc == null)
                {
                    htlcIndex++;
                    continue;
                }

                OutPoint outPoint = new OutPoint { Hash = localTransaction.Hash, Index = (uint)htlcIndex };
                Transaction htlcTransaction;
                byte[] redeemScript;
                if (htlc.Htlc.Side == ChannelSide.Local)
                {
                    redeemScript = Context.LightningScripts.GetHtlcOfferedRedeemscript(
                                   Context.LocalHtlckey,
                                   Context.RemoteHtlckey,
                                   htlc.Htlc.Rhash,
                                   Context.RemoteRevocationKey,
                                   Context.OptionAnchorOutputs);

                    htlcTransaction = Context.LightningTransactions.CreateHtlcTimeoutTransaction(
                       new CreateHtlcTransactionIn
                       {
                           OptionAnchorOutputs = Context.OptionAnchorOutputs,
                           FeeratePerKw = vectors.FeeratePerKw,
                           AmountMsat = htlc.Htlc.AmountMsat,
                           CommitOutPoint = outPoint,
                           RevocationPubkey = keyset.SelfRevocationKey,
                           LocalDelayedkey = keyset.SelfDelayedPaymentKey,
                           ToSelfDelay = Context.ToSelfDelay,
                           CltvExpiry = (uint)htlc.CltvExpirey
                       });
                }
                else
                {
                    redeemScript = Context.LightningScripts.GetHtlcReceivedRedeemscript(
                                   htlc.Htlc.Expirylocktime,
                                   Context.LocalHtlckey,
                                   Context.RemoteHtlckey,
                                   htlc.Htlc.Rhash,
                                   Context.RemoteRevocationKey,
                                   Context.OptionAnchorOutputs);

                    htlcTransaction = Context.LightningTransactions.CreateHtlcSuccessTransaction(
                       new CreateHtlcTransactionIn
                       {
                           OptionAnchorOutputs = Context.OptionAnchorOutputs,
                           FeeratePerKw = vectors.FeeratePerKw,
                           AmountMsat = htlc.Htlc.AmountMsat,
                           CommitOutPoint = outPoint,
                           RevocationPubkey = keyset.SelfRevocationKey,
                           LocalDelayedkey = keyset.SelfDelayedPaymentKey,
                           ToSelfDelay = Context.ToSelfDelay,
                       });
                }

                string expectedHtlcHex = vectors.HtlcTx[htlcOutputIndex++];
                var expectedHtlcOutput = TransactionHelper.SeriaizeTransaction(Context.TransactionSerializer, Hex.FromString(expectedHtlcHex));

                var newhtlctrx = TransactionHelper.ParseToString(htlcTransaction);
                var exphtlctrx = TransactionHelper.ParseToString(expectedHtlcOutput);

                NBitcoin.Transaction? trx = NBitcoin.Network.Main.CreateTransaction();
                trx.FromBytes(Hex.FromString(expectedHtlcHex));

                var htlcRemoteSignature = Context.LightningTransactions.SignInput(
                   Context.TransactionSerializer,
                   htlcTransaction,
                   Context.RemoteHtlcsecretkey,
                   inputIndex: 0,
                   redeemScript: redeemScript,
                   htlc.Htlc.AmountMsat,
                   vectors.RemoteAnchorOutputs ? (SigHash.Single | SigHash.AnyoneCanPay) : SigHash.All);

                var expectedHtlcRemoteSignature = Hex.ToString(expectedHtlcOutput.Inputs[0].ScriptWitness.Components[1].RawData.AsSpan());
                var actualHtlcRemoteSignature = Hex.ToString(htlcRemoteSignature.GetSpan());
                Assert.Equal(expectedHtlcRemoteSignature, actualHtlcRemoteSignature);

                var htlcLocalSignature = Context.LightningTransactions.SignInput(
                   Context.TransactionSerializer,
                   htlcTransaction,
                   Context.LocalHtlcsecretkey,
                   inputIndex: 0,
                   redeemScript: redeemScript,
                   htlc.Htlc.AmountMsat,
                   vectors.LocalAnchorOutputs ? (SigHash.Single | SigHash.AnyoneCanPay) : SigHash.All);

                var expectedHtlcLocalSignature = Hex.ToString(expectedHtlcOutput.Inputs[0].ScriptWitness.Components[2].RawData.AsSpan());
                var actualHtlcLocalSignature = Hex.ToString(htlcLocalSignature.GetSpan());
                Assert.Equal(expectedHtlcLocalSignature, actualHtlcLocalSignature);

                if (htlc.Htlc.Side == ChannelSide.Local)
                {
                    Context.LightningScripts.SetHtlcTimeoutInputWitness(htlcTransaction.Inputs[0], htlcLocalSignature, htlcRemoteSignature, redeemScript);
                }
                else
                {
                    Context.LightningScripts.SetHtlcSuccessInputWitness(htlcTransaction.Inputs[0], htlcLocalSignature, htlcRemoteSignature, htlc.Htlc.R, redeemScript);
                }

                byte[] htlcTransactionBytes = TransactionHelper.DeseriaizeTransaction(Context.TransactionSerializer, htlcTransaction);

                Assert.Equal(expectedHtlcHex, Hex.ToString(htlcTransactionBytes.AsSpan()).Substring(2));
            }
        }

        /* BOLT #3:
   *    htlc 5 direction: local->remote
   *    htlc 5 amount_msat: 5000000
   *    htlc 5 expiry: 505
   *    htlc 5 payment_preimage: 0505050505050505050505050505050505050505050505050505050505050505
   *    htlc 6 direction: local->remote
   *    htlc 6 amount_msat: 5000000
   *    htlc 6 expiry: 506
   *    htlc 6 payment_preimage: 0505050505050505050505050505050505050505050505050505050505050505
  */

        public static (List<Htlc>, List<Htlc>) Setup_htlcs_1_5_and_6()
        {
            List<Htlc> htlcs = new List<Htlc>
         {
            new Htlc
            {
               State = HtlcState.RcvdAddAckRevocation,
               AmountMsat = 2000000,
               Expirylocktime = 501,
               R = new Preimage(Hex.FromString("0101010101010101010101010101010101010101010101010101010101010101")),
            },

            new Htlc
            {
               State = HtlcState.SentAddAckRevocation,
               AmountMsat = 5000000,
               Expirylocktime = 505,
               R = new Preimage(Hex.FromString("0505050505050505050505050505050505050505050505050505050505050505")),
            },
            new Htlc
            {
               State = HtlcState.SentAddAckRevocation,
               AmountMsat = 5000000,
               Expirylocktime = 506,
               R = new Preimage(Hex.FromString("0505050505050505050505050505050505050505050505050505050505050505")),
            },
         };

            foreach (Htlc htlc in htlcs)
            {
                htlc.Rhash = new UInt256(HashGenerator.Sha256(htlc.R));
            }

            List<Htlc>? inverted = InvertHtlcs(htlcs);

            return (htlcs, inverted);
        }

        /* BOLT #3:
       *
       *    htlc 0 direction: remote.local
       *    htlc 0 amount_msat: 1000000
       *    htlc 0 expiry: 500
       *    htlc 0 payment_preimage: 0000000000000000000000000000000000000000000000000000000000000000
       *    htlc 1 direction: remote.local
       *    htlc 1 amount_msat: 2000000
       *    htlc 1 expiry: 501
       *    htlc 1 payment_preimage: 0101010101010101010101010101010101010101010101010101010101010101
       *    htlc 2 direction: local.remote
       *    htlc 2 amount_msat: 2000000
       *    htlc 2 expiry: 502
       *    htlc 2 payment_preimage: 0202020202020202020202020202020202020202020202020202020202020202
       *    htlc 3 direction: local.remote
       *    htlc 3 amount_msat: 3000000
       *    htlc 3 expiry: 503
       *    htlc 3 payment_preimage: 0303030303030303030303030303030303030303030303030303030303030303
       *    htlc 4 direction: remote.local
       *    htlc 4 amount_msat: 4000000
       *    htlc 4 expiry: 504
       *    htlc 4 payment_preimage: 0404040404040404040404040404040404040404040404040404040404040404
       */

        public static (List<Htlc>, List<Htlc>) Setup_htlcs_0_to_4()
        {
            List<Htlc> htlcs = new List<Htlc>
         {
            new Htlc
            {
               State = HtlcState.RcvdAddAckRevocation,
               AmountMsat = 1000000,
               Expirylocktime = 500,
               R = new Preimage(Hex.FromString("0000000000000000000000000000000000000000000000000000000000000000")),
            },
            new Htlc
            {
               State = HtlcState.RcvdAddAckRevocation,
               AmountMsat = 2000000,
               Expirylocktime = 501,
               R = new Preimage(Hex.FromString("0101010101010101010101010101010101010101010101010101010101010101")),
            },
            new Htlc
            {
               State = HtlcState.SentAddAckRevocation,
               AmountMsat = 2000000,
               Expirylocktime = 502,
               R = new Preimage(Hex.FromString("0202020202020202020202020202020202020202020202020202020202020202")),
            },
            new Htlc
            {
               State = HtlcState.SentAddAckRevocation,
               AmountMsat = 3000000,
               Expirylocktime = 503,
               R = new Preimage(Hex.FromString("0303030303030303030303030303030303030303030303030303030303030303")),
            },
            new Htlc
            {
               State = HtlcState.RcvdAddAckRevocation,
               AmountMsat = 4000000,
               Expirylocktime = 504,
               R = new Preimage(Hex.FromString("0404040404040404040404040404040404040404040404040404040404040404")),
            },
         };

            foreach (Htlc htlc in htlcs)
            {
                htlc.Rhash = new UInt256(HashGenerator.Sha256(htlc.R));
            }

            List<Htlc>? inverted = InvertHtlcs(htlcs);

            return (htlcs, inverted);
        }

        /* HTLCs as seen from other side. */

        public static List<Htlc> InvertHtlcs(List<Htlc> htlcs)
        {
            List<Htlc> htlcsinv = new List<Htlc>(htlcs.Count);

            for (int i = 0; i < htlcs.Count; i++)
            {
                Htlc htlc = htlcs[i];

                Htlc inv = new Htlc
                {
                    AmountMsat = htlc.AmountMsat,
                    Expirylocktime = htlc.Expirylocktime,
                    Id = htlc.Id,
                    R = htlc.R,
                    Rhash = htlc.Rhash,
                    State = htlc.State,
                };

                if (inv.State == HtlcState.RcvdAddAckRevocation)
                {
                    htlc.State = HtlcState.SentAddAckRevocation;
                }
                else
                {
                    Assert.True(inv.State == HtlcState.SentAddAckRevocation);
                    htlc.State = HtlcState.RcvdAddAckRevocation;
                }

                htlcsinv.Add(inv);
            }

            return htlcsinv;
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles