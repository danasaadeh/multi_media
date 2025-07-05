using System;
using System.IO;

namespace Compression_Vault.Models
{
    public class CompressibleFile : ICompressibleItem
    {
        private readonly FileInfo _fileInfo;

        public CompressibleFile(FileInfo fileInfo)
        {
            _fileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
        }

        public string Name => _fileInfo.Name;
        public string FullPath => _fileInfo.FullName;
        public long Size => _fileInfo.Length;
        public int FileCount => 1;
        public string DisplayText 
        { 
            get { return string.Format("{0} ({1})", Name, FormatFileSize(Size)); } 
        }

        public event EventHandler<ICompressibleItem> RemoveRequested;

        public void RaiseRemoveRequest()
        {
            RemoveRequested?.Invoke(this, this);
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