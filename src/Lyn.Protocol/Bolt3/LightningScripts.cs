using System;
using System.Collections.Generic;
using System.Linq;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Common.Hashing;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using NBitcoin;

namespace Lyn.Protocol.Bolt3
{
    public class LightningScripts : ILightningScripts
    {
        public byte[] CreateFundingTransactionScript(PublicKey pubkey1, PublicKey pubkey2)
        {
            var list = new List<byte[]> { pubkey1, pubkey2 };

            list.Sort(new LexicographicByteComparer());

            var script = new Script(
                 OpcodeType.OP_2,
                 Op.GetPushOp(list.First()),
                 Op.GetPushOp(list.Last()),
               OpcodeType.OP_2,
               OpcodeType.OP_CHECKMULTISIG
            );

            return script.ToBytes();
        }

        /* BOLT #3:
         *
         * This output sends funds back to the owner of this commitment transaction and
         * thus must be timelocked using `OP_CHECKSEQUENCEVERIFY`. It can be claimed, without delay,
         * by the other party if they know the revocation private key. The output is a
         * version-0 P2WSH, with a witness script:
         *
         *     OP_IF
         *         # Penalty transaction
         *         <revocationpubkey>
         *     OP_ELSE
         *         `to_self_delay`
         *         OP_CHECKSEQUENCEVERIFY
         *         OP_DROP
         *         <local_delayedpubkey>
         *     OP_ENDIF
         *     OP_CHECKSIG
         */

        public byte[] GetRevokeableRedeemscript(PublicKey revocationKey, ushort contestDelay, PublicKey broadcasterDelayedPaymentKey)
        {
            var script = new Script(
               OpcodeType.OP_IF,
               Op.GetPushOp(revocationKey),
               OpcodeType.OP_ELSE,
               Op.GetPushOp(contestDelay),
               OpcodeType.OP_CHECKSEQUENCEVERIFY,
               OpcodeType.OP_DROP,
               Op.GetPushOp(broadcasterDelayedPaymentKey),
               OpcodeType.OP_ENDIF,
               OpcodeType.OP_CHECKSIG);

            return script.ToBytes();
        }

        public byte[] AnchorToRemoteRedeem(PublicKey remoteKey)
        {
            var script = new Script(
               Op.GetPushOp(remoteKey),
               OpcodeType.OP_CHECKSIGVERIFY,
               Op.GetPushOp(1),
               OpcodeType.OP_CHECKSEQUENCEVERIFY);

            return script.ToBytes();
        }

        public byte[] AnchorOutput(PublicKey fundingPubkey)
        {
            // BOLT3:
            // `to_local_anchor` and `to_remote_anchor` Output (option_anchor_outputs):
            //    <local_funding_pubkey/remote_funding_pubkey> OP_CHECKSIG OP_IFDUP
            //    OP_NOTIF
            //        OP_16 OP_CHECKSEQUENCEVERIFY
            //    OP_ENDIF

            var script = new Script(
               Op.GetPushOp(fundingPubkey),
               OpcodeType.OP_CHECKSIG,
               OpcodeType.OP_IFDUP,
               OpcodeType.OP_NOTIF,
               Op.GetPushOp(16),
               OpcodeType.OP_CHECKSEQUENCEVERIFY,
               OpcodeType.OP_ENDIF);

            return script.ToBytes();
        }

