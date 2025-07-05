using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Compression_Vault.Models;

namespace Compression_Vault.Services
{
    public interface IDecompressionAlgorithm
    {
        string Name { get; }
        Task<DecompressionResult> DecompressAsync(string inputPath, string outputDirectory, string password = null, IProgress<DecompressionProgress> progress = null, CancellationToken cancellationToken = default);
    }

    public class DecompressionResult
    {
        public bool Success { get; set; }
        public string OutputDirectory { get; set; }
        public long OriginalSize { get; set; }
        public long DecompressedSize { get; set; }
        public double DecompressionRatio { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> ExtractedFiles { get; set; }

        public DecompressionResult()
        {
            ExtractedFiles = new List<string>();
        }
    }

    public class DecompressionProgress
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