using System;

namespace Lyn.Types.Bolt
{
   public class ChannelId
   {
      readonly byte[] _value;
      
      public const ushort LENGTH = 32;
      
      public ChannelId(byte[] value)
      {
         if (value.Length > LENGTH)
            throw new ArgumentOutOfRangeException(nameof(value));
            
         _value = value;
      }

      public static implicit operator byte[](ChannelId hash) => hash._value;
      public static implicit operator Span<byte>(ChannelId hash) => hash._value;
      public static implicit operator ChannelId(byte[] bytes) => new (bytes);
      public static implicit operator ChannelId(ReadOnlySpan<byte> bytes) => new (bytes.ToArray());

      public bool IsEmpty => _value.Length == 0;// TODO David check if should check all array is 0
   }
}