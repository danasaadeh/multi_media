using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Compression_Vault.Models;

namespace Compression_Vault.Services
{
    public interface ICompressionAlgorithm
    {
        string Name { get; }
        Task<CompressionResult> CompressAsync(IEnumerable<ICompressibleItem> items, string outputPath, string password = null, IProgress<CompressionProgress> progress = null, CancellationToken cancellationToken = default);
    }

    public class CompressionResult
    {
        public bool Success { get; set; }
        public string OutputPath { get; set; }
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
        public double CompressionRatio { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class CompressionProgress
    {
        public int CurrentFileIndex { get; set; }
        public int TotalFiles { get; set; }
        public string CurrentFileName { get; set; }
        public long ProcessedBytes { get; set; }
        public long TotalBytes { get; set; }
        public double Percentage { get; set; }
        public string Status { get; set; }
    }
} 