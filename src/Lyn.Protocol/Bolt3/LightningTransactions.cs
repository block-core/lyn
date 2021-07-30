using System;
using System.Collections.Generic;
using System.Linq;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Transaction = Lyn.Types.Bitcoin.Transaction;

namespace Lyn.Protocol.Bolt3
{
    public class LightningTransactions : ILightningTransactions
    {
        private readonly ILogger<LightningTransactions> _logger;
        private readonly ISerializationFactory _serializationFactory;
        private readonly ILightningScripts _lightningScripts;

        public LightningTransactions(ILogger<LightningTransactions> logger, ISerializationFactory serializationFactory, ILightningScripts lightningScripts)
        {
            _logger = logger;
            _serializationFactory = serializationFactory;
            _lightningScripts = lightningScripts;
        }

        public CommitmenTransactionOut CommitmentTransaction(CommitmentTransactionIn commitmentTransactionIn)
        {
            var str = JsonSerializer.Serialize(commitmentTransactionIn, typeof(CommitmentTransactionIn));
            
            _logger.LogDebug($"Creating the commitment transaction {str}");

            MiliSatoshis totalPayMsat = commitmentTransactionIn.SelfPayMsat + commitmentTransactionIn.OtherPayMsat;
            MiliSatoshis fundingMsat = commitmentTransactionIn.Funding;

            if (totalPayMsat > fundingMsat)
            {
                _logger.LogError($"The total amount {totalPayMsat}msat is greater then the channels capacity of {fundingMsat}msat");
                throw new Exception($"The total amount {totalPayMsat}msat is greater then the channels capacity of {fundingMsat}msat");
            }

            // BOLT3 Commitment Transaction Construction
            // 1. Initialize the commitment transaction input and locktime

            ulong obscured = commitmentTransactionIn.CommitmentNumber ^ commitmentTransactionIn.CnObscurer;

            var transaction = new Transaction
            {
                Version = 2,
                LockTime = (uint)(0x20000000 | (obscured & 0xffffff)),
                Inputs = new[]
                {
                    new TransactionInput
                    {
                        PreviousOutput = commitmentTransactionIn.FundingTxout,
                        Sequence = (uint) (0x80000000 | ((obscured >> 24) & 0xFFFFFF))
                    }
                }
            };

            _logger.LogDebug("Initialize the commitment transaction input {Obscured}, {LockTime}, {Sequence}", obscured, transaction.LockTime, transaction.Inputs[0].Sequence);

            // BOLT3 Commitment Transaction Construction
            // 1. Calculate which committed HTLCs need to be trimmed
            var htlcsUntrimmed = new List<Htlc>();
            foreach (Htlc htlc in commitmentTransactionIn.Htlcs)
            {
                if (htlc.Side == commitmentTransactionIn.Side)
                {
                    /* BOLT #3:
                    *
                    *   - for every offered HTLC:
                    *    - if the HTLC amount minus the HTLC-timeout fee would be less than
                    *    `dust_limit_satoshis` set by the transaction owner:
                    *      - MUST NOT contain that output.
                    *    - otherwise:
                    *      - MUST be generated as specified in
                    *      [Offered HTLC Outputs](#offered-htlc-outputs).
                    */

                    Satoshis htlcFeeTimeoutFee = HtlcTimeoutFee(commitmentTransactionIn.OptionAnchorOutputs, commitmentTransactionIn.FeeratePerKw);

                    Satoshis dustPlustFee = commitmentTransactionIn.DustLimitSatoshis + htlcFeeTimeoutFee;
                    MiliSatoshis dustPlustFeeMsat = dustPlustFee;

                    if (htlc.AmountMsat < dustPlustFeeMsat)
                    {
                        // do not add the htlc outpout
                        _logger.LogDebug("Do not add Htlc {@Htlc}, dust limit{dustPlustFeeMsat}msat", htlc, dustPlustFeeMsat);
                    }
                    else
                    {
                        htlcsUntrimmed.Add(htlc);
                        _logger.LogDebug("Add Htlc {@Htlc}, dust limit{dustPlustFeeMsat}msat", htlc, dustPlustFeeMsat);
                    }
                }
                else
                {
                    /* BOLT #3:
                    *
                    *  - for every received HTLC:
                    *    - if the HTLC amount minus the HTLC-success fee would be less than
                    *    `dust_limit_satoshis` set by the transaction owner:
                    *      - MUST NOT contain that output.
                    *    - otherwise:
                    *      - MUST be generated as specified in
                    */

                    Satoshis htlcFeeSuccessFee = HtlcSuccessFee(commitmentTransactionIn.OptionAnchorOutputs, commitmentTransactionIn.FeeratePerKw);

                    Satoshis dustPlustFee = commitmentTransactionIn.DustLimitSatoshis + htlcFeeSuccessFee;
                    MiliSatoshis dustPlustFeeMsat = dustPlustFee;

                    if (htlc.AmountMsat < dustPlustFeeMsat)
                    {
                        // do not add the htlc outpout
                        _logger.LogDebug("Do not add Htlc {@Htlc}, dust limit{dustPlustFeeMsat}msat", htlc, dustPlustFeeMsat);
                    }
                    else
                    {
                        htlcsUntrimmed.Add(htlc);
                        _logger.LogDebug("Add Htlc {@Htlc}, dust limit{dustPlustFeeMsat}msat", htlc, dustPlustFeeMsat);
                    }
                }
            }

            // BOLT3 Commitment Transaction Construction
            // 1. Calculate the base commitment transaction fee.

            Satoshis baseFee = GetBaseFee(
                commitmentTransactionIn.FeeratePerKw,
                commitmentTransactionIn.OptionAnchorOutputs,
                htlcsUntrimmed.Count);

            /* BOLT #3:
             * If `option_anchor_outputs` applies to the commitment
             * transaction, also subtract two times the fixed anchor size
             * of 330 sats from the funder (either `to_local` or
             * `to_remote`).
             */
            if (commitmentTransactionIn.OptionAnchorOutputs)
            {
                baseFee += 660;
            }

            _logger.LogDebug("baseFee = {baseFee}", baseFee);

            // BOLT3 Commitment Transaction Construction
            // 4. Subtract this base fee from the funder (either to_local or to_remote).
            // If option_anchor_outputs applies to the commitment transaction,
            // also subtract two times the fixed anchor size of 330 sats from the funder (either to_local or to_remote).

            MiliSatoshis baseFeeMsat = (MiliSatoshis)baseFee;

            if (commitmentTransactionIn.Opener == commitmentTransactionIn.Side)
            {
                if (commitmentTransactionIn.SelfPayMsat < baseFeeMsat)
                {
                    commitmentTransactionIn.SelfPayMsat = 0;
                    _logger.LogDebug("SelfPayMsat = {SelfPayMsat}", commitmentTransactionIn.SelfPayMsat);
                }
                else
                {
                    commitmentTransactionIn.SelfPayMsat = commitmentTransactionIn.SelfPayMsat - baseFeeMsat;
                    _logger.LogDebug("SelfPayMsat = {SelfPayMsat}", commitmentTransactionIn.SelfPayMsat);
                }
            }
            else
            {
                if (commitmentTransactionIn.OtherPayMsat < baseFeeMsat)
                {
                    commitmentTransactionIn.OtherPayMsat = 0;
                    _logger.LogDebug("OtherPayMsat = {OtherPayMsat}", commitmentTransactionIn.OtherPayMsat);
                }
                else
                {
                    commitmentTransactionIn.OtherPayMsat = commitmentTransactionIn.OtherPayMsat - baseFeeMsat;
                    _logger.LogDebug("OtherPayMsat = {OtherPayMsat}", commitmentTransactionIn.OtherPayMsat);
                }
            }

            var outputs = new List<HtlcToOutputMaping>();

            // BOLT3 Commitment Transaction Construction
            // 5. For every offered HTLC, if it is not trimmed, add an offered HTLC output.

            foreach (Htlc htlc in htlcsUntrimmed)
            {
                if (htlc.Side == commitmentTransactionIn.Side)
                {
                    // todo round down msat to sat in s common method
                    Satoshis amount = (Satoshis)htlc.AmountMsat;

                    byte[]? wscript = _lightningScripts.GetHtlcOfferedRedeemscript(
                       commitmentTransactionIn.Keyset.LocalHtlcKey,
                       commitmentTransactionIn.Keyset.RemoteHtlcKey,
                       htlc.Rhash,
                       commitmentTransactionIn.Keyset.LocalRevocationKey,
                       commitmentTransactionIn.OptionAnchorOutputs);

                    var wscriptinst = new Script(wscript);

                    Script? p2Wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface

                    _logger.LogDebug("Htlc Amount = {amount}, WitnessScript = {wscriptinst}, p2Wsh = {p2Wsh} ", wscriptinst, amount, p2Wsh);

                    outputs.Add(new HtlcToOutputMaping
                    {
                        TransactionOutput = new TransactionOutput
                        {
                            Value = (long)amount,
                            PublicKeyScript = p2Wsh.ToBytes()
                        },
                        CltvExpirey = htlc.Expirylocktime,
                        WitnessHashRedeemScript = wscript,
                        Htlc = htlc
                    });
                }
            }

            // BOLT3 Commitment Transaction Construction
            // 6. For every offered HTLC, if it is not trimmed, add an offered HTLC output.

            foreach (Htlc htlc in htlcsUntrimmed)
            {
                if (htlc.Side != commitmentTransactionIn.Side)
                {
                    // todo round down msat to sat in s common method
                    Satoshis amount = (Satoshis)htlc.AmountMsat;

                    var wscript = _lightningScripts.GetHtlcReceivedRedeemscript(
                       htlc.Expirylocktime,
                       commitmentTransactionIn.Keyset.LocalHtlcKey,
                       commitmentTransactionIn.Keyset.RemoteHtlcKey,
                       htlc.Rhash,
                       commitmentTransactionIn.Keyset.LocalRevocationKey,
                       commitmentTransactionIn.OptionAnchorOutputs);

                    var wscriptinst = new Script(wscript);

                    var p2Wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface

                    _logger.LogDebug("Htlc Amount = {amount}, WitnessScript = {wscriptinst}, p2Wsh = {p2Wsh} ", wscriptinst, amount, p2Wsh);

                    outputs.Add(new HtlcToOutputMaping
                    {
                        TransactionOutput = new TransactionOutput
                        {
                            Value = (long)amount,
                            PublicKeyScript = p2Wsh.ToBytes()
                        },
                        CltvExpirey = htlc.Expirylocktime,
                        WitnessHashRedeemScript = wscript,
                        Htlc = htlc
                    });
                }
            }

            MiliSatoshis dustLimitMsat = (MiliSatoshis)commitmentTransactionIn.DustLimitSatoshis;

            // BOLT3 Commitment Transaction Construction
            // 7. If the to_local amount is greater or equal to dust_limit_satoshis, add a to_local output.

            bool toLocal = false;
            if (commitmentTransactionIn.SelfPayMsat >= dustLimitMsat)
            {
                // todo round down msat to sat in s common method
                Satoshis amount = (Satoshis)commitmentTransactionIn.SelfPayMsat;

                var wscript = _lightningScripts.GetRevokeableRedeemscript(commitmentTransactionIn.Keyset.LocalRevocationKey, commitmentTransactionIn.ToSelfDelay, commitmentTransactionIn.Keyset.LocalDelayedPaymentKey);

                var wscriptinst = new Script(wscript);

                var p2Wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface

                _logger.LogDebug("Add a to_local output Amount = {amount}, WitnessScript = {wscriptinst}, p2Wsh = {p2Wsh} ", wscriptinst, amount, p2Wsh);

                outputs.Add(new HtlcToOutputMaping
                {
                    TransactionOutput = new TransactionOutput
                    {
                        Value = (long)amount,
                        PublicKeyScript = p2Wsh.ToBytes()
                    },
                    CltvExpirey = commitmentTransactionIn.ToSelfDelay,
                });

                toLocal = true;
            }

            // BOLT3 Commitment Transaction Construction
            // 8. If the to_remote amount is greater or equal to dust_limit_satoshis, add a to_remote output.

            bool toRemote = false;
            if (commitmentTransactionIn.OtherPayMsat >= dustLimitMsat)
            {
                // todo round down msat to sat in s common method
                Satoshis amount = (Satoshis)commitmentTransactionIn.OtherPayMsat;

                // BOLT3:
                // If option_anchor_outputs applies to the commitment transaction,
                // the to_remote output is encumbered by a one block csv lock.
                // <remote_pubkey> OP_CHECKSIGVERIFY 1 OP_CHECKSEQUENCEVERIFY
                // Otherwise, this output is a simple P2WPKH to `remotepubkey`.

                Script p2Wsh;
                if (commitmentTransactionIn.OptionAnchorOutputs)
                {
                    var wscript = _lightningScripts.AnchorToRemoteRedeem(commitmentTransactionIn.Keyset.RemotePaymentKey);

                    var wscriptinst = new Script(wscript);

                    p2Wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface

                    _logger.LogDebug("Add a to_remote output (anchor) Amount = {amount} WitnessScript = {wscriptinst}, p2Wsh = {p2Wsh}", wscriptinst, amount, p2Wsh);
                }
                else
                {
                    PubKey pubkey = new PubKey(commitmentTransactionIn.Keyset.RemotePaymentKey);

                    p2Wsh = PayToWitPubKeyHashTemplate.Instance.GenerateScriptPubKey(pubkey); // todo: dan - move this to interface

                    _logger.LogDebug("Add a to_remote output Amount = {amount} pubkey = {pubkey}, p2Wsh = {p2Wsh}", pubkey, amount, p2Wsh);
                }

                outputs.Add(new HtlcToOutputMaping
                {
                    TransactionOutput = new TransactionOutput
                    {
                        Value = (long)amount,
                        PublicKeyScript = p2Wsh.ToBytes()
                    },
                    CltvExpirey = 0
                });

                toRemote = true;
            }

            // BOLT3 Commitment Transaction Construction
            // 9. If option_anchor_outputs applies to the commitment transaction:
            //   if to_local exists or there are untrimmed HTLCs, add a to_local_anchor output
            //   if to_remote exists or there are untrimmed HTLCs, add a to_remote_anchor output

            if (commitmentTransactionIn.OptionAnchorOutputs)
            {
                if (toLocal || htlcsUntrimmed.Count != 0)
                {
                    // todo round down msat to sat in s common method
                    Satoshis amount = (ulong)330;

                    var wscript = _lightningScripts.AnchorOutput(commitmentTransactionIn.LocalFundingKey);

                    var wscriptinst = new Script(wscript);

                    var p2Wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface

                    _logger.LogDebug("Anchor - toLocal Amount = {amount}, WitnessScript = {wscriptinst}, p2Wsh = {p2Wsh} ", wscriptinst, amount, p2Wsh);

                    outputs.Add(new HtlcToOutputMaping
                    {
                        TransactionOutput = new TransactionOutput
                        {
                            Value = (long)amount,
                            PublicKeyScript = p2Wsh.ToBytes()
                        },
                        CltvExpirey = 0
                    });
                }

                if (toRemote || htlcsUntrimmed.Count != 0)
                {
                    // todo round down msat to sat in s common method
                    Satoshis amount = (ulong)330;

                    var wscript = _lightningScripts.AnchorOutput(commitmentTransactionIn.RemoteFundingKey);

                    var wscriptinst = new Script(wscript);

                    var p2Wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface

                    _logger.LogDebug("Anchor - toRemote Amount = {amount}, WitnessScript = {wscriptinst}, p2Wsh = {p2Wsh} ", wscriptinst, amount, p2Wsh);

                    outputs.Add(new HtlcToOutputMaping
                    {
                        TransactionOutput = new TransactionOutput
                        {
                            Value = (long)amount,
                            PublicKeyScript = p2Wsh.ToBytes()
                        },
                        CltvExpirey = 0
                    });
                }
            }

            // BOLT3 Commitment Transaction Construction
            // 10. Sort the outputs into BIP 69+CLTV order.

            var sorter = new HtlcLexicographicComparer(new LexicographicByteComparer());

            outputs.Sort(sorter);

            transaction.Outputs = outputs.Select(s => s.TransactionOutput).ToArray();

            var result = new CommitmenTransactionOut { Transaction = transaction, Htlcs = outputs };

            _logger.LogDebug("CommitmenTransactionOut {@CommitmenTransactionOut}", result);
            return result;
        }

