using System;

namespace Lyn.Protocol.Bolt8
{
   public interface INoiseHashFunction
   {
      void Hash(ReadOnlySpan<byte> span,Span<byte> output);
      void Hash(ReadOnlySpan<byte> first,ReadOnlySpan<byte> second,Span<byte> output);
   }
}