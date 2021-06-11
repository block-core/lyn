using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt8
{
   public class Sha256 : INoiseHashFunction
   {
      private readonly ILogger<Sha256> _logger;

      public Sha256(ILogger<Sha256> logger)
      {
         _logger = logger;
      }

      public void Hash(ReadOnlySpan<byte> span, Span<byte> output)
      {
         using var sha256 = SHA256.Create();
         sha256.ComputeHash(span.ToArray());
         sha256.Hash.AsSpan()
            .CopyTo(output);
         _logger.LogDebug($"hashed 1 parameter into output");
      }

      public void Hash(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second, Span<byte> output)
      {
         var array = new byte[first.Length + second.Length];
         
         first.CopyTo(array.AsSpan(0,first.Length));
         second.CopyTo(array.AsSpan(first.Length,second.Length));

         using var sha256 = SHA256.Create();
         sha256.ComputeHash(array);
         sha256.Hash.AsSpan()
            .CopyTo(output);
         
         _logger.LogDebug($"hashed 2 parameters into output");
      }
   }
}