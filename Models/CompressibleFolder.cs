using System;
using System.IO;
using System.Linq;

namespace Compression_Vault.Models
{
    public class CompressibleFolder : ICompressibleItem
    {
        private readonly DirectoryInfo _directoryInfo;
        private readonly Lazy<FileInfo[]> _files;

        public CompressibleFolder(DirectoryInfo directoryInfo)
        {
            _directoryInfo = directoryInfo ?? throw new ArgumentNullException(nameof(directoryInfo));
            _files = new Lazy<FileInfo[]>(() => GetFilesSafely());
        }

        public string Name 
        { 
            get { return _directoryInfo.Name; } 
        }
        public string FullPath 
        { 
            get { return _directoryInfo.FullName; } 
        }
        public long Size 
        { 
            get { return _files.Value.Sum(f => f.Length); } 
        }
        public int FileCount 
        { 
            get { return _files.Value.Length; } 
        }
        public string DisplayText 
        { 
            get { return string.Format("{0} ({1} files)", Name, FileCount); } 
        }

        public event EventHandler<ICompressibleItem> RemoveRequested;

        public void RaiseRemoveRequest()
        {
            RemoveRequested?.Invoke(this, this);
        }

        private FileInfo[] GetFilesSafely()
        {
            try
            {
                return _directoryInfo.GetFiles("*", SearchOption.AllDirectories);
            }
            catch (Exception)
            {
                return new FileInfo[0];
            }
        }
    }
} 