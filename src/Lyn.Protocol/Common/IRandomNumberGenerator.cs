using System;

namespace Lyn.Protocol.Common
{
    public interface IRandomNumberGenerator
    {
        void GetBytes(Span<byte> data);

        byte[] GetBytes(int len);

        void GetNonZeroBytes(Span<byte> data);

        ushort GetUint16();

        int GetInt32();

        uint GetUint32();

        long GetInt64();

        ulong GetUint64();
    }
}