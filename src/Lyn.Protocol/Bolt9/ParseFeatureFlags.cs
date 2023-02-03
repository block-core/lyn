using System;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using Lyn.Protocol.Bolt1.Messages;

namespace Lyn.Protocol.Bolt9
{
    public class ParseFeatureFlags : IParseFeatureFlags
    {
        public Features ParseFeatures(byte[] raw)
        {
            Span<byte> buffer = stackalloc byte[raw.Length];

            raw.CopyTo(buffer);
            
            var converted = MemoryMarshal.Cast<byte, ulong>(buffer);

            // if (converted.Length != 1)
            //     throw new InvalidCastException();
            
            var features = Features.GossipQueries;
            
            foreach (var data in converted)
            {
                features |= (Features)data;
            }

            var bits = new BitArray(buffer.ToArray());

            for (int i = 0; i < bits.Count; i++)
            {
                if (bits[i])
                {
                    Console.WriteLine($"{i} for feature {(Features)(ulong)i}");
                }
            }

            return features;
        }
        
        public byte[] ParseFeatures(Features features)
        {
            var bytes = BitConverter.GetBytes((ulong) features);

            var numOfBytes = GetTrimmedSizeOfArray(features);
            
            return bytes.Take(numOfBytes).ToArray();
        }
        
        public byte[] ParseNFeatures(Features features, int n)
        {
            var bytes = BitConverter.GetBytes((ulong) features);
            
            var numOfBytes = GetTrimmedSizeOfArray(features);
            
            bytes =  bytes.Take(numOfBytes).ToArray();

            var bitArray = new BitArray(bytes);

            for (var i = n + 1; i < bitArray.Length; i++)
            {
                bitArray[i] = false;
            }
            
            bitArray.CopyTo(bytes,0);

            return bytes;
        }
        
        private static int GetTrimmedSizeOfArray(Features features)
        {
            var numOfBytes = 1;

            var size = (ulong) features;

            while (size > (ulong) Math.Pow(256, numOfBytes) - 1)
                numOfBytes++;
            
            return numOfBytes;
        }
    }
}