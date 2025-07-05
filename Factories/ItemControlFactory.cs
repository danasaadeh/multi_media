using System;
using System.Windows.Forms;
using Compression_Vault.Controls;
using Compression_Vault.Models;

namespace Compression_Vault.Factories
{
    public class ItemControlFactory : IItemControlFactory
    {
        public UserControl CreateControl(ICompressibleItem item)
        {
            if (item is CompressibleFile file)
            {
                return new FileItemControl(file);
            }
            else if (item is CompressibleFolder folder)
            {
                return new FolderItemControl(folder);
            }
            else
            {
                throw new ArgumentException(string.Format("Unknown item type: {0}", item.GetType().Name));
            }
        }
    }
}