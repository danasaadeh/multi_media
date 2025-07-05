using System;

namespace Compression_Vault.Models
{
    public interface ICompressibleItem
    {
        string Name { get; }
        long Size { get; }
        int FileCount { get; }
        string DisplayText { get; }
        event EventHandler<ICompressibleItem> RemoveRequested;
        void RaiseRemoveRequest();
    }
} 