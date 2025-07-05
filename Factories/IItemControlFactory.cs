using System.Windows.Forms;
using Compression_Vault.Models;

namespace Compression_Vault.Factories
{
    public interface IItemControlFactory
    {
        UserControl CreateControl(ICompressibleItem item);
    }
} 