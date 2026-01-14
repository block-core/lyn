using System.Threading.Tasks;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Connection
{
    /// <summary>
    /// Manages peer connections and provides methods to close connections and fail channels.
    /// </summary>
    public interface IConnectionManager
    {
        /// <summary>
        /// Closes the connection to a peer gracefully.
        /// </summary>
        /// <param name="nodeId">The node ID of the peer to disconnect.</param>
        /// <param name="reason">The reason for disconnection.</param>
        Task CloseConnectionAsync(PublicKey nodeId, string? reason = null);

        /// <summary>
        /// Fails a specific channel with a peer.
        /// </summary>
        /// <param name="nodeId">The node ID of the peer.</param>
        /// <param name="channelId">The channel ID to fail.</param>
        /// <param name="reason">The reason for failing the channel.</param>
        Task FailChannelAsync(PublicKey nodeId, UInt256 channelId, string reason);

        /// <summary>
        /// Fails all channels with a peer.
        /// </summary>
        /// <param name="nodeId">The node ID of the peer.</param>
        /// <param name="reason">The reason for failing all channels.</param>
        Task FailAllChannelsAsync(PublicKey nodeId, string reason);
    }
}