        /* BOLT #3:
         *
         * #### Offered HTLC Outputs
         *
         * This output sends funds to either an HTLC-timeout transaction after the
         * HTLC-timeout or to the remote node using the payment preimage or the
         * revocation key. The output is a P2WSH, with a witness script (no
         * option_anchor_outputs):
         *
         *     # To remote node with revocation key
         *     OP_DUP OP_HASH160 <RIPEMD160(SHA256(revocationpubkey))> OP_EQUAL
         *     OP_IF
         *         OP_CHECKSIG
         *     OP_ELSE
         *         <remote_htlcpubkey> OP_SWAP OP_SIZE 32 OP_EQUAL
         *         OP_NOTIF
         *             # To local node via HTLC-timeout transaction (timelocked).
         *             OP_DROP 2 OP_SWAP <local_htlcpubkey> 2 OP_CHECKMULTISIG
         *         OP_ELSE
         *             # To remote node with preimage.
         *             OP_HASH160 <RIPEMD160(payment_hash)> OP_EQUALVERIFY
         *             OP_CHECKSIG
         *         OP_ENDIF
         *     OP_ENDIF
         *
         * Or, with `option_anchor_outputs`:
         *
         *  # To remote node with revocation key
         *  OP_DUP OP_HASH160 <RIPEMD160(SHA256(revocationpubkey))> OP_EQUAL
         *  OP_IF
         *      OP_CHECKSIG
         *  OP_ELSE
         *      <remote_htlcpubkey> OP_SWAP OP_SIZE 32 OP_EQUAL
         *      OP_NOTIF
         *          # To local node via HTLC-timeout transaction (timelocked).
         *          OP_DROP 2 OP_SWAP <local_htlcpubkey> 2 OP_CHECKMULTISIG
         *      OP_ELSE
         *          # To remote node with preimage.
         *          OP_HASH160 <RIPEMD160(payment_hash)> OP_EQUALVERIFY
         *          OP_CHECKSIG
         *      OP_ENDIF
         *      1 OP_CHECKSEQUENCEVERIFY OP_DROP
         *  OP_ENDIF
         */

        public byte[] GetHtlcOfferedRedeemscript(
           PublicKey localhtlckey,
           PublicKey remotehtlckey,
           UInt256 paymenthash,
           PublicKey revocationkey,
           bool optionAnchorOutputs)
        {
            // todo: dan - move this to a hashing interface
            byte[]? paymentHash160 = NBitcoin.Crypto.Hashes.RIPEMD160(paymenthash.GetBytes().ToArray());
            byte[]? revocationKey256 = NBitcoin.Crypto.Hashes.SHA256(revocationkey);
            byte[]? revocationKey160 = NBitcoin.Crypto.Hashes.RIPEMD160(revocationKey256);

            List<Op> ops = new List<Op>
            {
               OpcodeType.OP_DUP,
               OpcodeType.OP_HASH160,
               Op.GetPushOp(revocationKey160),
               OpcodeType.OP_EQUAL,
               OpcodeType.OP_IF,
               OpcodeType.OP_CHECKSIG,
               OpcodeType.OP_ELSE,
               Op.GetPushOp(remotehtlckey),
               OpcodeType.OP_SWAP,
               OpcodeType.OP_SIZE,
               Op.GetPushOp(32),
               OpcodeType.OP_EQUAL,
               OpcodeType.OP_NOTIF,
               OpcodeType.OP_DROP,
               Op.GetPushOp(2),
               OpcodeType.OP_SWAP,
               Op.GetPushOp(localhtlckey),
               Op.GetPushOp(2),
               OpcodeType.OP_CHECKMULTISIG,
               OpcodeType.OP_ELSE,
               OpcodeType.OP_HASH160,
               Op.GetPushOp(paymentHash160),
               OpcodeType.OP_EQUALVERIFY,
               OpcodeType.OP_CHECKSIG,
               OpcodeType.OP_ENDIF,
               OpcodeType.OP_ENDIF
         };

            if (optionAnchorOutputs)
            {
                ops.Insert(ops.Count - 1, Op.GetPushOp(1));
                ops.Insert(ops.Count - 1, OpcodeType.OP_CHECKSEQUENCEVERIFY);
                ops.Insert(ops.Count - 1, OpcodeType.OP_DROP);
            }

            var script = new Script(ops);
            return script.ToBytes();
        }

