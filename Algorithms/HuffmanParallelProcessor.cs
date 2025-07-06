using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compression_Vault.Models;

namespace Compression_Vault.Algorithms
{
    /// <summary>
    /// مسؤول عن المعالجة المتوازية في خوارزمية Huffman
    /// </summary>
    public static class HuffmanParallelProcessor
    {
        /// <summary>
        /// معالجة عنصر بالتوازي
        /// </summary>
        public static async Task<CompressedItemData> CompressItemParallelAsync(ICompressibleItem item, string password, SemaphoreSlim semaphore, CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                if (item is CompressibleFile file)
                {
                    return await CompressFileParallelAsync(file, password, cancellationToken);
                }
                else if (item is CompressibleFolder folder)
                {
                    return await CompressFolderParallelAsync(folder, password, cancellationToken);
                }
                else
                {
                    return new CompressedItemData { ItemName = item.Name, OriginalSize = item.Size };
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// ضغط ملف بالتوازي
        /// </summary>
        public static async Task<CompressedItemData> CompressFileParallelAsync(CompressibleFile file, string password, CancellationToken cancellationToken)
        {
            string filePath = file.FullPath;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return new CompressedItemData { ItemName = file.Name, OriginalSize = file.Size };

            using (var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var buffer = new byte[inputStream.Length];
                await inputStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                // Skip very small files (less than 100 bytes) for compression
                if (buffer.Length < 100)
                {
                    return new CompressedItemData
                    {
                        ItemName = file.Name,
                        OriginalSize = buffer.Length,
                        CompressedSize = buffer.Length,
                        FrequencyTable = null,
                        CompressedData = buffer,
                        ValidBitsInLastByte = 8,
                        IsCompressed = false
                    };
                }
                else
                {
                    // Build frequency table using parallel processing
                    var frequencies = BuildFrequencyTableParallel(buffer);

                    // Build Huffman tree
                    var root = HuffmanTreeBuilder.BuildHuffmanTree(frequencies);

                    // Generate Huffman codes
                    var codes = HuffmanTreeBuilder.GenerateHuffmanCodes(root);

                    // Compress data using parallel processing
                    var compressResult = HuffmanDataCompressor.CompressDataParallel(buffer, codes);

                    // Calculate total compressed size including metadata
                    int frequencyTableSize = frequencies.Count * 5; // 1 byte key + 4 bytes frequency per entry
                    int metadataSize = 8; // for example, 4 bytes for valid bits + 4 bytes for frequency table count

                    int totalCompressedSize = compressResult.CompressedBytes.Length + frequencyTableSize + metadataSize;

                    // Only use compression if it actually saves space
                    if (totalCompressedSize < buffer.Length)
                    {
                        return new CompressedItemData
                        {
                            ItemName = file.Name,
                            OriginalSize = buffer.Length,
                            CompressedSize = totalCompressedSize,
                            FrequencyTable = frequencies,
                            CompressedData = compressResult.CompressedBytes,
                            ValidBitsInLastByte = compressResult.ValidBitsInLastByte,
                            IsCompressed = true
                        };
                    }
                    else
                    {
                        return new CompressedItemData
                        {
                            ItemName = file.Name,
                            OriginalSize = buffer.Length,
                            CompressedSize = buffer.Length,
                            FrequencyTable = null,
                            CompressedData = buffer,
                            ValidBitsInLastByte = 8,
                            IsCompressed = false
                        };
                    }
                }
            }
        }

        /// <summary>
        /// ضغط مجلد بالتوازي
        /// </summary>
        public static async Task<CompressedItemData> CompressFolderParallelAsync(CompressibleFolder folder, string password, CancellationToken cancellationToken)
        {
            string folderPath = folder.FullPath;
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return new CompressedItemData { ItemName = folder.Name, OriginalSize = folder.Size };

            var directoryInfo = new DirectoryInfo(folderPath);
            var files = directoryInfo.GetFiles("*", SearchOption.AllDirectories);

            // Compress all files in the folder in parallel
            var fileCompressionTasks = new List<Task<CompressedItemData>>();
            foreach (var fileInfo in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relativePath = GetRelativePath(directoryInfo.FullName, fileInfo.FullName);
                var compressibleFile = new CompressibleFile(fileInfo);
                var task = CompressFileParallelAsync(compressibleFile, password, cancellationToken);
                fileCompressionTasks.Add(task);
            }

            // Wait for all file compression tasks to complete
            var compressedFiles = await Task.WhenAll(fileCompressionTasks);

            // Update the ItemName to use relative paths for proper folder structure
            for (int i = 0; i < files.Length; i++)
            {
                var relativePath = GetRelativePath(directoryInfo.FullName, files[i].FullName);
                compressedFiles[i].ItemName = relativePath;
            }

            return new CompressedItemData
            {
                ItemName = folder.Name,
                OriginalSize = folder.Size,
                CompressedSize = compressedFiles.Sum(f => f.CompressedSize),
                FrequencyTable = null,
                CompressedData = null,
                ValidBitsInLastByte = 0,
                IsCompressed = false,
                FolderFiles = compressedFiles.ToList()
            };
        }

        /// <summary>
        /// بناء جدول التكرار بالتوازي
        /// </summary>
        public static Dictionary<byte, int> BuildFrequencyTableParallel(byte[] data)
        {
            var frequencies = new ConcurrentDictionary<byte, int>();

            if (data.Length > 1024 * 1024) // 1MB threshold
            {
                int processorCount = Environment.ProcessorCount;
                var chunkSize = data.Length / processorCount;
                var tasks = new List<Task<Dictionary<byte, int>>>();

                for (int i = 0; i < processorCount; i++)
                {
                    int startIndex = i * chunkSize;
                    int endIndex = (i == processorCount - 1) ? data.Length : (i + 1) * chunkSize;
                    var chunk = new byte[endIndex - startIndex];
                    Array.Copy(data, startIndex, chunk, 0, chunk.Length);

                    var task = Task.Run(() => BuildFrequencyTableChunk(chunk));
                    tasks.Add(task);
                }

                var results = Task.WhenAll(tasks).Result;

                foreach (var result in results)
                {
                    foreach (var kvp in result)
                    {
                        frequencies.AddOrUpdate(kvp.Key, kvp.Value, (key, oldValue) => oldValue + kvp.Value);
                    }
                }
            }
            else
            {
                foreach (byte b in data)
                {
                    frequencies.AddOrUpdate(b, 1, (key, oldValue) => oldValue + 1);
                }
            }

            return new Dictionary<byte, int>(frequencies);
        }

        /// <summary>
        /// بناء جدول التكرار لجزء من البيانات
        /// </summary>
        private static Dictionary<byte, int> BuildFrequencyTableChunk(byte[] chunk)
        {
            var frequencies = new Dictionary<byte, int>();
            foreach (byte b in chunk)
            {
                if (frequencies.ContainsKey(b))
                    frequencies[b]++;
                else
                    frequencies[b] = 1;
            }
            return frequencies;
        }

        /// <summary>
        /// الحصول على المسار النسبي
        /// </summary>
        private static string GetRelativePath(string basePath, string fullPath)
        {
            var baseUri = new Uri(basePath + Path.DirectorySeparatorChar);
            var fullUri = new Uri(fullPath);
            var relativeUri = baseUri.MakeRelativeUri(fullUri);
            return Uri.UnescapeDataString(relativeUri.ToString());
        }

        /// <summary>
        /// حساب نسبة الضغط والمساحة المحفوظة
        /// </summary>
        public static (double CompressionRatioPercent, long SpaceSavedBytes) CalculateCompressionStats(long originalSize, long compressedSize)
        {
            if (originalSize == 0)
                return (0, 0);

            long spaceSaved = originalSize - compressedSize;
            double ratio = ((double)compressedSize / originalSize) * 100;

            return (Math.Round(ratio, 2), spaceSaved);
        }
    }
}

