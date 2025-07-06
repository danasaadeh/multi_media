using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compression_Vault.Algorithms;
using Compression_Vault.Models;

namespace Compression_Vault.Services
{
    /// <summary>
    /// خدمة فك الضغط الموحدة
    /// </summary>
    public class DecompressionService
    {
        private readonly List<IDecompressionAlgorithm> _algorithms;

        public DecompressionService()
        {
            _algorithms = new List<IDecompressionAlgorithm>
            {
                new ShannonFanoDecompression(),
                new HuffmanDecompression()
            };
        }

        /// <summary>
        /// فك ضغط ملف تلقائياً بناءً على نوع الضغط
        /// </summary>
        public async Task<DecompressionResult> DecompressAsync(string inputPath, string outputDirectory, string password = null, IProgress<DecompressionProgress> progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Detect compression type from file
                var algorithm = DetectCompressionAlgorithm(inputPath);
                if (algorithm == null)
                {
                    return new DecompressionResult
                    {
                        Success = false,
                        ErrorMessage = "Unable to detect compression algorithm. File may be corrupted or not compressed."
                    };
                }

                return await algorithm.DecompressAsync(inputPath, outputDirectory, password, progress, cancellationToken);
            }
            catch (Exception ex)
            {
                return new DecompressionResult
                {
                    Success = false,
                    ErrorMessage = string.Format("Decompression failed: {0}", ex.Message)
                };
            }
        }

        /// <summary>
        /// فك ضغط باستخدام خوارزمية محددة
        /// </summary>
        public async Task<DecompressionResult> DecompressWithAlgorithmAsync(string algorithmName, string inputPath, string outputDirectory, string password = null, IProgress<DecompressionProgress> progress = null, CancellationToken cancellationToken = default)
        {
            var algorithm = _algorithms.FirstOrDefault(a => a.Name.Equals(algorithmName, StringComparison.OrdinalIgnoreCase));
            if (algorithm == null)
            {
                return new DecompressionResult
                {
                    Success = false,
                    ErrorMessage = string.Format("Unknown compression algorithm: {0}", algorithmName)
                };
            }

            return await algorithm.DecompressAsync(inputPath, outputDirectory, password, progress, cancellationToken);
        }

        /// <summary>
        /// الحصول على قائمة خوارزميات فك الضغط المتاحة
        /// </summary>
        public IEnumerable<string> GetAvailableAlgorithms()
        {
            return _algorithms.Select(a => a.Name);
        }

