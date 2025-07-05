using System.Collections.Generic;
using System.Linq;
using Compression_Vault.Models;

namespace Compression_Vault.Services
{
    public class CompressionSummaryService : ICompressionSummaryService
    {
        private const double EstimatedCompressionRatio = 0.5; // 50% compression

        public CompressionSummary CalculateSummary(IEnumerable<ICompressibleItem> items)
        {
            var itemList = items.ToList();
            
            var summary = new CompressionSummary
            {
                TotalFiles = itemList.Sum(item => item.FileCount),
                TotalSize = itemList.Sum(item => item.Size),
            };

            summary.EstimatedCompressedSize = (long)(summary.TotalSize * EstimatedCompressionRatio);
            summary.FormattedTotalSize = FormatFileSize(summary.TotalSize);
            summary.FormattedEstimatedSize = FormatFileSize(summary.EstimatedCompressedSize);

            return summary;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
} 