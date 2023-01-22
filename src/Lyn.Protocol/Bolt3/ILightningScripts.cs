using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt3
{
    public interface ILightningScripts
    {
        byte[] FundingWitnessScript(PublicKey pubkey1, PublicKey pubkey2);

        byte[] GetRevokeableRedeemscript(PublicKey revocationKey, ushort contestDelay, PublicKey broadcasterDelayedPaymentKey);

        byte[] AnchorToRemoteRedeem(PublicKey remoteKey);

        byte[] AnchorOutput(PublicKey fundingPubkey);

        byte[] GetHtlcOfferedRedeemscript(
            PublicKey localhtlckey,
            PublicKey remotehtlckey,
            UInt256 paymenthash,
            PublicKey revocationkey,
            bool optionAnchorOutputs);

        byte[] GetHtlcReceivedRedeemscript(
            ulong expirylocktime,
            PublicKey localhtlckey,
            PublicKey remotehtlckey,
            UInt256 paymenthash,
            PublicKey revocationkey,
            bool optionAnchorOutputs);

        ulong CommitNumberObscurer(
            PublicKey openerPaymentBasepoint,
            PublicKey accepterPaymentBasepoint);

        byte[] FundingRedeemScript(PublicKey pubkey1, PublicKey pubkey2);

        void SetCommitmentInputWitness(TransactionInput transactionInput, BitcoinSignature localSignature, BitcoinSignature remoteSignature, byte[] pubkeyScriptToRedeem);

        void SetHtlcSuccessInputWitness(TransactionInput transactionInput, BitcoinSignature localSignature, BitcoinSignature remoteSignature, Preimage preimage, byte[] pubkeyScriptToRedeem);

        void SetHtlcTimeoutInputWitness(TransactionInput transactionInput, BitcoinSignature localSignature, BitcoinSignature remoteSignature, byte[] pubkeyScriptToRedeem);
        TransactionWitness CreateClosingTransactionWitnessScript(BitcoinSignature pubkey1, BitcoinSignature pubkey2);
    }
}