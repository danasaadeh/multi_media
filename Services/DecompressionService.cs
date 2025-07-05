using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compression_Vault.Algorithms;

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
    }
} 