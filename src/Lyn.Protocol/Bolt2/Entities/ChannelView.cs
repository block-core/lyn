using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.Entities
{
    /// <summary>
    /// How much is owed to each side (includes pending changes)
    /// </summary>
    public class ChannelView
    {
        private MiliSatoshis Amountmsat { get; set; }
    };
}