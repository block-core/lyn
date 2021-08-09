using System.Collections.Generic;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;

namespace Lyn.Types
{
   public enum SupportedChains
   {
      Bitcoin = 0,
      BitcoinSignet = 1,
      BitcoinRegTest = 2
   }

   public static class ChainHashes
   {
      public static readonly Dictionary<SupportedChains, UInt256> SupportedChainHashes = new()
      {
         {SupportedChains.Bitcoin, Bitcoin},
         {SupportedChains.BitcoinSignet, BitcoinSignet},
         {SupportedChains.BitcoinRegTest, BitcoinRegTest}
      };

      public static UInt256 Bitcoin => new (Hex.FromString(BITCOIN_HEX_CHAIN_HASH));
      
      public static UInt256 BitcoinSignet => new (Hex.FromString(BITCOIN_SIGNET_HEX_CHAIN_HASH));
      
      public static UInt256 BitcoinRegTest => new (Hex.FromString(BITCOIN_REGTEST_HEX_CHAIN_HASH));
      
      public const string BITCOIN_HEX_CHAIN_HASH = "000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f";

      public const string BITCOIN_SIGNET_HEX_CHAIN_HASH = "00000008819873e925422c1ff0f99f7cc9bbb232af63a077a480a3633bee1ef6";
      
      public const string BITCOIN_REGTEST_HEX_CHAIN_HASH = "06226e46111a0b59caaf126043eb5bbf28c34f3a5e332a1fc7b2b73cf188910f";
   }
}