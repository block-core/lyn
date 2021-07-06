using System.Collections.Generic;

namespace Lyn.Protocol.Bolt3.Shachain
{
    public class ShachainItems
    {
        public Dictionary<int, ShachainItem> Secrets { get; set; } = new(Shachain.MAX_HEIGHT);
    }
}