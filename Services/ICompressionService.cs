using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Compression_Vault.Models;

namespace Compression_Vault.Services
{
    public interface ICompressionService
    {
        Task<CompressionResult> CompressAsync(IEnumerable<ICompressibleItem> items, string outputPath, string algorithm, string password = null, IProgress<CompressionProgress> progress = null, CancellationToken cancellationToken = default);
        IEnumerable<string> GetAvailableAlgorithms();
    }
} 