        public BitcoinSignature SignInput(Transaction transaction, PrivateKey privateKey, uint inputIndex, byte[] redeemScript, Satoshis amountSats, bool anchorOutputs = false)
        {
            // Currently we use NBitcoin to create the transaction hash to be signed,
            // the extra serialization to NBitcoin Transaction is costly so later
            // we will move to generating the hash to sign and signatures directly in code.

            var key = new NBitcoin.Key(privateKey);

            byte[] transactionbytes = _serializationFactory.Serialize(transaction);
            NBitcoin.Transaction? trx = NBitcoin.Network.Main.CreateTransaction();
            trx.FromBytes(transactionbytes);

            // Create the P2WSH redeem script
            var wscript = new Script(redeemScript);
            var utxo = new NBitcoin.TxOut(Money.Satoshis((long)amountSats), wscript.WitHash);
            var outpoint = new NBitcoin.OutPoint(trx.Inputs[inputIndex].PrevOut);
            ScriptCoin witnessCoin = new ScriptCoin(new Coin(outpoint, utxo), wscript);

            SigHash sigHash = anchorOutputs ? (SigHash.Single | SigHash.AnyoneCanPay) : SigHash.All;

            uint256? hashToSign = trx.GetSignatureHash(witnessCoin.GetScriptCode(), (int)inputIndex, sigHash, utxo, HashVersion.WitnessV0);
            TransactionSignature? sig = key.Sign(hashToSign, sigHash, useLowR: false);

            return new BitcoinSignature(sig.ToBytes());
        }
        
