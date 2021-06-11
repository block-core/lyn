using System.Collections.Generic;

namespace Lyn.Types.Bolt.Messages
{
   public class TlVStream
   {
      public List<TlvRecord> Records { get; set; } = new List<TlvRecord>();
   }
}