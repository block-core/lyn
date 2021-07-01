using System;

namespace Lyn.Types.Bitcoin
{
    public class BlockLocator
    {
        /// <summary>
        /// Block locator objects.
        /// Newest back to genesis block (dense to start, but then sparse)
        /// </summary>
        public UInt256[] BlockLocatorHashes { get; set; }

        public BlockLocator()
        {
            BlockLocatorHashes = Array.Empty<UInt256>();
        }
    }
}