        public CompressedSignature SignInputCompressed(Transaction transaction, PrivateKey privateKey, uint inputIndex, byte[] redeemScript, Satoshis amountSats, bool anchorOutputs = false)
        {
            // Currently we use NBitcoin to create the transaction hash to be signed,
            // the extra serialization to NBitcoin Transaction is costly so later
            // we will move to generating the hash to sign and signatures directly in code.

            var key = new NBitcoin.Key(privateKey);

            byte[] transactionbytes = _serializationFactory.Serialize(transaction);
            var trx = Network.Main.CreateTransaction();
            trx.FromBytes(transactionbytes);

            // Create the P2WSH redeem script
            var wscript = new Script(redeemScript);
            var utxo = new TxOut(Money.Satoshis((long)amountSats), wscript.WitHash);
            var outpoint = new NBitcoin.OutPoint(trx.Inputs[inputIndex].PrevOut);
            var witnessCoin = new ScriptCoin(new Coin(outpoint, utxo), wscript);

            var sigHash = anchorOutputs ? (SigHash.Single | SigHash.AnyoneCanPay) : SigHash.All;

            var hashToSign = trx.GetSignatureHash(witnessCoin.GetScriptCode(), (int)inputIndex, sigHash, utxo, HashVersion.WitnessV0);
            var sig = key.SignCompact(hashToSign, true);

            return new CompressedSignature(sig.AsSpan(1).ToArray());
        }

