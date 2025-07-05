using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Compression_Vault.Models;

namespace Compression_Vault.Services
{
    public class FileSelectionService : IFileSelectionService
    {
        public IEnumerable<ICompressibleItem> SelectFiles()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Multiselect = true;
                openFileDialog.Title = "Select Files to Compress";
                openFileDialog.Filter = "All Files (*.*)|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    return openFileDialog.FileNames
                        .Where(File.Exists)
                        .Select(filePath => new CompressibleFile(new FileInfo(filePath)));
                }
            }
            return Enumerable.Empty<ICompressibleItem>();
        }

        public IEnumerable<ICompressibleItem> SelectFolders()
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select Folder to Compress";
                folderDialog.ShowNewFolderButton = false;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string folderPath = folderDialog.SelectedPath;
                    if (Directory.Exists(folderPath))
                    {
                        return new[] { new CompressibleFolder(new DirectoryInfo(folderPath)) };
                    }
                }
            }
            return Enumerable.Empty<ICompressibleItem>();
        }
    }
} 