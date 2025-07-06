using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compression_Vault.Models;
using Compression_Vault.Services;

namespace Compression_Vault.Managers
{
    /// <summary>
    /// مدير فك الضغط
    /// </summary>
    public class DecompressionManager
    {
        private readonly DecompressionService _decompressionService;
        private readonly DecompressionOptions _options;

        public DecompressionManager(DecompressionOptions options = null)
        {
            _decompressionService = new DecompressionService();
            _options = options ?? new DecompressionOptions();
        }

        /// <summary>
        /// فك ضغط ملف أو مجلد
        /// </summary>
        public async Task<DecompressionResult> DecompressAsync(DecompressionInfo info, IProgress<DecompressionProgress> progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (!File.Exists(info.InputPath))
                {
                    return new DecompressionResult
                    {
                        Success = false,
                        ErrorMessage = "Input file does not exist."
                    };
                }

                // Create output directory
                Directory.CreateDirectory(info.OutputDirectory);

                // Auto-detect algorithm if requested
                if (info.AutoDetectAlgorithm)
                {
                    return await _decompressionService.DecompressAsync(info.InputPath, info.OutputDirectory, info.Password, progress, cancellationToken);
                }
                else
                {
                    return await _decompressionService.DecompressWithAlgorithmAsync(info.Algorithm, info.InputPath, info.OutputDirectory, info.Password, progress, cancellationToken);
                }
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
        /// فك ضغط متعدد الملفات
        /// </summary>
        public async Task<List<DecompressionResult>> DecompressMultipleAsync(List<DecompressionInfo> infos, IProgress<DecompressionProgress> progress = null, CancellationToken cancellationToken = default)
        {
            var results = new List<DecompressionResult>();
            var semaphore = new SemaphoreSlim(_options.MaxDegreeOfParallelism, _options.MaxDegreeOfParallelism);

            var tasks = infos.Select(async info =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    return await DecompressAsync(info, progress, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var completedResults = await Task.WhenAll(tasks);
            results.AddRange(completedResults);

            return results;
        }

        /// <summary>
        /// الحصول على معلومات الملف المضغوط
        /// </summary>
        public async Task<CompressedFileInfo> GetCompressedFileInfoAsync(string filePath)
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

                    string algorithm;
                    switch (magicString)
                    {
                        case "CVS1":
                            algorithm = "Shannon-Fano";
                            break;
                        case "CVH1":
                            algorithm = "Huffman";
                            break;
                        default:
                            algorithm = "Unknown";
                            break;
                    }

                    // Read basic header info
                    bool hasPassword = reader.ReadBoolean();
                    if (hasPassword)
                    {
                        var hashLength = reader.ReadInt32();
                        reader.BaseStream.Position += hashLength;
                    }

                    int itemCount = reader.ReadInt32();
                    var items = new List<CompressedItemInfo>();

                    for (int i = 0; i < itemCount; i++)
                    {
                        var item = new CompressedItemInfo
                        {
                            Name = reader.ReadString(),
                            Size = reader.ReadInt64(),
                            FileCount = reader.ReadInt32(),
                            IsFolder = reader.ReadBoolean()
                        };
                        items.Add(item);
                    }

                    return new CompressedFileInfo
                    {
                        FilePath = filePath,
                        Algorithm = algorithm,
                        HasPassword = hasPassword,
                        ItemCount = itemCount,
                        Items = items,
                        TotalSize = items.Sum(i => i.Size),
                        CompressedSize = new FileInfo(filePath).Length
                    };
                }
            }
            catch (Exception ex)
            {
                return new CompressedFileInfo
                {
                    FilePath = filePath,
                    Algorithm = "Unknown",
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// التحقق من صحة الملف المضغوط
        /// </summary>
        public async Task<bool> ValidateCompressedFileAsync(string filePath, string password = null)
        {
            try
            {
                var info = await GetCompressedFileInfoAsync(filePath);
                if (info == null || !string.IsNullOrEmpty(info.ErrorMessage))
                    return false;

                if (info.HasPassword && string.IsNullOrEmpty(password))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// الحصول على قائمة خوارزميات فك الضغط المتاحة
        /// </summary>
        public IEnumerable<string> GetAvailableAlgorithms()
        {
            return _decompressionService.GetAvailableAlgorithms();
        }
    }

    /// <summary>
    /// معلومات الملف المضغوط
    /// </summary>
    public class CompressedFileInfo
    {
        public string FilePath { get; set; }
        public string Algorithm { get; set; }
        public bool HasPassword { get; set; }
        public int ItemCount { get; set; }
        public List<CompressedItemInfo> Items { get; set; }

        public CompressedFileInfo()
        {
            Items = new List<CompressedItemInfo>();
        }
        public long TotalSize { get; set; }
        public long CompressedSize { get; set; }
        public string ErrorMessage { get; set; }
        public double CompressionRatio 
        { 
            get { return TotalSize > 0 ? (double)CompressedSize / TotalSize : 0; } 
        }
    }

    // تم نقل الفئة CompressedItemInfo إلى Models/CompressionModels.cs
} 