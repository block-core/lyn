using System;
using System.Threading.Tasks;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt1
{
    public interface IPingPongMessageRepository
    {
        ValueTask AddPingMessageAsync(PublicKey nodeId, DateTime dateTimeGenerated, PingMessage pingMessage);

        ValueTask<bool> PendingPingExistsForIdAsync(PublicKey nodeId,ushort pongId);
        
        ValueTask<bool> MarkPongReplyForPingAsync(PublicKey nodeId,ushort pongId);
    }
}