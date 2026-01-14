using System.Threading.Tasks;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Common.Blockchain
{
    /// <summary>
    /// Interface for broadcasting transactions to the Bitcoin network.
    /// Implementation is deferred to the wallet project.
    /// </summary>
    public interface ITransactionBroadcaster
    {
        /// <summary>
        /// Broadcasts a transaction to the Bitcoin network.
        /// </summary>
        /// <param name="transaction">The transaction to broadcast.</param>
        /// <returns>The transaction ID if successful.</returns>
        Task<UInt256> BroadcastTransactionAsync(Transaction transaction);

        /// <summary>
        /// Checks if a transaction has been confirmed.
        /// </summary>
        /// <param name="transactionId">The transaction ID to check.</param>
        /// <param name="requiredConfirmations">The number of confirmations required.</param>
        /// <returns>True if the transaction has the required confirmations.</returns>
        Task<bool> IsTransactionConfirmedAsync(UInt256 transactionId, int requiredConfirmations = 1);
    }
}

