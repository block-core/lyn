using System;
using System.Threading.Tasks;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt1
{
    public interface IPingPongMessageRepository
    {
        ValueTask AddPingMessageAsync(DateTime dateTimeGenerated, PingMessage pingMessage);

        ValueTask<bool> PendingPingExistsForIdAsync(ushort pongId);
        
        ValueTask<bool> MarkPongReplyForPingAsync(ushort pongId);
    }
}