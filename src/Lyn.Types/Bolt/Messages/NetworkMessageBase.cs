namespace Lyn.Types.Bolt.Messages
{
   public abstract class NetworkMessageBase
   {
      public abstract string Command { get; }

      public TlVStream? Extension { get; set; }
   }
}