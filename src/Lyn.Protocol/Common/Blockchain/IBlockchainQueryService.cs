using System.Threading.Tasks;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;

namespace Lyn.Protocol.Common.Blockchain
{
    /// <summary>
    /// Interface for querying blockchain data.
    /// Implementation is deferred to the wallet project.
    /// </summary>
    public interface IBlockchainQueryService
    {
        /// <summary>
        /// Verifies that a P2WSH output exists at the specified short_channel_id location.
        /// Used to validate channel_announcement messages per BOLT7.
        /// </summary>
        /// <param name="shortChannelId">The short channel ID encoding the funding transaction location.</param>
        /// <param name="expectedP2WSH">The expected P2WSH script hash.</param>
        /// <returns>True if the P2WSH output exists and matches.</returns>
        Task<bool> VerifyP2WSHExistsAsync(ShortChannelId shortChannelId, byte[] expectedP2WSH);

        /// <summary>
        /// Gets the transaction at the specified location.
        /// </summary>
        /// <param name="shortChannelId">The short channel ID encoding the transaction location.</param>
        /// <returns>The transaction if found, null otherwise.</returns>
        Task<Transaction?> GetTransactionByShortChannelIdAsync(ShortChannelId shortChannelId);

        /// <summary>
        /// Gets the current block height.
        /// </summary>
        /// <returns>The current block height.</returns>
        Task<uint> GetCurrentBlockHeightAsync();

        /// <summary>
        /// Checks if a transaction output is unspent.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <param name="outputIndex">The output index.</param>
        /// <returns>True if the output is unspent.</returns>
        Task<bool> IsOutputUnspentAsync(UInt256 transactionId, ushort outputIndex);
    }
}

