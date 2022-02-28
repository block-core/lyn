using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Lyn.Types.Bitcoin;

namespace Lyn.Protocol.Common.Crypto
{
    public static partial class HashGenerator
    {
        public static ReadOnlySpan<byte> Sha256(ReadOnlySpan<byte> data)
        {
            using var sha = new SHA256Managed();
            Span<byte> result = new byte[32];

            if (!sha.TryComputeHash(data, result, out _)) ThrowHashGeneratorException($"Failed to perform {nameof(Sha256)}");

            return result;
        }

        public static ReadOnlySpan<byte> DoubleSha256(ReadOnlySpan<byte> data)
        {
            using var sha = new SHA256Managed();
            Span<byte> result = new byte[32];

            if (!sha.TryComputeHash(data, result, out _) || !sha.TryComputeHash(result, result, out _))
            {
                ThrowHashGeneratorException($"Failed to perform {nameof(DoubleSha256)}");
            }

            return result;
        }

        public static ReadOnlySpan<byte> DoubleSha512(ReadOnlySpan<byte> data)
        {
            using var sha = new SHA512Managed();
            Span<byte> result = new byte[64];
            sha.TryComputeHash(data, result, out _);
            sha.TryComputeHash(result, result, out _);
            return result.Slice(0, 32);
        }

        public static UInt256 DoubleSha256AsUInt256(ReadOnlySpan<byte> data)
        {
            using var sha = new SHA256Managed();
            Span<byte> result = stackalloc byte[32];
            if (!sha.TryComputeHash(data, result, out _)) throw new HashGeneratorException($"Failed to perform {nameof(DoubleSha256AsUInt256)}");
            if (!sha.TryComputeHash(result, result, out _)) throw new HashGeneratorException($"Failed to perform {nameof(DoubleSha256AsUInt256)}");
            return new UInt256(result);
        }

        public static UInt256 DoubleSha512AsUInt256(ReadOnlySpan<byte> data)
        {
            using var sha = new SHA512Managed();
            Span<byte> result = stackalloc byte[64];
            if (!sha.TryComputeHash(data, result, out _)) throw new HashGeneratorException($"Failed to perform {nameof(DoubleSha512AsUInt256)}");
            if (!sha.TryComputeHash(result, result, out _)) throw new HashGeneratorException($"Failed to perform {nameof(DoubleSha512AsUInt256)}");
            return new UInt256(result.Slice(0, 32));
        }

        public static ReadOnlySpan<byte> HmacSha256(byte[] key, ReadOnlySpan<byte> data)
        {
            using var hmac = new HMACSHA256(key);
            Span<byte> result = new byte[32];
            if (!hmac.TryComputeHash(data, result, out _)) throw new HashGeneratorException($"Failed to perform {nameof(HmacSha256)}");
            return result;
        }

        [DoesNotReturn]
        public static void ThrowHashGeneratorException(string message)
        {
            throw new HashGeneratorException(message);
        }
    }
}