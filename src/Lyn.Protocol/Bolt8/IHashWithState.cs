using System;

namespace Lyn.Protocol.Bolt8
{
   public interface IHashWithState
   {
      IHashWithState AppendData(ReadOnlySpan<byte> data);

      void GetHashAndReset(Span<byte> hash);

      int HashLen { get; }
      
      int BlockLen { get; }
   }
}