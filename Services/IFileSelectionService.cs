using System.Collections.Generic;
using Compression_Vault.Models;

namespace Compression_Vault.Services
{
    public interface IFileSelectionService
    {
        IEnumerable<ICompressibleItem> SelectFiles();
        IEnumerable<ICompressibleItem> SelectFolders();
    }
} 