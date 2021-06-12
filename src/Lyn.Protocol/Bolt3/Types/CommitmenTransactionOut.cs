using System.Collections.Generic;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt3.Types
{
    public class CommitmenTransactionOut
    {
        public List<HtlcToOutputMaping>? Htlcs { get; set; }
        public Transaction? Transaction { get; set; }
    }
}