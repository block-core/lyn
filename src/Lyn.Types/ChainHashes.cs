using System.Collections.Generic;
using Lyn.Types.Bolt;

namespace Lyn.Types
{
   public enum SupportedChains
   {
      Bitcoin = 0
   }

   public static class ChainHashes
   {
      public static readonly Dictionary<SupportedChains, ChainHash> SupportedChainHashes = new()
      {
         {SupportedChains.Bitcoin, Bitcoin}
      };

      public static ChainHash Bitcoin => (ChainHash)BITCOIN_HEX_CHAIN_HASH.ToByteArray();  
      
      public const string BITCOIN_HEX_CHAIN_HASH = "6fe28c0ab6f1b372c1a6a246ae63f74f931e8365e15a089c68d6190000000000";
   }
}