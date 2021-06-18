namespace Lyn.Types.Bolt.Messages
{
   public abstract class BoltMessage
   {
      public abstract string Command { get; }

      public TlVStream? Extension { get; set; }
   }
}