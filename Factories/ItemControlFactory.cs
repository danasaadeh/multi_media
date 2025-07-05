using System.Windows.Forms;
using Compression_Vault.Controls;
using Compression_Vault.Models;

namespace Compression_Vault.Factories
{
    public class ItemControlFactory : IItemControlFactory
    {
        public UserControl CreateControl(ICompressibleItem item)
        {
            switch (item)
            {
                case CompressibleFile file:
                    return new FileItemControl(file);
                case CompressibleFolder folder:
                    return new FolderItemControl(folder);
                default:
                    throw new System.ArgumentException($"Unknown item type: {item.GetType().Name}");
            }
        }
    }
}