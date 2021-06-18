using Lyn.Types.Bolt.Messages;

namespace Lyn.Protocol.Bolt7
{
    public interface IMessageValidator<in T> where T : BoltMessage
    {
        (bool, ErrorMessage?) ValidateMessage(T networkMessage);
    }
}