        /* BOLT #3:
         *
         * #### Received HTLC Outputs
         *
         * This output sends funds to either the remote node after the HTLC-timeout or
         * using the revocation key, or to an HTLC-success transaction with a
         * successful payment preimage. The output is a P2WSH, with a witness script
         * (no `option_anchor_outputs`):
         *
         *     # To remote node with revocation key
         *     OP_DUP OP_HASH160 <RIPEMD160(SHA256(revocationpubkey))> OP_EQUAL
         *     OP_IF
         *         OP_CHECKSIG
         *     OP_ELSE
         *         <remote_htlcpubkey> OP_SWAP
         *             OP_SIZE 32 OP_EQUAL
         *         OP_IF
         *             # To local node via HTLC-success transaction.
         *             OP_HASH160 <RIPEMD160(payment_hash)> OP_EQUALVERIFY
         *             2 OP_SWAP <local_htlcpubkey> 2 OP_CHECKMULTISIG
         *         OP_ELSE
         *             # To remote node after timeout.
         *             OP_DROP <cltv_expiry> OP_CHECKLOCKTIMEVERIFY OP_DROP
         *             OP_CHECKSIG
         *         OP_ENDIF
         *     OP_ENDIF
         *
         * Or, with `option_anchor_outputs`:
         *
         *  # To remote node with revocation key
         *  OP_DUP OP_HASH160 <RIPEMD160(SHA256(revocationpubkey))> OP_EQUAL
         *  OP_IF
         *      OP_CHECKSIG
         *  OP_ELSE
         *      <remote_htlcpubkey> OP_SWAP OP_SIZE 32 OP_EQUAL
         *      OP_IF
         *          # To local node via HTLC-success transaction.
         *          OP_HASH160 <RIPEMD160(payment_hash)> OP_EQUALVERIFY
         *          2 OP_SWAP <local_htlcpubkey> 2 OP_CHECKMULTISIG
         *      OP_ELSE
         *          # To remote node after timeout.
         *          OP_DROP <cltv_expiry> OP_CHECKLOCKTIMEVERIFY OP_DROP
         *          OP_CHECKSIG
         *      OP_ENDIF
         *      1 OP_CHECKSEQUENCEVERIFY OP_DROP
         *  OP_ENDIF
         */

        public byte[] GetHtlcReceivedRedeemscript(
           ulong expirylocktime,
           PublicKey localhtlckey,
           PublicKey remotehtlckey,
           UInt256 paymenthash,
           PublicKey revocationkey,
           bool optionAnchorOutputs)
        {
            // todo: dan - move this to a hashing interface
            byte[]? paymentHash160 = NBitcoin.Crypto.Hashes.RIPEMD160(paymenthash.GetBytes().ToArray());
            byte[]? revocationKey256 = NBitcoin.Crypto.Hashes.SHA256(revocationkey);
            byte[]? revocationKey160 = NBitcoin.Crypto.Hashes.RIPEMD160(revocationKey256);

            List<Op> ops = new List<Op>
         {
            OpcodeType.OP_DUP,
            OpcodeType.OP_HASH160,
            Op.GetPushOp(revocationKey160),
            OpcodeType.OP_EQUAL,
            OpcodeType.OP_IF,
            OpcodeType.OP_CHECKSIG,
            OpcodeType.OP_ELSE,
            Op.GetPushOp(remotehtlckey),
            OpcodeType.OP_SWAP,
            OpcodeType.OP_SIZE,
            Op.GetPushOp(32),
            OpcodeType.OP_EQUAL,
            OpcodeType.OP_IF,
            OpcodeType.OP_HASH160,
            Op.GetPushOp(paymentHash160),
            OpcodeType.OP_EQUALVERIFY,
            Op.GetPushOp(2),
            OpcodeType.OP_SWAP,
            Op.GetPushOp(localhtlckey),
            Op.GetPushOp(2),
            OpcodeType.OP_CHECKMULTISIG,
            OpcodeType.OP_ELSE,
            OpcodeType.OP_DROP,
            Op.GetPushOp((long)expirylocktime),
            OpcodeType.OP_CHECKLOCKTIMEVERIFY,
            OpcodeType.OP_DROP,
            OpcodeType.OP_CHECKSIG,
            OpcodeType.OP_ENDIF,
            OpcodeType.OP_ENDIF
      };

            if (optionAnchorOutputs)
            {
                ops.Insert(ops.Count - 1, Op.GetPushOp(1));
                ops.Insert(ops.Count - 1, OpcodeType.OP_CHECKSEQUENCEVERIFY);
                ops.Insert(ops.Count - 1, OpcodeType.OP_DROP);
            }

            var script = new Script(ops);
            return script.ToBytes();
        }

