using System.Threading.Tasks;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.Wallet
{
    public interface IWalletTransactions
    {
        Task<bool> IsAmountAvailableAsync(Satoshis amount);

        Task<Transaction> GenerateTransactionForOutputAsync(TransactionOutput transactionOutput);

        Task PublishTransactionAsync(Transaction transaction);
    }
}