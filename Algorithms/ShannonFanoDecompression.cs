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
    /// خوارزمية فك الضغط Shannon-Fano مع دعم المعالجة المتوازية
    /// </summary>
    public class ShannonFanoDecompression : IDecompressionAlgorithm
    {
        public string Name 
        { 
            get { return "Shannon-Fano Decompression"; } 
        }

        public async Task<DecompressionResult> DecompressAsync(string inputPath, string outputDirectory, string password = null, IProgress<DecompressionProgress> progress = null, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            var result = new DecompressionResult { Success = false };

            try
            {
                if (!File.Exists(inputPath))
                {
                    result.ErrorMessage = "Input file does not exist.";
                    return result;
                }

                // Create output directory if it doesn't exist
                Directory.CreateDirectory(outputDirectory);

                using (var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(inputStream))
                {
                    // Read and validate header
                    var headerInfo = await ReadHeaderAsync(reader, password, cancellationToken);
                    if (!headerInfo.IsValid)
                    {
                        result.ErrorMessage = headerInfo.ErrorMessage;
                        return result;
                    }

                    result.OriginalSize = new FileInfo(inputPath).Length;
                    long processedBytes = 0;
                    long totalBytes = headerInfo.Items.Sum(item => item.Size);

                    // Process items in parallel for better performance
                    var decompressionTasks = new List<Task<DecompressedItemData>>();
                    var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);

                    // Start parallel decompression tasks
                    for (int i = 0; i < headerInfo.Items.Count; i++)
                    {
                        var item = headerInfo.Items[i];
                        var task = DecompressItemParallelAsync(reader, item, outputDirectory, semaphore, cancellationToken);
                        decompressionTasks.Add(task);
                    }

                    // Wait for all decompression tasks to complete
                    for (int i = 0; i < decompressionTasks.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var decompressedItem = await decompressionTasks[i];
                        if (decompressedItem.Success)
                        {
                            result.ExtractedFiles.Add(decompressedItem.OutputPath);
                            processedBytes += decompressedItem.OriginalSize;

                            progress?.Report(new DecompressionProgress
                            {
                                CurrentFileIndex = i + 1,
                                TotalFiles = headerInfo.Items.Count,
                                CurrentFileName = decompressedItem.ItemName,
                                ProcessedBytes = processedBytes,
                                TotalBytes = totalBytes,
                                Percentage = (double)processedBytes / totalBytes * 100,
                                Status = string.Format("Completed {0}", decompressedItem.ItemName)
                            });
                        }
                        else
                        {
                            result.ErrorMessage = string.Format("Failed to decompress {0}: {1}", decompressedItem.ItemName, decompressedItem.ErrorMessage);
                            return result;
                        }
                    }
                }

                // Calculate final statistics
                result.Success = true;
                result.OutputDirectory = outputDirectory;
                result.DecompressedSize = result.ExtractedFiles.Sum(file => new FileInfo(file).Length);
                result.DecompressionRatio = (double)result.DecompressedSize / result.OriginalSize;
                result.Duration = DateTime.Now - startTime;

                return result;
            }
            catch (OperationCanceledException)
            {
                result.ErrorMessage = "Decompression was cancelled.";
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = string.Format("Decompression failed: {0}", ex.Message);
                return result;
            }
        }

        /// <summary>
        /// قراءة رأس الملف المضغوط
        /// </summary>
        private async Task<HeaderInfo> ReadHeaderAsync(BinaryReader reader, string password, CancellationToken cancellationToken)
        {
            var headerInfo = new HeaderInfo { Items = new List<CompressedItemInfo>() };

            try
            {
                // Read magic bytes
                var magicBytes = reader.ReadBytes(4);
                var magicString = System.Text.Encoding.ASCII.GetString(magicBytes);

                if (magicString != "CVS1")
                {
                    headerInfo.ErrorMessage = "Invalid file format. Expected CVS1 magic bytes.";
                    return headerInfo;
                }

                // Read password hash if present
                bool hasPassword = reader.ReadBoolean();
                if (hasPassword)
                {
                    if (string.IsNullOrEmpty(password))
                    {
                        headerInfo.ErrorMessage = "Password required but not provided.";
                        return headerInfo;
                    }

                    var hashLength = reader.ReadInt32();
                    if (hashLength < 0 || hashLength > 1024)
                    {
                        headerInfo.ErrorMessage = "Invalid password hash length.";
                        return headerInfo;
                    }

                    var storedHash = reader.ReadBytes(hashLength);
                    var computedHash = ComputePasswordHash(password);

                    if (!storedHash.SequenceEqual(computedHash))
                    {
                        headerInfo.ErrorMessage = "Incorrect password.";
                        return headerInfo;
                    }
                }

                // Read item count
                int itemCount = reader.ReadInt32();
                if (itemCount < 0 || itemCount > 10000)
                {
                    headerInfo.ErrorMessage = "Invalid item count in header.";
                    return headerInfo;
                }

                // Read items
                for (int i = 0; i < itemCount; i++)
                {
                    var item = new CompressedItemInfo
                    {
                        Name = reader.ReadString(),
                        Size = reader.ReadInt64(),
                        FileCount = reader.ReadInt32(),
                        IsFolder = reader.ReadBoolean()
                    };

                    // Validate item data
                    if (string.IsNullOrEmpty(item.Name) || item.Size < 0 || item.FileCount < 0)
                    {
                        headerInfo.ErrorMessage = string.Format("Invalid item data at index {0}.", i);
                        return headerInfo;
                    }

                    headerInfo.Items.Add(item);
                }

                headerInfo.IsValid = true;
                return headerInfo;
            }
            catch (EndOfStreamException ex)
            {
                headerInfo.ErrorMessage = string.Format("Failed to read header: Unexpected end of stream - {0}", ex.Message);
                return headerInfo;
            }
            catch (Exception ex)
            {
                headerInfo.ErrorMessage = string.Format("Failed to read header: {0}", ex.Message);
                return headerInfo;
            }
        }

        /// <summary>
        /// فك ضغط عنصر واحد بشكل متوازي
        /// </summary>
        private async Task<DecompressedItemData> DecompressItemParallelAsync(BinaryReader reader, CompressedItemInfo itemInfo, string outputDirectory, SemaphoreSlim semaphore, CancellationToken cancellationToken)
        {
            try
            {
                await semaphore.WaitAsync(cancellationToken);
                
                var result = new DecompressedItemData { ItemName = itemInfo.Name, OriginalSize = itemInfo.Size };

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
        private async Task DecompressFileAsync(BinaryReader reader, string outputPath, CancellationToken cancellationToken)
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
        /// فك ضغط البيانات باستخدام شجرة Shannon-Fano
        /// </summary>
        private byte[] DecompressData(ShannonFanoNode root, byte[] compressedData, byte validBitsInLastByte, int originalSize)
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
        private bool IsNextItemStart(BinaryReader reader)
        {
            try
            {
                var currentPosition = reader.BaseStream.Position;
                
                // Try to read a string that might be a file name
                var testString = reader.ReadString();
                
                // Check if it looks like a valid file name (not empty, reasonable length)
                if (string.IsNullOrEmpty(testString) || testString.Length > 260) // Max path length
                {
                    reader.BaseStream.Position = currentPosition;
                    return true; // Likely end of current item
                }
                
                // Put the string back
                reader.BaseStream.Position = currentPosition;
                return false;
            }
            catch
            {
                return true; // End of stream or error, likely end of current item
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

        /// <summary>
        /// معلومات الرأس
        /// </summary>
        private class HeaderInfo
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }
            public List<CompressedItemInfo> Items { get; set; }
        }

        /// <summary>
        /// معلومات العنصر المضغوط
        /// </summary>
        private class CompressedItemInfo
        {
            public string Name { get; set; }
            public long Size { get; set; }
            public int FileCount { get; set; }
            public bool IsFolder { get; set; }
        }

        /// <summary>
        /// بيانات العنصر المفكوك
        /// </summary>
        private class DecompressedItemData
        {
            public string ItemName { get; set; }
            public string OutputPath { get; set; }
            public long OriginalSize { get; set; }
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
        }
    }
} 