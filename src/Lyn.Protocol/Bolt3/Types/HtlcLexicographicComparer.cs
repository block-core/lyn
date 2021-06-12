using System.Collections.Generic;

namespace Lyn.Protocol.Bolt3.Types
{
    public class HtlcLexicographicComparer : IComparer<HtlcToOutputMaping>
    {
        private readonly LexicographicByteComparer _lexicographicByteComparer;

        public HtlcLexicographicComparer(LexicographicByteComparer lexicographicByteComparer)
        {
            _lexicographicByteComparer = lexicographicByteComparer;
        }

        public int Compare(HtlcToOutputMaping x, HtlcToOutputMaping y)
        {
            if (x?.TransactionOutput?.PublicKeyScript == null) return 1;
            if (y?.TransactionOutput?.PublicKeyScript == null) return -1;

            if (x.TransactionOutput.Value > y.TransactionOutput.Value)
            {
                return 1;
            }

            if (x.TransactionOutput.Value < y.TransactionOutput.Value)
            {
                return -1;
            }

            if (x.TransactionOutput.PublicKeyScript != y.TransactionOutput.PublicKeyScript)
            {
                return _lexicographicByteComparer.Compare(x.TransactionOutput.PublicKeyScript, y.TransactionOutput.PublicKeyScript);
            }

            return x.CltvExpirey < y.CltvExpirey ? 1 : -1;
        }
    }
}