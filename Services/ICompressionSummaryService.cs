 using System.Collections.Generic;
using Compression_Vault.Models;

namespace Compression_Vault.Services
{
    public interface ICompressionSummaryService
    {
        CompressionSummary CalculateSummary(IEnumerable<ICompressibleItem> items);
    }

    public class CompressionSummary
    {
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public long EstimatedCompressedSize { get; set; }
        public string FormattedTotalSize { get; set; }
        public string FormattedEstimatedSize { get; set; }
    }
}