using System;

namespace Lyn.Types.Bolt
{
   public class ShortChannelId
   {
      private readonly byte[] _value; //TODO David move this to IProtocolSerializer (?)

      public int BlockHeight { get; }
      public int TransactionIndex { get; } 
      public ushort OutputIndex { get; }

      public const ushort LENGTH = 8;

      public ShortChannelId(byte[] value) : this(value.AsSpan())
      { }

      public ShortChannelId(ReadOnlySpan<byte> value)
      {
         if (value.Length > LENGTH)
            throw new ArgumentOutOfRangeException(nameof(value));

         BlockHeight = BitConverter.ToInt32(value.Slice(0, 3));
         TransactionIndex = BitConverter.ToInt32(value.Slice(3, 3));
         OutputIndex = BitConverter.ToUInt16(value.Slice(6, 2));

         _value = new byte[8];
         
         value.CopyTo(_value);
      }

      public ShortChannelId(int blockHeight,int transactionIndex,ushort outputIndex)
      {
         BlockHeight = blockHeight;
         TransactionIndex = transactionIndex;
         OutputIndex = outputIndex;
      }
      
      public static implicit operator byte[](ShortChannelId hash) => hash._value;

      public static implicit operator ShortChannelId(byte[] bytes) => new(bytes);
      
      public static implicit operator ShortChannelId(ReadOnlySpan<byte> bytes) => new(bytes.ToArray());
   }
}