        public Transaction HtlcSuccessTransaction(HtlcTransactionIn htlcTransactionIn)
        {
            htlcTransactionIn.HtlcFee = HtlcSuccessFee(htlcTransactionIn.OptionAnchorOutputs, htlcTransactionIn.FeeratePerKw);
            htlcTransactionIn.Locktime = 0;
            htlcTransactionIn.Sequence = (uint)(htlcTransactionIn.OptionAnchorOutputs ? 1 : 0);

            return CreateHtlcTransaction(htlcTransactionIn);
        }

        public Transaction HtlcTimeoutTransaction(HtlcTransactionIn htlcTransactionIn)
        {
            htlcTransactionIn.HtlcFee = HtlcTimeoutFee(htlcTransactionIn.OptionAnchorOutputs, htlcTransactionIn.FeeratePerKw);
            htlcTransactionIn.Locktime = htlcTransactionIn.CltvExpiry;
            htlcTransactionIn.Sequence = (uint)(htlcTransactionIn.OptionAnchorOutputs ? 1 : 0);

            return CreateHtlcTransaction(htlcTransactionIn);
        }

        private Transaction CreateHtlcTransaction(HtlcTransactionIn htlcTransactionIn)
        {
            var transaction = new Transaction
            {
                Version = 2,
                LockTime = (uint)htlcTransactionIn.Locktime,
                Inputs = new[]
                {
                    new TransactionInput
                    {
                        PreviousOutput = htlcTransactionIn.CommitOutPoint,
                        Sequence = htlcTransactionIn.Sequence,
                    }
                }
            };

            byte[]? wscript = _lightningScripts.GetRevokeableRedeemscript(htlcTransactionIn.RevocationPubkey, htlcTransactionIn.ToSelfDelay, htlcTransactionIn.LocalDelayedkey);
            var wscriptinst = new Script(wscript);
            Script? p2Wsh = PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(new WitScriptId(wscriptinst)); // todo: dan - move this to interface

            Satoshis amountSat = htlcTransactionIn.AmountMsat;
            if (htlcTransactionIn.HtlcFee > amountSat) throw new Exception();
            amountSat -= htlcTransactionIn.HtlcFee;

            transaction.Outputs = new TransactionOutput[]
            {
                new TransactionOutput
                {
                    Value = (long) amountSat,
                    PublicKeyScript = p2Wsh.ToBytes()
                },
            };

            return transaction;
        }

