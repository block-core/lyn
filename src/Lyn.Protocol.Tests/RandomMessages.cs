using System;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Tests
{
    public static class RandomMessages
    {
        private static Random _random = new Random();

        public static ChannelId NewRandomChannelId()
        {
            return new(GetRandomByteArray(ChannelId.LENGTH));
        }

        public static ShortChannelId NewRandomShortChannelId()
        {
            return new (GetRandomByteArray(ShortChannelId.LENGTH));
        }

        public static CompressedSignature NewRandomCompressedSignature()
        {
            return new (GetRandomByteArray(CompressedSignature.LENGTH));
        }

        public static PublicKey NewRandomPublicKey()
        {
            return new (new NBitcoin.Key().PubKey.ToBytes());
        }

        public static ChainHash NewRandomChainHash()
        {
            return new (GetRandomByteArray(32));
        }

        public static ChainHash NewRandomUint256()
        {
            return new (GetRandomByteArray(32));
        }

        public static byte[] GetRandomByteArray(int length)
        {
            var bytes = new byte[length];

            _random.NextBytes(bytes);

            return bytes;
        }

        public static uint GetRandomNumberUInt32(int? maxValue = null)
        {
            return (uint)_random.Next(0, maxValue ?? int.MaxValue);
        }

        public static ushort GetRandomNumberUInt16()
        {
            return (ushort)_random.Next(0, short.MaxValue);
        }
    }
}