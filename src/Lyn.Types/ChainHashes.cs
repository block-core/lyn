using System.Collections.Generic;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;

namespace Lyn.Types
{
   public enum SupportedChains
   {
      Bitcoin = 0,
      BitcoinSignet = 1
   }

   public static class ChainHashes
   {
      public static readonly Dictionary<SupportedChains, ChainHash> SupportedChainHashes = new()
      {
         {SupportedChains.Bitcoin, Bitcoin},
         {SupportedChains.BitcoinSignet, BitcoinSignet}
      };

      public static ChainHash Bitcoin => new (Hex.FromString(BITCOIN_HEX_CHAIN_HASH));
      
      public static ChainHash BitcoinSignet => new (Hex.FromString(BITCOIN_SIGNET_HEX_CHAIN_HASH));
      
      public const string BITCOIN_HEX_CHAIN_HASH = "000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f";

      public const string BITCOIN_SIGNET_HEX_CHAIN_HASH = "00000008819873e925422c1ff0f99f7cc9bbb232af63a077a480a3633bee1ef6";
   }
}