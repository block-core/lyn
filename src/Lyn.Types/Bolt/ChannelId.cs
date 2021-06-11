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
      public static explicit operator ChannelId(byte[] bytes) => new ChannelId(bytes);   }
}