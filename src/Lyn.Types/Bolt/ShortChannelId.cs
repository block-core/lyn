using System;

namespace Lyn.Types.Bolt
{
   public class ShortChannelId
   {
      private readonly byte[] _value;

      public const ushort LENGTH = 8;

      public ShortChannelId(byte[] value)
      {
         if (value.Length > LENGTH)
            throw new ArgumentOutOfRangeException(nameof(value));

         _value = value;
      }

      public static implicit operator byte[](ShortChannelId hash) => hash._value;

      public static implicit operator ShortChannelId(byte[] bytes) => new(bytes);
      
      public static implicit operator ShortChannelId(ReadOnlySpan<byte> bytes) => new(bytes.ToArray());
   }
}