using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Compression_Vault.Models;
using Compression_Vault.Services;

namespace Compression_Vault.Algorithms
{
    /// <summary>
    /// خوارزمية ضغط Shannon-Fano مع دعم المعالجة المتوازية
    /// </summary>
    public class ShannonFanoCompression : ICompressionAlgorithm
    {
        public string Name => "Shannon-Fano";

        public async Task<CompressionResult> CompressAsync(IEnumerable<ICompressibleItem> items, string outputPath, string password = null, IProgress<CompressionProgress> progress = null, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            var result = new CompressionResult { Success = false };

            try
            {
                var itemList = items.ToList();
                if (!itemList.Any())
                {
                    result.ErrorMessage = "No items to compress.";
                    return result;
                }

                // Calculate total size for progress tracking
                long totalSize = itemList.Sum(item => item.Size);
                long processedBytes = 0;

                using (var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                using (var writer = new BinaryWriter(outputStream))
                {
                    // Write header
                    await WriteHeaderAsync(writer, itemList, password, cancellationToken);

                    // Process items in parallel for better performance
                    var compressionTasks = new List<Task<CompressedItemData>>();
                    var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);

                    // Start parallel compression tasks
                    for (int i = 0; i < itemList.Count; i++)
                    {
                        var item = itemList[i];
                        var task = ShannonFanoParallelProcessor.CompressItemParallelAsync(item, password, semaphore, cancellationToken);
                        compressionTasks.Add(task);
                    }

                    // Wait for all compression tasks to complete and write results
                    var compressedItems = new List<CompressedItemData>();
                    for (int i = 0; i < compressionTasks.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var compressedItem = await compressionTasks[i];
                        compressedItems.Add(compressedItem);

                        progress?.Report(new CompressionProgress
                        {
                            CurrentFileIndex = i + 1,
                            TotalFiles = itemList.Count,
                            CurrentFileName = compressedItem.ItemName,
                            ProcessedBytes = processedBytes,
                            TotalBytes = totalSize,
                            Percentage = (double)processedBytes / totalSize * 100,
                            Status = $"Writing {compressedItem.ItemName}..."
                        });

                        // Write compressed item data
                        await WriteCompressedItemAsync(writer, compressedItem, cancellationToken);
                        processedBytes += compressedItem.OriginalSize;

                        progress?.Report(new CompressionProgress
                        {
                            CurrentFileIndex = i + 1,
                            TotalFiles = itemList.Count,
                            CurrentFileName = compressedItem.ItemName,
                            ProcessedBytes = processedBytes,
                            TotalBytes = totalSize,
                            Percentage = (double)processedBytes / totalSize * 100,
                            Status = $"Completed {compressedItem.ItemName}"
                        });
                    }
                }

                // Calculate final statistics
                var fileInfo = new FileInfo(outputPath);
                result.Success = true;
                result.OutputPath = outputPath;
                result.OriginalSize = totalSize;
                result.CompressedSize = fileInfo.Length;
                result.CompressionRatio = (double)result.CompressedSize / result.OriginalSize;
                result.Duration = DateTime.Now - startTime;

                return result;
            }
            catch (OperationCanceledException)
            {
                result.ErrorMessage = "Compression was cancelled.";
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Compression failed: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// كتابة رأس الملف المضغوط
        /// </summary>
        private async Task WriteHeaderAsync(BinaryWriter writer, List<ICompressibleItem> items, string password, CancellationToken cancellationToken)
        {
            // Write magic number and version
            writer.Write(Encoding.ASCII.GetBytes("CVS1")); // Compression Vault Shannon-Fano v1

            // Write password flag and hash
            bool hasPassword = !string.IsNullOrEmpty(password);
            writer.Write(hasPassword);
            if (hasPassword)
            {
                var passwordHash = ComputePasswordHash(password);
                writer.Write(passwordHash.Length);
                writer.Write(passwordHash);
            }

            // Write item count
            writer.Write(items.Count);

            // Write item metadata
            foreach (var item in items)
            {
                writer.Write(item.Name);
                writer.Write(item.Size);
                writer.Write(item.FileCount);
                writer.Write(item is CompressibleFolder);
            }

            await writer.BaseStream.FlushAsync(cancellationToken);
        }

        /// <summary>
        /// كتابة عنصر مضغوط
        /// </summary>
        private async Task WriteCompressedItemAsync(BinaryWriter writer, CompressedItemData compressedItem, CancellationToken cancellationToken)
        {
            if (compressedItem.FolderFiles != null)
            {
                // Write folder marker and process folder files
                writer.Write($"FOLDER:{compressedItem.ItemName}");
                foreach (var fileData in compressedItem.FolderFiles)
                {
                    writer.Write(fileData.ItemName);
                    await WriteCompressedItemAsync(writer, fileData, cancellationToken);
                }
            }
            else
            {
                // Write file data
                writer.Write(compressedItem.ItemName);
                writer.Write(compressedItem.OriginalSize);
                writer.Write(compressedItem.CompressedSize);

                if (compressedItem.IsCompressed && compressedItem.FrequencyTable != null)
                {
                    writer.Write(compressedItem.FrequencyTable.Count);
                    
                    // Write frequency table
                    foreach (var kvp in compressedItem.FrequencyTable)
                    {
                        writer.Write(kvp.Key);
                        writer.Write(kvp.Value);
                    }

                    // Write compressed data
                    writer.Write(compressedItem.CompressedData);
                    writer.Write(compressedItem.ValidBitsInLastByte);
                }
                else
                {
                    writer.Write(0); // No frequency table
                    writer.Write(compressedItem.CompressedData);
                }
            }
        }

        /// <summary>
        /// حساب hash كلمة المرور
        /// </summary>
        private byte[] ComputePasswordHash(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }
    }
} 