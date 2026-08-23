using System;
using System.Linq;

namespace Lyn.Types.Bolt
{
   public class ShortChannelId : IEquatable<ShortChannelId>
   {
      private byte[] _value; //TODO David move this to IProtocolSerializer (?)

      public int BlockHeight { get; set; }
      public int TransactionIndex { get; set; }
      public ushort OutputIndex { get; set; }

      public const ushort LENGTH = 8;

      public ShortChannelId()
      {
         _value = new byte[8];
      }

      public ShortChannelId(byte[] value)
      {
         _value = new byte[8];
         
         ParseBytes(value);
      }

      public ShortChannelId(ReadOnlySpan<byte> value)
      {
         _value = new byte[8];
         
         ParseBytes(value);
      }

      private void ParseBytes(ReadOnlySpan<byte> value)
      {
         if (value.Length > LENGTH)
            throw new ArgumentOutOfRangeException(nameof(value));

         BlockHeight = GetInt(value.Slice(0, 3));
         TransactionIndex = GetInt(value.Slice(3, 3));
         OutputIndex = BitConverter.ToUInt16(value.Slice(6, 2));

         value.CopyTo(_value);
      }

      public ShortChannelId(int blockHeight,int transactionIndex,ushort outputIndex)
      {
         _value = new byte[8];
         BlockHeight = blockHeight;
         TransactionIndex = transactionIndex;
         OutputIndex = outputIndex;
      }

      /// <summary>
      /// Converts the short channel id to its canonical 64 bit representation as
      /// used on the wire (block_height &lt;&lt; 40 | transaction_index &lt;&lt; 16 | output_index).
      /// </summary>
      public ulong ToUInt64()
      {
         return ((ulong)(uint)BlockHeight << 40) | ((ulong)(uint)TransactionIndex << 16) | OutputIndex;
      }

      /// <summary>
      /// Creates a short channel id from its canonical 64 bit representation.
      /// </summary>
      public static ShortChannelId FromUInt64(ulong value)
      {
         return new ShortChannelId((int)((value >> 40) & 0xFF_FF_FF),
            (int)((value >> 16) & 0xFF_FF_FF),
            (ushort)(value & 0xFFFF));
      }

      public bool Equals(ShortChannelId? other)
      {
         if (ReferenceEquals(null, other)) return false;
         if (ReferenceEquals(this, other)) return true;

         return BlockHeight == other.BlockHeight
                && TransactionIndex == other.TransactionIndex
                && OutputIndex == other.OutputIndex;
      }

      public override bool Equals(object? obj) => ReferenceEquals(this, obj) || Equals(obj as ShortChannelId);

      public override int GetHashCode()
      {
         // Combine the three components into a stable hash code.
         return HashCode.Combine(BlockHeight, TransactionIndex, OutputIndex);
      }


      
      public static implicit operator byte[](ShortChannelId hash) => hash._value;

      public static implicit operator ShortChannelId(byte[] bytes) => new(bytes);
      
      public static implicit operator ShortChannelId(ReadOnlySpan<byte> bytes) => new(bytes.ToArray());

      private static int GetInt(ReadOnlySpan<byte> source)
      {
         var target = new byte[4];
         source.CopyTo(target);
         return BitConverter.ToInt32(target);
      }
   }
}