using Lyn.Protocol.Bolt1.Messages;

namespace Lyn.Protocol.Bolt7
{
    public interface IMessageValidator<in T> where T : MessagePayload
    {
        bool ValidateMessage(T networkMessage);
    }
}