using Compression_Vault.Models;

namespace Compression_Vault.Controls
{
    public partial class FolderItemControl : BaseItemControl
    {
        public FolderItemControl(CompressibleFolder folder) : base(folder)
        {
        }

        protected override string GetInfoText()
        {
            return string.Format("({0} files)", Item.FileCount);
        }
    }
} 