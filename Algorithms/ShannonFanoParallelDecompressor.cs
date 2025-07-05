using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compression_Vault.Models;
using Compression_Vault.Services;

namespace Compression_Vault.Algorithms
{
    /// <summary>
    /// معالج متوازي لفك الضغط Shannon-Fano
    /// </summary>
    public static class ShannonFanoParallelDecompressor
    {
        /// <summary>
        /// فك ضغط عنصر واحد بشكل متوازي
        /// </summary>
        public static async Task<DecompressedItemData> DecompressItemParallelAsync(
            BinaryReader reader, 
            CompressedItemInfo itemInfo, 
            string outputDirectory, 
            SemaphoreSlim semaphore, 
            CancellationToken cancellationToken)
        {
            try
            {
                await semaphore.WaitAsync(cancellationToken);
                
                var result = new DecompressedItemData 
                { 
                    ItemName = itemInfo.Name, 
                    OriginalSize = itemInfo.Size 
                };

                if (itemInfo.IsFolder)
                {
                    // Handle folder decompression
                    var folderPath = Path.Combine(outputDirectory, itemInfo.Name);
                    Directory.CreateDirectory(folderPath);
                    result.OutputPath = folderPath;
                    result.Success = true;
                }
                else
                {
                    // Handle file decompression
                    var outputPath = Path.Combine(outputDirectory, itemInfo.Name);
                    var outputDir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }

                    await DecompressFileAsync(reader, outputPath, cancellationToken);
                    result.OutputPath = outputPath;
                    result.Success = true;
                }

                return result;
            }
            catch (Exception ex)
            {
                return new DecompressedItemData 
                { 
                    ItemName = itemInfo.Name, 
                    Success = false, 
                    ErrorMessage = ex.Message 
                };
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// فك ضغط ملف واحد
        /// </summary>
        private static async Task DecompressFileAsync(BinaryReader reader, string outputPath, CancellationToken cancellationToken)
        {
            try
            {
                // Read file metadata
                var fileName = reader.ReadString();
                var originalSize = reader.ReadInt64();
                var compressedSize = reader.ReadInt64();

                // Validate sizes
                if (originalSize < 0 || compressedSize < 0)
                {
                    throw new InvalidDataException("Invalid file size in metadata");
                }

                // Read frequency table count
                int frequencyTableCount = reader.ReadInt32();

                if (frequencyTableCount > 0)
                {
                    // Validate frequency table count
                    if (frequencyTableCount > 256)
                    {
                        throw new InvalidDataException("Invalid frequency table count");
                    }

                    // Read frequency table
                    var frequencyTable = new Dictionary<byte, int>();
                    for (int i = 0; i < frequencyTableCount; i++)
                    {
                        var key = reader.ReadByte();
                        var value = reader.ReadInt32();
                        frequencyTable[key] = value;
                    }

                    // Read compressed data
                    if (compressedSize > int.MaxValue)
                    {
                        throw new InvalidDataException("Compressed data too large");
                    }

                    var compressedData = reader.ReadBytes((int)compressedSize);
                    
                    // Check if we have enough data
                    if (compressedData.Length != compressedSize)
                    {
                        throw new EndOfStreamException("Insufficient compressed data");
                    }

                    var validBitsInLastByte = reader.ReadByte();

                    // Validate valid bits
                    if (validBitsInLastByte > 8)
                    {
                        throw new InvalidDataException("Invalid bits count in last byte");
                    }

                    // Build Shannon-Fano tree and decompress
                    var root = ShannonFanoTreeBuilder.BuildShannonFanoTree(frequencyTable);
                    var decompressedData = DecompressData(root, compressedData, validBitsInLastByte, (int)originalSize);

                    // Write decompressed data
                    File.WriteAllBytes(outputPath, decompressedData);
                }
                else
                {
                    // File was not compressed, just copy the data
                    if (compressedSize > int.MaxValue)
                    {
                        throw new InvalidDataException("Uncompressed data too large");
                    }

                    var data = reader.ReadBytes((int)compressedSize);
                    
                    // Check if we have enough data
                    if (data.Length != compressedSize)
                    {
                        throw new EndOfStreamException("Insufficient uncompressed data");
                    }

                    File.WriteAllBytes(outputPath, data);
                }
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidDataException(string.Format("Failed to read file data: {0}", ex.Message), ex);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(string.Format("Failed to decompress file: {0}", ex.Message), ex);
            }
        }

        /// <summary>
        /// فك ضغط البيانات باستخدام شجرة Shannon-Fano
        /// </summary>
        private static byte[] DecompressData(ShannonFanoNode root, byte[] compressedData, byte validBitsInLastByte, int originalSize)
        {
            var decompressedData = new List<byte>();
            var currentNode = root;

            for (int i = 0; i < compressedData.Length; i++)
            {
                var currentByte = compressedData[i];
                var bitsToProcess = (i == compressedData.Length - 1) ? validBitsInLastByte : 8;

                for (int bit = 0; bit < bitsToProcess; bit++)
                {
                    var bitValue = (currentByte >> (7 - bit)) & 1;

                    if (bitValue == 0)
                    {
                        currentNode = currentNode.Left;
                    }
                    else
                    {
                        currentNode = currentNode.Right;
                    }

                    if (currentNode.IsLeaf)
                    {
                        decompressedData.Add(currentNode.Symbol);
                        currentNode = root;

                        if (decompressedData.Count >= originalSize)
                        {
                            break;
                        }
                    }
                }

                if (decompressedData.Count >= originalSize)
                {
                    break;
                }
            }

            return decompressedData.ToArray();
        }

        /// <summary>
        /// معلومات العنصر المضغوط
        /// </summary>
        public class CompressedItemInfo
        {
            public string Name { get; set; }
            public long Size { get; set; }
            public int FileCount { get; set; }
            public bool IsFolder { get; set; }
        }

        /// <summary>
        /// بيانات العنصر المفكوك
        /// </summary>
        public class DecompressedItemData
        {
            public string ItemName { get; set; }
            public string OutputPath { get; set; }
            public long OriginalSize { get; set; }
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
        }
    }
} 