using System.Threading.Tasks;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.Wallet
{
    public interface IWalletTransactions
    {
        Task<bool> IsAmountAvailableAsync(Satoshis amount);

        Task<Transaction> GenerateTransactionForOutputAsync(TransactionOutput transactionOutput);

        Task PublishTransactionAsync(Transaction transaction);

        Task<Transaction?> GetTransactionByIdAsync(UInt256 transactionId);
        
        Task<ShortChannelId> LookupShortChannelIdByTransactionHashAsync(UInt256 hash, ushort outputIndex);
        Task<long> GetMinimumFeeAsync();
    }
}