        /* BOLT #3:
        *
        * The fee for an HTLC-timeout transaction:
        * - MUST BE calculated to match:
        *   1. Multiply `feerate_per_kw` by 663 (666 if `option_anchor_outputs`
        *      applies) and divide by 1000 (rounding down).
        */

        public Satoshis HtlcTimeoutFee(bool optionAnchorOutputs, Satoshis feeratePerKw)
        {
            ulong baseTimeOutFee = optionAnchorOutputs ? (ulong)666 : (ulong)663;
            Satoshis htlcFeeTimeoutFee = feeratePerKw * baseTimeOutFee / 1000;
            return htlcFeeTimeoutFee;
        }

        /* BOLT #3:
         *
         * The fee for an HTLC-success transaction:
         * - MUST BE calculated to match:
         *   1. Multiply `feerate_per_kw` by 703 (706 if `option_anchor_outputs`
         *      applies) and divide by 1000 (rounding down).
         */

        public Satoshis HtlcSuccessFee(bool optionAnchorOutputs, Satoshis feeratePerKw)
        {
            ulong baseSuccessFee = optionAnchorOutputs ? (ulong)706 : (ulong)703;
            Satoshis htlcFeeSuccessFee = feeratePerKw * baseSuccessFee / 1000;
            return htlcFeeSuccessFee;
        }

        public Satoshis GetBaseFee(Satoshis feeratePerKw, bool optionAnchorOutputs, int htlcCount)
        {
            ulong weight;
            ulong numUntrimmedHtlcs = (ulong)htlcCount;

            /* BOLT #3:
             *
             * The base fee for a commitment transaction:
             *  - MUST be calculated to match:
             *    1. Start with `weight` = 724 (1124 if `option_anchor_outputs` applies).
             */
            if (optionAnchorOutputs)
                weight = 1124;
            else
                weight = 724;

            /* BOLT #3:
             *
             *    2. For each committed HTLC, if that output is not trimmed as
             *       specified in [Trimmed Outputs](#trimmed-outputs), add 172
             *       to `weight`.
             */
            weight += 172 * numUntrimmedHtlcs;

            Satoshis baseFee = feeratePerKw * weight / 1000;

            return baseFee;
        }

        public Transaction ClosingTransaction(ClosingTransactionIn closingTransactionIn)
        {
            throw new NotImplementedException();
        }
    }
}