using System;

namespace Lyn.Protocol.Bolt8
{
   public interface IHkdf
   {
      void ExtractAndExpand(
         ReadOnlySpan<byte> chainingKey,
         ReadOnlySpan<byte> inputKeyMaterial,
         Span<byte> output);
   }
}