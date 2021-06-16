using System;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt1
{
    public interface IPingPongMessageRepository
    {
        ValueTask AddPingMessageAsync(DateTime dateTimeGenerated, PingMessage pingMessage);

        ValueTask<bool> PendingPingWithIdExistsAsync(ushort pingId);
        
        ValueTask<TrackedPingPong?> GetPingMessageAsync(ushort pingId);

        ValueTask<bool> MarkPongReplyForPingAsync(ushort pingId);
    }
}