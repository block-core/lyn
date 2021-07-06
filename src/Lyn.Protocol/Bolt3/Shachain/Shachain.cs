using System;
using System.Linq;
using Lyn.Protocol.Common.Hashing;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt3.Shachain
{
    /// <summary>
    /// This was ported from
    /// https://github.com/ElementsProject/lightning/tree/master/ccan/ccan/crypto/shachain
    /// </summary>
    public class Shachain
    {
        public const int MAX_HEIGHT = 48;

        public UInt256 DeriveSecret(UInt256 secret, int bits, ulong index)
        {
            if (bits > MAX_HEIGHT)
                throw new InvalidOperationException($"The bits = {bits} is above limit {MAX_HEIGHT}");

            var buffer = secret.GetBytes().ToArray().AsSpan();

            for (int position = bits - 1; position >= 0; position--)
            {
                // find bit on index at position.
                var byteAtPosition = GetByeAtPosition(index, position);

                if (byteAtPosition == 1)
                {
                    FlipByte(buffer, position);
                    HashBuffer(ref buffer);
                }
            }

            return new UInt256(buffer);
        }

        public bool InsertSecret(ShachainItems chain, UInt256 secret, ulong index)
        {
            if (chain.Secrets.Last().Value.Index - 1 != index)
                throw new InvalidOperationException("Invalid order");

            int position = CountTrailingZeroes(index);

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

            return true;
        }

        public UInt256? DeriveOldSecret(ShachainItems chain, ulong index)
        {
            foreach (var item in chain.Secrets)
            {
                var mask = ~((1 << item.Key) - 1);

                if ((index & (ulong)mask) == item.Value.Index)
                {
                    return DeriveSecret(item.Value.Secret, item.Key, item.Value.Index);
                }
            }

            return null;
        }

        private int CountTrailingZeroes(ulong index)
        {
            var retTest = System.Numerics.BitOperations.TrailingZeroCount(index);

            for (int position = 0; position < MAX_HEIGHT; position++)
            {
                if (GetByeAtPosition(index, position) != 0)
                {
                    if (retTest != position) throw new Exception(); // todo: delete this test code

                    return position;
                }
            }

            return 0;
        }

        private void FlipByte(Span<byte> buffer, int position)
        {
            var byteNumber = position / 8;
            var bitNumber = position % 8;

            int byteContent = buffer[byteNumber];

            byteContent ^= (1 << bitNumber);

            buffer[byteNumber] = (byte)byteContent;
        }

        private void HashBuffer(ref Span<byte> buffer)
        {
            // todo: optimize by using a Span<byte> instead of a ReadOnlySpan<byte> on the HashGenerator
            var hashed = HashGenerator.Sha256(buffer);
            buffer = hashed.ToArray();
        }

        private byte GetByeAtPosition(ulong index, int position)
        {
            return (byte)((index >> position) & 1);
        }
    }
}