using Lyn.Protocol.Bolt3.Types;
using Lyn.Types.Fundamental;
using Lyn.Types.Serialization.Serializers;
using NBitcoin;
using Transaction = Lyn.Types.Bitcoin.Transaction;

namespace Lyn.Protocol.Bolt3
{
    public interface ILightningTransactions
    {
        CommitmenTransactionOut CommitmentTransaction(CommitmentTransactionIn commitmentTransactionIn);

        BitcoinSignature SignInput(TransactionSerializer serializer, Transaction transaction, PrivateKey privateKey, uint inputIndex, byte[] redeemScript, Satoshis amountSats, bool anchorOutputs = false);

        Transaction CreateHtlcSuccessTransaction(CreateHtlcTransactionIn createHtlcTransactionIn);

        Transaction CreateHtlcTimeoutTransaction(CreateHtlcTransactionIn createHtlcTransactionIn);

        Satoshis HtlcTimeoutFee(bool optionAnchorOutputs, Satoshis feeratePerKw);

        Satoshis HtlcSuccessFee(bool optionAnchorOutputs, Satoshis feeratePerKw);
    }
}