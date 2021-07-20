using System;

namespace Lyn.Types.Bolt
{
   public class ChannelId : IEquatable<ChannelId>
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

      public bool Equals(ChannelId? other)
      {
         if (ReferenceEquals(null, other)) return false;
         if (ReferenceEquals(this, other)) return true;
         return _value.Equals(other._value);
      }

      public override bool Equals(object? obj)
      {
         if (ReferenceEquals(null, obj)) return false;
         if (ReferenceEquals(this, obj)) return true;
         if (obj.GetType() != this.GetType()) return false;
         return Equals((ChannelId) obj);
      }

      public override int GetHashCode()
      {
         return _value.GetHashCode();
      }
   }
}