using System;
using System.Collections.Generic;

namespace Compression_Vault.Models
{
    /// <summary>
    /// نموذج معلومات فك الضغط
    /// </summary>
    public class DecompressionInfo
    {
        public string InputPath { get; set; }
        public string OutputDirectory { get; set; }
        public string Password { get; set; }
        public string Algorithm { get; set; }
        public bool AutoDetectAlgorithm { get; set; }
        public int MaxConcurrency { get; set; }

        public DecompressionInfo()
        {
            AutoDetectAlgorithm = true;
            MaxConcurrency = Environment.ProcessorCount;
        }
    }

    /// <summary>
    /// نموذج إحصائيات فك الضغط
    /// </summary>
    public class DecompressionStatistics
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration 
        { 
            get { return EndTime - StartTime; } 
        }
        public long OriginalSize { get; set; }
        public long DecompressedSize { get; set; }
        public double DecompressionRatio { get; set; }
        public int TotalFiles { get; set; }
        public int SuccessfullyDecompressedFiles { get; set; }
        public int FailedFiles { get; set; }
        public List<string> ExtractedFiles { get; set; }
        public List<string> Errors { get; set; }

        public DecompressionStatistics()
        {
            ExtractedFiles = new List<string>();
            Errors = new List<string>();
        }
    }

    /// <summary>
    /// نموذج حالة فك الضغط
    /// </summary>
    public class DecompressionStatus
    {
        public bool IsRunning { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsCancelled { get; set; }
        public bool HasErrors { get; set; }
        public string CurrentFile { get; set; }
        public int CurrentFileIndex { get; set; }
        public int TotalFiles { get; set; }
        public double ProgressPercentage { get; set; }
        public string StatusMessage { get; set; }
        public DecompressionStatistics Statistics { get; set; }
    }

    /// <summary>
    /// نموذج خيارات فك الضغط
    /// </summary>
    public class DecompressionOptions
    {
        public bool OverwriteExistingFiles { get; set; }
        public bool CreateSubdirectories { get; set; }
        public bool PreserveFileAttributes { get; set; }
        public bool ValidateChecksums { get; set; }
        public int BufferSize { get; set; }
        public bool EnableParallelProcessing { get; set; }
        public int MaxDegreeOfParallelism { get; set; }

        public DecompressionOptions()
        {
            OverwriteExistingFiles = false;
            CreateSubdirectories = true;
            PreserveFileAttributes = true;
            ValidateChecksums = true;
            BufferSize = 8192;
            EnableParallelProcessing = true;
            MaxDegreeOfParallelism = Environment.ProcessorCount;
        }
    }
} 