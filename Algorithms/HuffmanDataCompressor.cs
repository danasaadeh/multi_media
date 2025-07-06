using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Compression_Vault.Algorithms
{
    /// <summary>
    /// مسؤول عن ضغط البيانات باستخدام خوارزمية Huffman
    /// </summary>
    public static class HuffmanDataCompressor
    {
        /// <summary>
        /// ضغط البيانات باستخدام معالجة متوازية
        /// </summary>
        public static (byte[] CompressedBytes, byte ValidBitsInLastByte) CompressDataParallel(byte[] data, Dictionary<byte, string> codes)
        {
            // Convert string codes to bit arrays for efficiency
            var codeBits = new Dictionary<byte, bool[]>();
            foreach (var kvp in codes)
            {
                var bits = new bool[kvp.Value.Length];
                for (int i = 0; i < kvp.Value.Length; i++)
                {
                    bits[i] = kvp.Value[i] == '1';
                }
                codeBits[kvp.Key] = bits;
            }

            // Use parallel processing for large files
            if (data.Length > 1024 * 1024) // 1MB threshold
            {
                return CompressDataParallelLarge(data, codeBits);
            }
            else
            {
                return CompressDataSequential(data, codeBits);
            }
        }

        /// <summary>
        /// ضغط البيانات الكبيرة بالتوازي
        /// </summary>
        private static (byte[] CompressedBytes, byte ValidBitsInLastByte) CompressDataParallelLarge(byte[] data, Dictionary<byte, bool[]> codeBits)
        {
            var compressedBits = new ConcurrentBag<bool>();
            
            // Process data in parallel chunks
            var chunkSize = data.Length / Environment.ProcessorCount;
            var tasks = new List<Task<List<bool>>>();

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                var startIndex = i * chunkSize;
                var endIndex = (i == Environment.ProcessorCount - 1) ? data.Length : (i + 1) * chunkSize;
                var chunk = new byte[endIndex - startIndex];
                Array.Copy(data, startIndex, chunk, 0, chunk.Length);

                var task = Task.Run(() => CompressDataChunk(chunk, codeBits));
                tasks.Add(task);
            }

            // Wait for all tasks and combine results
            var results = Task.WhenAll(tasks).Result;
            var allBits = new List<bool>();
            foreach (var result in results)
            {
                allBits.AddRange(result);
            }

            return PackBitsToBytes(allBits);
        }

        /// <summary>
        /// ضغط جزء من البيانات
        /// </summary>
        private static List<bool> CompressDataChunk(byte[] chunk, Dictionary<byte, bool[]> codeBits)
        {
            var compressedBits = new List<bool>();
            foreach (byte b in chunk)
            {
                if (codeBits.ContainsKey(b))
                {
                    compressedBits.AddRange(codeBits[b]);
                }
            }
            return compressedBits;
        }

        /// <summary>
        /// ضغط البيانات بشكل تسلسلي
        /// </summary>
        private static (byte[] CompressedBytes, byte ValidBitsInLastByte) CompressDataSequential(byte[] data, Dictionary<byte, bool[]> codeBits)
        {
            var compressedBits = new List<bool>();
            foreach (byte b in data)
            {
                if (codeBits.ContainsKey(b))
                {
                    compressedBits.AddRange(codeBits[b]);
                }
            }

            return PackBitsToBytes(compressedBits);
        }

        /// <summary>
        /// تحويل البتات إلى بايتات
        /// </summary>
        private static (byte[] CompressedBytes, byte ValidBitsInLastByte) PackBitsToBytes(List<bool> compressedBits)
        {
            var compressedBytes = new List<byte>();
            
            // Handle empty data case
            if (compressedBits.Count == 0)
            {
                return (new byte[0], 0);
            }
            
            int i = 0;
            for (; i + 8 <= compressedBits.Count; i += 8)
            {
                byte b = 0;
                for (int j = 0; j < 8; j++)
                {
                    if (compressedBits[i + j])
                        b |= (byte)(1 << (7 - j));
                }
                compressedBytes.Add(b);
            }

            // Handle the last byte (if not a multiple of 8 bits)
            byte lastByte = 0;
            byte validBitsInLastByte = 8;
            int remaining = compressedBits.Count - i;
            if (remaining > 0)
            {
                for (int j = 0; j < remaining; j++)
                {
                    if (compressedBits[i + j])
                        lastByte |= (byte)(1 << (7 - j));
                }
                compressedBytes.Add(lastByte);
                validBitsInLastByte = (byte)remaining;
            }
            else if (compressedBytes.Count > 0)
            {
                validBitsInLastByte = 8;
            }
            else
            {
                validBitsInLastByte = 0;
            }

            return (compressedBytes.ToArray(), validBitsInLastByte);
        }
    }
} 