        /// <summary>
        /// كشف نوع خوارزمية الضغط من الملف
        /// </summary>
        private IDecompressionAlgorithm DetectCompressionAlgorithm(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(stream))
                {
                    if (stream.Length < 4)
                        return null;

                    var magicBytes = reader.ReadBytes(4);
                    var magicString = System.Text.Encoding.ASCII.GetString(magicBytes);

                    switch (magicString)
                    {
                        case "CVS1":
                            return _algorithms.FirstOrDefault(a => a is ShannonFanoDecompression);
                        case "CVH1":
                            return _algorithms.FirstOrDefault(a => a is HuffmanDecompression);
                        default:
                            return null;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// فك ضغط ملف واحد محدد من الأرشيف
        /// </summary>
        public async Task<DecompressionResult> ExtractSingleFileAsync(string inputPath, string fileName, string outputDirectory, string password = null, IProgress<DecompressionProgress> progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Detect compression type from file
                var algorithm = DetectCompressionAlgorithm(inputPath);
                if (algorithm == null)
                {
                    return new DecompressionResult
                    {
                        Success = false,
                        ErrorMessage = "Unable to detect compression algorithm. File may be corrupted or not compressed."
                    };
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
                        return new DecompressionResult
                        {
                            Success = false,
                            ErrorMessage = headerInfo.ErrorMessage
                        };
                    }

                    // Find the requested file
                    var targetItem = headerInfo.Items.FirstOrDefault(item => 
                        string.Equals(item.Name, fileName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(Path.GetFileName(item.Name), fileName, StringComparison.OrdinalIgnoreCase));

                    if (targetItem == null)
                    {
                        return new DecompressionResult
                        {
                            Success = false,
                            ErrorMessage = string.Format("File '{0}' not found in archive.", fileName)
                        };
                    }

                    // Extract the specific file
                    var result = new DecompressionResult { Success = false };
                    var startTime = DateTime.Now;

                    try
                    {
                        // Skip to the target item
                        await SkipToItemAsync(reader, targetItem, headerInfo.Items, cancellationToken);

                        // Extract the file
                        var outputPath = Path.Combine(outputDirectory, Path.GetFileName(targetItem.Name));
                        var outputDir = Path.GetDirectoryName(outputPath);
                        if (!string.IsNullOrEmpty(outputDir))
                        {
                            Directory.CreateDirectory(outputDir);
                        }

                        if (targetItem.IsFolder)
                        {
                            // Handle folder extraction
                            Directory.CreateDirectory(outputPath);
                            var folderMarker = reader.ReadString();
                            if (folderMarker.StartsWith("FOLDER:"))
                            {
                                // Extract all files in the folder
                                var extractedFiles = new List<string>();
                                while (reader.BaseStream.Position < reader.BaseStream.Length)
                                {
                                    try
                                    {
                                        var fileInFolder = reader.ReadString();
                                        if (fileInFolder.StartsWith("FOLDER:") || IsNextItemStart(reader))
                                        {
                                            reader.BaseStream.Position -= System.Text.Encoding.UTF8.GetByteCount(fileInFolder) + 4;
                                            break;
                                        }

                                        var fileOutputPath = Path.Combine(outputPath, fileInFolder);
                                        var fileDir = Path.GetDirectoryName(fileOutputPath);
                                        if (!string.IsNullOrEmpty(fileDir))
                                        {
                                            Directory.CreateDirectory(fileDir);
                                        }

                                        await ExtractFileAsync(reader, fileOutputPath, cancellationToken);
                                        extractedFiles.Add(fileOutputPath);
                                    }
                                    catch (EndOfStreamException)
                                    {
                                        break;
                                    }
                                }
                                result.ExtractedFiles = extractedFiles;
                            }
                        }
                        else
                        {
                            // Extract single file
                            await ExtractFileAsync(reader, outputPath, cancellationToken);
                            result.ExtractedFiles.Add(outputPath);
                        }

                        result.Success = true;
                        result.OutputDirectory = outputDirectory;
                        result.OriginalSize = new FileInfo(inputPath).Length;
                        result.DecompressedSize = result.ExtractedFiles.Sum(file => new FileInfo(file).Length);
                        result.DecompressionRatio = (double)result.DecompressedSize / result.OriginalSize;
                        result.Duration = DateTime.Now - startTime;

                        return result;
                    }
                    catch (Exception ex)
                    {
                        return new DecompressionResult
                        {
                            Success = false,
                            ErrorMessage = string.Format("Failed to extract file: {0}", ex.Message)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new DecompressionResult
                {
                    Success = false,
                    ErrorMessage = string.Format("Extraction failed: {0}", ex.Message)
                };
            }
        }

        /// <summary>
        /// Skip to a specific item in the archive
        /// </summary>
        private async Task SkipToItemAsync(BinaryReader reader, CompressedItemInfo targetItem, List<CompressedItemInfo> allItems, CancellationToken cancellationToken)
        {
            // Find the index of the target item
            int targetIndex = allItems.IndexOf(targetItem);
            if (targetIndex == -1)
            {
                throw new InvalidOperationException("Target item not found in item list");
            }

            // Skip through all items before the target
            for (int i = 0; i < targetIndex; i++)
            {
                var item = allItems[i];
                await SkipItemAsync(reader, item, cancellationToken);
            }
        }

        /// <summary>
        /// Skip an item without extracting it
        /// </summary>
        private async Task SkipItemAsync(BinaryReader reader, CompressedItemInfo item, CancellationToken cancellationToken)
        {
            if (item.IsFolder)
            {
                // Skip folder marker
                var folderMarker = reader.ReadString();
                if (folderMarker.StartsWith("FOLDER:"))
                {
                    // Skip all files in the folder
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        try
                        {
                            var fileName = reader.ReadString();
                            if (fileName.StartsWith("FOLDER:") || IsNextItemStart(reader))
                            {
                                reader.BaseStream.Position -= System.Text.Encoding.UTF8.GetByteCount(fileName) + 4;
                                break;
                            }
                            await SkipFileAsync(reader, cancellationToken);
                        }
                        catch (EndOfStreamException)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                await SkipFileAsync(reader, cancellationToken);
            }
        }

        /// <summary>
        /// Skip a file without extracting it
        /// </summary>
        private async Task SkipFileAsync(BinaryReader reader, CancellationToken cancellationToken)
        {
            var fileName = reader.ReadString();
            var originalSize = reader.ReadInt64();
            var compressedSize = reader.ReadInt64();
            int frequencyTableCount = reader.ReadInt32();

            if (frequencyTableCount > 0)
            {
                // Skip frequency table
                for (int i = 0; i < frequencyTableCount; i++)
                {
                    reader.ReadByte(); // key
                    reader.ReadInt32(); // value
                }

                // Skip compressed data
                reader.ReadBytes((int)compressedSize);
                reader.ReadByte(); // valid bits
            }
            else
            {
                // Skip uncompressed data
                reader.ReadBytes((int)compressedSize);
                reader.ReadByte(); // valid bits for uncompressed data
            }
        }

        /// <summary>
        /// Extract a single file
        /// </summary>
        private async Task ExtractFileAsync(BinaryReader reader, string outputPath, CancellationToken cancellationToken)
        {
            var fileName = reader.ReadString();
            var originalSize = reader.ReadInt64();
            var compressedSize = reader.ReadInt64();
            int frequencyTableCount = reader.ReadInt32();

            if (frequencyTableCount > 0)
            {
                // Read frequency table
                var frequencyTable = new Dictionary<byte, int>();
                for (int i = 0; i < frequencyTableCount; i++)
                {
                    var key = reader.ReadByte();
                    var value = reader.ReadInt32();
                    frequencyTable[key] = value;
                }

                // Read compressed data
                var compressedData = reader.ReadBytes((int)compressedSize);
                var validBitsInLastByte = reader.ReadByte();

                // Decompress data (this would need to be implemented based on the algorithm)
                var decompressedData = DecompressData(frequencyTable, compressedData, validBitsInLastByte, (int)originalSize);
                File.WriteAllBytes(outputPath, decompressedData);
            }
            else
            {
                // Copy uncompressed data
                var data = reader.ReadBytes((int)compressedSize);
                
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

        /// <summary>
        /// Decompress data (placeholder - would need algorithm-specific implementation)
        /// </summary>
        private byte[] DecompressData(Dictionary<byte, int> frequencyTable, byte[] compressedData, byte validBitsInLastByte, int originalSize)
        {
            // This is a placeholder - the actual decompression would depend on the algorithm
            // For now, return the compressed data as-is
            return compressedData;
        }

        /// <summary>
        /// Check if the current position is the start of the next item
        /// </summary>
        private bool IsNextItemStart(BinaryReader reader)
        {
            try
            {
                var currentPosition = reader.BaseStream.Position;
                var testString = reader.ReadString();
                
                if (string.IsNullOrEmpty(testString) || testString.Length > 260)
                {
                    reader.BaseStream.Position = currentPosition;
                    return true;
                }
                
                reader.BaseStream.Position = currentPosition;
                return false;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Read header information
        /// </summary>
        private async Task<HeaderInfo> ReadHeaderAsync(BinaryReader reader, string password, CancellationToken cancellationToken)
        {
            var headerInfo = new HeaderInfo { Items = new List<CompressedItemInfo>() };

            try
            {
                // Read magic bytes
                var magicBytes = reader.ReadBytes(4);
                var magicString = System.Text.Encoding.ASCII.GetString(magicBytes);

                if (magicString != "CVH1" && magicString != "CVS1")
                {
                    headerInfo.ErrorMessage = "Invalid file format. Expected CVH1 or CVS1 magic bytes.";
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
                    // Note: Password verification would need to be implemented based on the algorithm
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
            catch (Exception ex)
            {
                headerInfo.ErrorMessage = string.Format("Failed to read header: {0}", ex.Message);
                return headerInfo;
            }
        }

        // تم نقل الفئات المشتركة إلى Models/CompressionModels.cs
    }
} 