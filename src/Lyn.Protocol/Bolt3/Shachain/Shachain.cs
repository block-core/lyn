using System;
using System.Linq;
using Lyn.Protocol.Common.Hashing;
using Lyn.Types;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Bolt3.Shachain
{
    /// <summary>
    /// This was ported from
    /// https://github.com/ElementsProject/lightning/tree/master/ccan/ccan/crypto/shachain
    /// </summary>
    public class Shachain
    {
        public const int MAX_HEIGHT = 48;
        public const ulong INDEX_ROOT = 281474976710655;

        public UInt256 GenerateFromSeed(UInt256 secret, ulong index)
        {
            return DeriveSecret(secret, MAX_HEIGHT, index);
        }

        public UInt256 DeriveSecret(UInt256 secret, byte bits, ulong from_index)
        {
            if (bits > MAX_HEIGHT)
                throw new InvalidOperationException($"The bits = {bits} is above limit {MAX_HEIGHT}");

            var buffer = secret.GetBytes().ToArray().AsSpan();

            for (byte position = (byte) (bits - 1); position < byte.MaxValue; position--)
            {
                // find bit on index at position.
                var bitAtPosition = GetBitAtPositionAsBoolean(from_index, position);

                if (bitAtPosition)
                {
                    FlipBit(buffer, position);
                    HashBuffer(ref buffer);
                }
            }

            return new UInt256(buffer);
        }

        public bool InsertSecret(ShachainItems chain, UInt256 secret, ulong index)
        {
            if (chain.Index - 1 != index) throw new InvalidOperationException("Invalid order");

            byte position = CountTrailingZeroes(index);

            for (int i = 0; i < position; i++)
            {
                ShachainItem shachainItem = chain.Secrets[i];
                UInt256 newSecret = DeriveSecret(secret, position, shachainItem.Index);

                if (newSecret != shachainItem.Secret)
                {
                    return false;
                }
            }

            if (chain.Secrets.TryGetValue(position, out ShachainItem? item))
            {
                item.Secret = secret;
                item.Index = index;
            }
            else
            {
                chain.Secrets.Add(position, new ShachainItem(secret, index)); 
            }

            chain.Index = index;

            return true;
        }

        public UInt256? DeriveOldSecret(ShachainItems chain, ulong index)
        {
            foreach (var item in chain.Secrets.OrderBy(o => o.Key))
            {
                var mask = ~((1 << item.Key) - 1);

                if ((index & (ulong)mask) == item.Value.Index)
                {
                    return DeriveSecret(item.Value.Secret, CountTrailingZeroes(item.Value.Index), index);
                }
            }

            return null;
        }

        private byte CountTrailingZeroes(ulong index)
        {
            // return System.Numerics.BitOperations.TrailingZeroCount(index); // is this more optimized?

            for (byte position = 0; position < MAX_HEIGHT; position++)
            {
                if (GetBitAtPositionAsBoolean(index, position))
                {
                    return position;
                }
            }

            return 0;
        }

        private static void FlipBit(Span<byte> buffer, byte position)
        {
            var byteNumber = position / 8;
            var bitNumber = position % 8;

            var byteContent = buffer[byteNumber];

            byteContent ^= (byte) (1 << bitNumber);

            buffer[byteNumber] = byteContent;
        }

        private static void HashBuffer(ref Span<byte> buffer)
        {
            // todo: optimize by using a Span<byte> instead of a ReadOnlySpan<byte> on the HashGenerator
            HashGenerator.Sha256(buffer)
                .CopyTo(buffer);
        }

        private static bool GetBitAtPositionAsBoolean(ulong index, byte position)
        {
            return ((index >> position) & 1) > 0;
        }
    }
}