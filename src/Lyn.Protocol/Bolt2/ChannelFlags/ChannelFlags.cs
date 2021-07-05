using System;

namespace Lyn.Protocol.Bolt2.ChannelFlags
{
    [Flags]
    public enum ChannelFlags : byte
    {
        AnnounceChannel = 1 << 0,
    }
}