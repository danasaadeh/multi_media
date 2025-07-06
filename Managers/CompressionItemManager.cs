using System;
using System.Collections.Generic;
using System.Linq;
using Compression_Vault.Models;

namespace Compression_Vault.Managers
{
    public class CompressionItemManager : ICompressionItemManager
    {
        private readonly List<ICompressibleItem> _items = new List<ICompressibleItem>();

        public event EventHandler ItemsChanged;

        public IReadOnlyList<ICompressibleItem> Items 
        { 
            get { return _items.AsReadOnly(); } 
        }

        public void AddItems(IEnumerable<ICompressibleItem> items)
        {
            if (items == null) return;

            var newItems = items.ToList();
            if (newItems.Any())
            {
                foreach (var item in newItems)
                {
                    item.RemoveRequested += OnItemRemoveRequested;
                }
                _items.AddRange(newItems);
                OnItemsChanged();
            }
        }

        public void RemoveItem(ICompressibleItem item)
        {
            if (item != null && _items.Remove(item))
            {
                item.RemoveRequested -= OnItemRemoveRequested;
                OnItemsChanged();
            }
        }

        public void Clear()
        {
            if (_items.Any())
            {
                foreach (var item in _items)
                {
                    item.RemoveRequested -= OnItemRemoveRequested;
                }
                _items.Clear();
                OnItemsChanged();
            }
        }

        private void OnItemRemoveRequested(object sender, ICompressibleItem item)
        {
            RemoveItem(item);
        }

        private void OnItemsChanged()
        {
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
} 