        public ulong CommitNumberObscurer(
           PublicKey openerPaymentBasepoint,
           PublicKey accepterPaymentBasepoint)
        {
            Span<byte> bytes = stackalloc byte[66];
            openerPaymentBasepoint.GetSpan().CopyTo(bytes);
            accepterPaymentBasepoint.GetSpan().CopyTo(bytes.Slice(33));

            ReadOnlySpan<byte> hashed = HashGenerator.Sha256(bytes);

            // the lower 48 bits of the hash above
            Span<byte> output = stackalloc byte[6];
            hashed.Slice(26).CopyTo(output);

            Uint48 ret = new Uint48(output);//  BitConverter.ToUInt64(output);

            Span<byte> output2 = stackalloc byte[8];
            hashed.Slice(26).CopyTo(output2.Slice(2));
            output2.Reverse();

            ulong n2 = BitConverter.ToUInt64(output2);
            return n2;
            //return ret;
        }

        public byte[] FundingRedeemScript(PublicKey pubkey1, PublicKey pubkey2)
        {
            var comparer = new LexicographicByteComparer();

            ReadOnlySpan<byte> first, second;
            if (comparer.Compare(pubkey1, pubkey2) < 0)
            {
                first = pubkey1.GetSpan();
                second = pubkey2.GetSpan();
            }
            else
            {
                first = pubkey2.GetSpan();
                second = pubkey1.GetSpan();
            }

            List<Op> ops = new List<Op>
         {
            Op.GetPushOp(2),
            Op.GetPushOp(first.ToArray()),
            Op.GetPushOp(second.ToArray()),
            Op.GetPushOp(2),
            OpcodeType.OP_CHECKMULTISIG
         };

            var script = new Script(ops);
            return script.ToBytes();
        }

        public void SetCommitmentInputWitness(TransactionInput transactionInput, BitcoinSignature localSignature, BitcoinSignature remoteSignature, byte[] pubkeyScriptToRedeem)
        {
            var redeemScript = new Script(
                  Op.GetPushOp(0),
               Op.GetPushOp(localSignature),
               Op.GetPushOp(remoteSignature),
               Op.GetPushOp(pubkeyScriptToRedeem))
               .ToWitScript();

            transactionInput.ScriptWitness = new TransactionWitness
            {
                Components = redeemScript.Pushes.Select(opcode => new TransactionWitnessComponent { RawData = opcode }).ToArray()
            };
        }

        public void SetHtlcSuccessInputWitness(TransactionInput transactionInput, BitcoinSignature localSignature, BitcoinSignature remoteSignature, Preimage preimage, byte[] pubkeyScriptToRedeem)
        {
            var redeemScript = new Script(
                  Op.GetPushOp(0),
                  Op.GetPushOp(remoteSignature),
                  Op.GetPushOp(localSignature),
                  Op.GetPushOp(preimage),
                  Op.GetPushOp(pubkeyScriptToRedeem))
               .ToWitScript();

            transactionInput.ScriptWitness = new TransactionWitness
            {
                Components = redeemScript.Pushes.Select(opcode => new TransactionWitnessComponent { RawData = opcode }).ToArray()
            };
        }

        public void SetHtlcTimeoutInputWitness(TransactionInput transactionInput, BitcoinSignature localSignature, BitcoinSignature remoteSignature, byte[] pubkeyScriptToRedeem)
        {
            var redeemScript = new Script(
                  Op.GetPushOp(0),
                  Op.GetPushOp(remoteSignature),
                  Op.GetPushOp(localSignature),
                  Op.GetPushOp(0),
                  Op.GetPushOp(pubkeyScriptToRedeem))
               .ToWitScript();

            transactionInput.ScriptWitness = new TransactionWitness
            {
                Components = redeemScript.Pushes.Select(opcode => new TransactionWitnessComponent { RawData = opcode }).ToArray()
            };
        }
    }
}