using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Common.Messages;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.ChannelClose
{
    public interface IShutdownAction
    {
        Task<MessageProcessingOutput> GenerateClosingSignedAsync(PublicKey nodeId,UInt256 channelId, CancellationToken token);
    }
}