using System.Collections.Generic;

namespace Lyn.Protocol.Bolt3.Shachain
{
    public class ShachainItems
    {
        public ulong Index { get; set; } = Shachain.INDEX_ROOT + 1;

        public Dictionary<int, ShachainItem> Secrets { get; set; } = new(Shachain.MAX_HEIGHT);
    }
}