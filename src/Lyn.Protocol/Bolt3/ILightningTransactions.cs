using Lyn.Protocol.Bolt3.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt3
{
    public interface ILightningTransactions
    {
        CommitmenTransactionOut CommitmentTransaction(CommitmentTransactionIn commitmentTransactionIn);

        CompressedSignature SignInput(Transaction transaction, PrivateKey privateKey, uint inputIndex, byte[] redeemScript, Satoshis amountSats, bool anchorOutputs = false);

        BitcoinSignature FromCompressedSignature(CompressedSignature compressedSignature);

        CompressedSignature SignInputCompressed(Transaction transaction, PrivateKey privateKey, uint inputIndex,
            byte[] redeemScript, Satoshis amountSats, bool anchorOutputs = false);

        Transaction HtlcSuccessTransaction(HtlcTransactionIn htlcTransactionIn);

        Transaction HtlcTimeoutTransaction(HtlcTransactionIn htlcTransactionIn);

        Satoshis HtlcTimeoutFee(bool optionAnchorOutputs, Satoshis feeratePerKw);

        Satoshis HtlcSuccessFee(bool optionAnchorOutputs, Satoshis feeratePerKw);

        Satoshis GetBaseFee(Satoshis feeratePerKw, bool optionAnchorOutputs, int htlcCount);

        Transaction ClosingTransaction(ClosingTransactionIn closingTransactionIn);
    }
}