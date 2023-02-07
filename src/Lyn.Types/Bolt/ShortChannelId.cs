using System;

namespace Lyn.Types.Bolt
{
   public class ShortChannelId
   {
      private byte[] _value; //TODO David move this to IProtocolSerializer (?)

      public int BlockHeight { get; set; }
      public int TransactionIndex { get; set; }
      public ushort OutputIndex { get; set; }

      public const ushort LENGTH = 8;

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
         BitConverter.GetBytes(blockHeight).AsSpan(0,3).CopyTo(_value.AsSpan(0));
         TransactionIndex = transactionIndex;
         BitConverter.GetBytes(TransactionIndex).AsSpan(0,3).CopyTo(_value.AsSpan(3));
         OutputIndex = outputIndex;
         BitConverter.GetBytes(outputIndex).CopyTo(_value.AsSpan(6));
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