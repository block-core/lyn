using System;

namespace Lyn.Protocol.Common
{
    public interface IDateTimeProvider
    {
        DateTime GetUtcNow();
    }
}