using System;
using System.Collections.Generic;
using Compression_Vault.Models;

namespace Compression_Vault.Managers
{
    public interface ICompressionItemManager
    {
        event EventHandler ItemsChanged;
        IReadOnlyList<ICompressibleItem> Items { get; }
        void AddItems(IEnumerable<ICompressibleItem> items);
        void RemoveItem(ICompressibleItem item);
        void Clear();
    }
} 