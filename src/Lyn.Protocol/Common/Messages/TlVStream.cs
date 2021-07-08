using System.Collections.Generic;

namespace Lyn.Protocol.Common.Messages
{
   public class TlVStream
   {
      public List<TlvRecord> Records { get; set; } = new List<TlvRecord>();
   }
}