using Compression_Vault.Models;

namespace Compression_Vault.Controls
{
    public partial class FileItemControl : BaseItemControl
    {
        public FileItemControl(CompressibleFile file) : base(file)
        {
        }

        protected override string GetInfoText()
        {
            return string.Format("({0})", FormatFileSize(Item.Size));
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
            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }
    }
} 