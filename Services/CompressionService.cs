using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compression_Vault.Algorithms;
using Compression_Vault.Models;

namespace Compression_Vault.Services
{
    public class CompressionService : ICompressionService
    {
        private readonly Dictionary<string, ICompressionAlgorithm> _algorithms;

        public CompressionService()
        {
            _algorithms = new Dictionary<string, ICompressionAlgorithm>
            {
                { "Huffman", new HuffmanCompression() },
                { "Shannon-Fano", new ShannonFanoCompression() }
                // Future: Add Shannon-Fano and other algorithms here
            };
        }

        public async Task<CompressionResult> CompressAsync(IEnumerable<ICompressibleItem> items, string outputPath, string algorithm, string password = null, IProgress<CompressionProgress> progress = null, CancellationToken cancellationToken = default)
        {
            if (!_algorithms.ContainsKey(algorithm))
            {
                return new CompressionResult
                {
                    Success = false,
                    ErrorMessage = string.Format("Algorithm '{0}' is not supported.", algorithm)
                };
            }

            var selectedAlgorithm = _algorithms[algorithm];
            return await selectedAlgorithm.CompressAsync(items, outputPath, password, progress, cancellationToken);
        }

        public IEnumerable<string> GetAvailableAlgorithms()
        {
            return _algorithms.Keys;
        }
    }
} 