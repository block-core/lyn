using Lyn.Types.Bitcoin;

namespace Lyn.Types.Bolt
{
   public class ChannelId : UInt256
   {
      public const int LENGTH = EXPECTED_SIZE;

      public ChannelId(byte[] bytes) : base(bytes)
      { }
      public bool IsEmpty => part1 == 0 && part2 == 0 && part3 == 0 && part4 == 0;
   }
}