using System.Collections.Concurrent;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.NormalOperations;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Connection
{
    /// <summary>
    /// Default implementation of connection manager for managing peer connections and channel failures.
    /// </summary>
    public class ConnectionManager : IConnectionManager
    {
        private readonly ILogger<ConnectionManager> _logger;
        private readonly IPaymentChannelRepository _paymentChannelRepository;
        private readonly ConcurrentDictionary<PublicKey, bool> _disconnectedPeers = new();

        public ConnectionManager(
            ILogger<ConnectionManager> logger,
            IPaymentChannelRepository paymentChannelRepository)
        {
            _logger = logger;
            _paymentChannelRepository = paymentChannelRepository;
        }

        public Task CloseConnectionAsync(PublicKey nodeId, string? reason = null)
        {
            _logger.LogInformation("Closing connection to peer {NodeId}. Reason: {Reason}", 
                nodeId, reason ?? "No reason provided");
            
            _disconnectedPeers.TryAdd(nodeId, true);
            
            // The actual socket close is handled by the transport layer
            // This marks the peer as disconnected for protocol-level handling
            return Task.CompletedTask;
        }

        public async Task FailChannelAsync(PublicKey nodeId, UInt256 channelId, string reason)
        {
            _logger.LogWarning("Failing channel {ChannelId} with peer {NodeId}. Reason: {Reason}",
                channelId, nodeId, reason);

            var channel = await _paymentChannelRepository.TryGetPaymentChannelAsync(channelId);
            
            if (channel != null)
            {
                channel.ChannelShutdownTriggered = true;
                await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);
            }
        }

        public async Task FailAllChannelsAsync(PublicKey nodeId, string reason)
        {
            _logger.LogWarning("Failing all channels with peer {NodeId}. Reason: {Reason}",
                nodeId, reason);

            var channels = await _paymentChannelRepository.GetAllChannelsForPeerAsync(nodeId);
            
            foreach (var channel in channels)
            {
                channel.ChannelShutdownTriggered = true;
                await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);
            }
            
            await CloseConnectionAsync(nodeId, reason);
        }

        public bool IsPeerDisconnected(PublicKey nodeId)
        {
            return _disconnectedPeers.ContainsKey(nodeId);
        }
    }
}

