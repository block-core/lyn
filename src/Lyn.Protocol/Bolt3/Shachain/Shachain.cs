using System;
using Lyn.Protocol.Common.Hashing;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt3.Shachain
{
    /// <summary>
    /// This was ported from
    /// https://github.com/ElementsProject/lightning/tree/master/ccan/ccan/crypto/shachain
    /// </summary>
    public class Shachain
    {
        public UInt256 derive(ulong from, ulong to, UInt256 from_hash)
        {
            int branches;
            int i;

            if (!can_derive(from, to)) throw new Exception();

            /* We start with the first hash. */
            //UInt256 hash = from_hash;

            byte[] hash = from_hash.GetBytes().ToArray();

            /* This represents the bits set in to, and not from. */
            branches = (int)(from ^ to);
            for (i = branches - 1; i >= 0; i--)
            {
                if (((branches >> i) & 1) == 0)
                {
                    change_bit(hash, i);

                    // todo this could potentially be optimized to use only a single span for the entire calculations
                    var span = hash.AsSpan();
                    ReadOnlySpan<byte> res = HashGenerator.Sha256(span);
                    hash = res.ToArray();
                }
            }

            return new UInt256(hash);
        }

        private void change_bit(byte[] arr, int index)
        {
            byte a = arr[index / CHAR_BIT];
            byte b = (byte)(index % CHAR_BIT);

            a ^= (byte)(1 << b);
        }

        private bool can_derive(ulong from, ulong to)
        {
            ulong mask;

            /* Corner case: can always derive from seed. */
            if (from == 0)
                return true;

            /* Leading bits must be the same */
            mask = ~(((ulong)1 << count_trailing_zeroes(from)) - 1);
            return ((from ^ to) & mask) == 0;
        }

        private const byte CHAR_BIT = 8;
        private const int SHACHAIN_BITS = 8;//(sizeof(ulong) * 8)

        private int count_trailing_zeroes(ulong index)
        {
            var ret = System.Numerics.BitOperations.TrailingZeroCount(index);
            //uint i;

            //for (i = 0; i < SHACHAIN_BITS; i++)
            //{
            //    if (index & (1ULL << i))
            //break;
            //}

            return ret;
        }
    }
}