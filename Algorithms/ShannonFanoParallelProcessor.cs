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
    /// مسؤول عن المعالجة المتوازية في خوارزمية Shannon-Fano
    /// </summary>
    public static class ShannonFanoParallelProcessor
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

                // Only compress if the file is large enough to benefit from compression
                if (buffer.Length < 100) // Skip very small files
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

                    // Build Shannon-Fano tree
                    var root = ShannonFanoTreeBuilder.BuildShannonFanoTree(frequencies);

                    // Generate Shannon-Fano codes
                    var codes = ShannonFanoTreeBuilder.GenerateShannonFanoCodes(root);

                    // Compress data using parallel processing
                    var compressResult = ShannonFanoDataCompressor.CompressDataParallel(buffer, codes);

                    // Calculate total compressed size including metadata
                    int totalCompressedSize = compressResult.CompressedBytes.Length + 
                                            (frequencies.Count * 5) + // frequency table (1 byte + 4 bytes for count)
                                            4 + // valid bits count
                                            4; // frequency table count

                    // Only use compression if it actually saves space
                    if (totalCompressedSize < buffer.Length)
                    {
                        return new CompressedItemData
                        {
                            ItemName = file.Name,
                            OriginalSize = buffer.Length,
                            CompressedSize = compressResult.CompressedBytes.Length,
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
            
            // Use parallel processing for large files
            if (data.Length > 1024 * 1024) // 1MB threshold
            {
                var chunkSize = data.Length / Environment.ProcessorCount;
                var tasks = new List<Task<Dictionary<byte, int>>>();

                for (int i = 0; i < Environment.ProcessorCount; i++)
                {
                    var startIndex = i * chunkSize;
                    var endIndex = (i == Environment.ProcessorCount - 1) ? data.Length : (i + 1) * chunkSize;
                    var chunk = new byte[endIndex - startIndex];
                    Array.Copy(data, startIndex, chunk, 0, chunk.Length);

                    var task = Task.Run(() => BuildFrequencyTableChunk(chunk));
                    tasks.Add(task);
                }

                // Wait for all tasks and merge results
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
                // For smaller files, use regular processing
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
    }
} 