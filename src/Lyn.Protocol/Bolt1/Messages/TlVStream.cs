using System.Collections.Generic;

namespace Lyn.Protocol.Bolt1.Messages
{
   public class TlVStream
   {
      public List<TlvRecord> Records { get; set; } = new List<TlvRecord>();
   }
}