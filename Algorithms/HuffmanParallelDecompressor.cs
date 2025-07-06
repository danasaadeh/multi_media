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
    /// معالج متوازي لفك الضغط Huffman
    /// </summary>
    public static class HuffmanParallelDecompressor
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
                    
                    // Read folder marker and process folder contents
                    var folderMarker = reader.ReadString();
                    if (folderMarker.StartsWith("FOLDER:"))
                    {
                        // Process all files in the folder
                        var extractedFiles = new List<string>();
                        while (reader.BaseStream.Position < reader.BaseStream.Length)
                        {
                            try
                            {
                                // Try to read next file name
                                var fileName = reader.ReadString();
                                
                                // Check if this is the start of the next item (not a file in current folder)
                                if (fileName.StartsWith("FOLDER:") || IsNextItemStart(reader))
                                {
                                    // Put the string back and break
                                    reader.BaseStream.Position -= System.Text.Encoding.UTF8.GetByteCount(fileName) + 4; // +4 for string length
                                    break;
                                }
                                
                                // Decompress the file in the folder
                                var fileOutputPath = Path.Combine(folderPath, fileName);
                                var fileDir = Path.GetDirectoryName(fileOutputPath);
                                if (!string.IsNullOrEmpty(fileDir))
                                {
                                    Directory.CreateDirectory(fileDir);
                                }
                                
                                await DecompressFileAsync(reader, fileOutputPath, cancellationToken);
                                extractedFiles.Add(fileOutputPath);
                            }
                            catch (EndOfStreamException)
                            {
                                // End of stream, folder processing complete
                                break;
                            }
                        }
                        
                        result.OutputPath = folderPath;
                        result.Success = true;
                    }
                    else
                    {
                        result.Success = true;
                        result.OutputPath = folderPath;
                    }
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
                    if (frequencyTableCount <= 0 || frequencyTableCount > 256)
                    {
                        throw new InvalidDataException("Invalid frequency table count");
                    }

                    // Read frequency table
                    var frequencyTable = new Dictionary<byte, int>();
                    for (int i = 0; i < frequencyTableCount; i++)
                    {
                        var key = reader.ReadByte();
                        var value = reader.ReadInt32();
                        
                        // Validate frequency value
                        if (value <= 0)
                        {
                            throw new InvalidDataException("Invalid frequency value in table");
                        }
                        
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
                    if (validBitsInLastByte > 8 || validBitsInLastByte < 0)
                    {
                        throw new InvalidDataException("Invalid bits count in last byte");
                    }

                    // Build Huffman tree and decompress
                    var root = HuffmanTreeBuilder.BuildHuffmanTree(frequencyTable);
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

                    // Read valid bits for uncompressed data (should be 8 for non-empty files, 0 for empty files)
                    var validBitsInLastByte = reader.ReadByte();
                    if (validBitsInLastByte != 8 && validBitsInLastByte != 0)
                    {
                        throw new InvalidDataException("Invalid valid bits for uncompressed data");
                    }
                    
                    // For empty files, validBitsInLastByte should be 0
                    if (compressedSize == 0 && validBitsInLastByte != 0)
                    {
                        throw new InvalidDataException("Invalid valid bits for empty file");
                    }
                    
                    // For non-empty files, validBitsInLastByte should be 8
                    if (compressedSize > 0 && validBitsInLastByte != 8)
                    {
                        throw new InvalidDataException("Invalid valid bits for non-empty file");
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
        /// فك ضغط البيانات باستخدام شجرة Huffman
        /// </summary>
        private static byte[] DecompressData(HuffmanNode root, byte[] compressedData, byte validBitsInLastByte, int originalSize)
        {
            // Handle empty data case
            if (originalSize == 0 || compressedData.Length == 0)
            {
                return new byte[0];
            }
            
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
        /// Check if the current position is the start of the next item
        /// </summary>
        private static bool IsNextItemStart(BinaryReader reader)
        {
            try
            {
                long currentPosition = reader.BaseStream.Position;
                
                // Try to read a string (item name)
                var itemName = reader.ReadString();
                
                // Check if it looks like a valid item name (not empty, reasonable length)
                if (string.IsNullOrEmpty(itemName) || itemName.Length > 260)
                {
                    reader.BaseStream.Position = currentPosition;
                    return false;
                }
                
                // Try to read the size (should be a reasonable value)
                var size = reader.ReadInt64();
                if (size < 0 || size > 1024L * 1024L * 1024L * 1024L) // 1TB max
                {
                    reader.BaseStream.Position = currentPosition;
                    return false;
                }
                
                // Try to read file count
                var fileCount = reader.ReadInt32();
                if (fileCount < 0 || fileCount > 100000)
                {
                    reader.BaseStream.Position = currentPosition;
                    return false;
                }
                
                // Try to read is folder flag
                var isFolder = reader.ReadBoolean();
                
                // If we got here, it looks like the start of a new item
                reader.BaseStream.Position = currentPosition;
                return true;
            }
            catch
            {
                // If any exception occurs, it's not the start of a new item
                return false;
            }
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