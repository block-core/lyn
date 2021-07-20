using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Lyn.Protocol.Common
{
    public class DefaultRandomNumberGenerator : IRandomNumberGenerator //TODO David this was imported from Mithril shards and needs unit tests
    {
        //private static readonly RandomNumberGenerator Generator = RandomNumberGenerator.Create();
        private static readonly RNGCryptoServiceProvider Generator = new ();
        
        public void GetBytes(Span<byte> data)
        {
            Generator.GetBytes(data);
        }

        public byte[] GetBytes(int len)
        {
            Span<byte> result = stackalloc byte[len];
            Generator.GetBytes(MemoryMarshal.AsBytes(result));
            return result.ToArray();
        }

        public void GetNonZeroBytes(Span<byte> data)
        {
            Generator.GetNonZeroBytes(data);
        }

        public ushort GetUint16()
        {
            Span<ushort> resultSpan = stackalloc ushort[1];
            Generator.GetBytes(MemoryMarshal.AsBytes(resultSpan));
            return resultSpan[0];
        }

        public int GetInt32()
        {
            Span<int> resultSpan = stackalloc int[1];
            Generator.GetBytes(MemoryMarshal.AsBytes(resultSpan));
            return resultSpan[0];
        }

        public uint GetUint32()
        {
            Span<uint> resultSpan = stackalloc uint[1];
            Generator.GetBytes(MemoryMarshal.AsBytes(resultSpan));
            return resultSpan[0];
        }

        public long GetInt64()
        {
            Span<long> resultSpan = stackalloc long[1];
            Generator.GetBytes(MemoryMarshal.AsBytes(resultSpan));
            return resultSpan[0];
        }

        public ulong GetUint64()
        {
            Span<ulong> resultSpan = stackalloc ulong[1];
            Generator.GetBytes(MemoryMarshal.AsBytes(resultSpan));
            return resultSpan[0];
        }
    }
}