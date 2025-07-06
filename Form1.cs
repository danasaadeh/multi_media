using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Compression_Vault.Controls;
using Compression_Vault.Factories;
using Compression_Vault.Managers;
using Compression_Vault.Models;
using Compression_Vault.Services;

namespace Compression_Vault
{
    public partial class Form1 : Form
    {
        private readonly IFileSelectionService _fileSelectionService;
        private readonly ICompressionSummaryService _summaryService;
        private readonly ICompressionItemManager _itemManager;
        private readonly IItemControlFactory _controlFactory;
        private readonly ICompressionService _compressionService;
        private readonly DecompressionManager _decompressionManager;
        private readonly DecompressionService _decompressionService;
        private FlowLayoutPanel _flowPanelFiles;
        private CancellationTokenSource _cancellationTokenSource;
        private System.Windows.Forms.Timer _statusTimer;
        private CompressionResult _lastCompressionResult;
        private DecompressionResult _lastDecompressionResult;
        private string _currentCompressionPath; // مسار الملف المضغوط الحالي
        private string _currentExtractionPath; // مسار مجلد الاستخراج الحالي

        public Form1()
        {
            InitializeComponent();

            _fileSelectionService = new FileSelectionService();
            _summaryService = new CompressionSummaryService();
            _itemManager = new CompressionItemManager();
            _controlFactory = new ItemControlFactory();
            _compressionService = new CompressionService();
            _decompressionManager = new DecompressionManager();
            _decompressionService = new DecompressionService();

            _statusTimer = new System.Windows.Forms.Timer();
            _statusTimer.Interval = 5000;
            _statusTimer.Tick += (s, e) =>
            {
                lblStatus.Text = "Ready to compress";
                _statusTimer.Stop();
            };

            InitializeFilePanel();
            WireUpEvents();
            UpdateCompressionStats();
        }

        private void InitializeFilePanel()
        {
            _flowPanelFiles = new FlowLayoutPanel
            {
                Location = listBoxFiles.Location,
                Size = listBoxFiles.Size,
                Name = "flowPanelFiles",
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            tabCompress.Controls.Remove(listBoxFiles);
            tabCompress.Controls.Add(_flowPanelFiles);

            // Initialize ListView context menu for single file extraction
            InitializeListViewContextMenu();
        }

        private void InitializeListViewContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            
            var extractAllItem = new ToolStripMenuItem("Extract All Files");
            extractAllItem.Click += (s, e) => BtnExtract_Click(s, e);
            
            var extractSingleItem = new ToolStripMenuItem("Extract Single File...");
            extractSingleItem.Click += (s, e) => ExtractSingleFile();
            
            contextMenu.Items.Add(extractAllItem);
            contextMenu.Items.Add(extractSingleItem);
            
            listViewArchive.ContextMenuStrip = contextMenu;
        }

        private void WireUpEvents()
        {
            btnAddFiles.Click += BtnAddFiles_Click;
            btnAddFolder.Click += BtnAddFolder_Click;
            btnStart.Click += BtnStart_Click;
            _itemManager.ItemsChanged += OnItemsChanged;
            togglePassword.CheckedChanged += TogglePassword_CheckedChanged;
            
            // Decompression events
            btnExtract.Click += BtnExtract_Click;
            btnBrowseArchive.Click += BtnBrowseArchive_Click;
            btnBrowseExtractPath.Click += BtnBrowseExtractPath_Click;
            
            // Tab control events
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            
            // ListView events
            listViewArchive.SelectedIndexChanged += ListViewArchive_SelectedIndexChanged;
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == tabExtract)
            {
                LoadCompressedArchives();
            }
        }

        private void BtnAddFiles_Click(object sender, EventArgs e)
        {
            var selectedFiles = _fileSelectionService.SelectFiles();
            _itemManager.AddItems(selectedFiles);
        }

        private void BtnAddFolder_Click(object sender, EventArgs e)
        {
            var selectedFolders = _fileSelectionService.SelectFolders();
            _itemManager.AddItems(selectedFolders);
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (btnStart.Text == "Start Compression")
            {
                StartCompression();
            }
            else
            {
                CancelCompression();
            }
        }

        private void TogglePassword_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.Enabled = togglePassword.Checked;
            if (!togglePassword.Checked)
            {
                txtPassword.Text = string.Empty;
            }
        }

        private async void StartCompression()
        {
            if (!_itemManager.Items.Any())
            {
                MessageBox.Show("Please add files or folders to compress.", "No Items", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _lastCompressionResult = null;
            UpdateCompressionStats(null);

            string staticFolder = Path.Combine(Application.StartupPath, "CompressedFiles");
            Directory.CreateDirectory(staticFolder);

            string inputName = PromptForArchiveName();
            if (string.IsNullOrWhiteSpace(inputName))
            {
                MessageBox.Show("Compression cancelled. No name was provided.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string fileName = inputName.EndsWith(".cva") ? inputName : inputName + ".cva";

            string outputPath = Path.Combine(staticFolder, fileName);

            await PerformCompression(outputPath);


        }

        private async Task PerformCompression(string outputPath)
        {
            try
            {
                // تخزين مسار الملف المضغوط الحالي
                _currentCompressionPath = outputPath;
                
                btnStart.Text = "Cancel Compression";
                btnStart.Enabled = true;
                progressBar.Value = 0;
                progressBar.Style = ProgressBarStyle.Continuous;
                lblStatus.Text = "Preparing compression...";

                btnAddFiles.Enabled = false;
                btnAddFolder.Enabled = false;
                radioHuffman.Enabled = false;
                radioShannon.Enabled = false;
                togglePassword.Enabled = false;
                txtPassword.Enabled = false;

                _cancellationTokenSource = new CancellationTokenSource();
                string algorithm = radioHuffman.Checked ? "Huffman" : "Shannon-Fano";
                string password = togglePassword.Checked ? txtPassword.Text : null;
                var progress = new Progress<CompressionProgress>(UpdateProgress);

                var result = await _compressionService.CompressAsync(
                    _itemManager.Items,
                    outputPath,
                    algorithm,
                    password,
                    progress,
                    _cancellationTokenSource.Token);

                if (result.Success)
                {
                    // Safely calculate compression ratio
                    double ratio = CalculateCompressionRatio(result.OriginalSize, result.CompressedSize);
                    result.CompressionRatio = ratio;

                    _lastCompressionResult = result;
                    UpdateCompressionStats(result);
                    lblStatus.Text = string.Format("Compression completed successfully! Original: {0}, Compressed: {1}, Ratio: {2:P2}, Time: {3:mm\\:ss}", FormatFileSize(result.OriginalSize), FormatFileSize(result.CompressedSize), ratio, result.Duration);
                    _statusTimer.Start();
                    LoadCompressedArchives(); // Refresh list after new file saved

                }

                else
                {
                    // حذف الملف المضغوط في حالة الفشل
                    if (!string.IsNullOrEmpty(_currentCompressionPath) && File.Exists(_currentCompressionPath))
                    {
                        try
                        {
                            File.Delete(_currentCompressionPath);
                        }
                        catch { /* تجاهل أخطاء الحذف */ }
                    }
                    
                    MessageBox.Show(string.Format("Compression failed: {0}", result.ErrorMessage), "Compression Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Compression was cancelled.", "Compression Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // حذف الملف المضغوط في حالة حدوث خطأ
                if (!string.IsNullOrEmpty(_currentCompressionPath) && File.Exists(_currentCompressionPath))
                {
                    try
                    {
                        File.Delete(_currentCompressionPath);
                    }
                    catch { /* تجاهل أخطاء الحذف */ }
                }
                
                MessageBox.Show(string.Format("An error occurred: {0}", ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ResetUI();
            }
        }

        private void CancelCompression()
        {
            _cancellationTokenSource?.Cancel();
            lblStatus.Text = "Cancelling compression...";
            
            // حذف الملف المضغوط إذا كان موجوداً
            if (!string.IsNullOrEmpty(_currentCompressionPath) && File.Exists(_currentCompressionPath))
            {
                try
                {
                    File.Delete(_currentCompressionPath);
                    lblStatus.Text = "Compression cancelled and file deleted.";
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "Compression cancelled but could not delete file: " + ex.Message;
                }
            }
        }

        private void CancelExtraction()
        {
            _cancellationTokenSource?.Cancel();
            lblExtractStatus.Text = "Cancelling extraction...";
            
            // حذف الملفات المستخرجة إذا كان المجلد موجوداً
            if (!string.IsNullOrEmpty(_currentExtractionPath) && Directory.Exists(_currentExtractionPath))
            {
                try
                {
                    CleanupExtractedFiles(_currentExtractionPath);
                    lblExtractStatus.Text = "Extraction cancelled and extracted files deleted.";
                }
                catch (Exception ex)
                {
                    lblExtractStatus.Text = "Extraction cancelled but could not delete extracted files: " + ex.Message;
                }
            }
        }

        /// <summary>
        /// تنظيف الملفات المستخرجة من مجلد معين
        /// </summary>
        private void CleanupExtractedFiles(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return;

            try
            {
                // حذف جميع الملفات في المجلد
                var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch { /* تجاهل أخطاء حذف الملفات الفردية */ }
                }
                
                // حذف المجلدات الفارغة
                var directories = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories)
                    .OrderByDescending(d => d.Length); // حذف المجلدات الفرعية أولاً
                
                foreach (var dir in directories)
                {
                    try
                    {
                        if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                        {
                            Directory.Delete(dir);
                        }
                    }
                    catch { /* تجاهل أخطاء حذف المجلدات الفردية */ }
                }
                
                // حذف المجلد الرئيسي إذا كان فارغاً
                if (Directory.Exists(directoryPath) && !Directory.EnumerateFileSystemEntries(directoryPath).Any())
                {
                    Directory.Delete(directoryPath);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to cleanup extracted files: {ex.Message}", ex);
            }
        }

        private void UpdateProgress(CompressionProgress progress)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<CompressionProgress>(UpdateProgress), progress);
                return;
            }

            int clampedValue = Math.Max(progressBar.Minimum, Math.Min(progressBar.Maximum, (int)progress.Percentage));
            progressBar.Value = clampedValue;
            lblStatus.Text = progress.Status;
        }

        private void UpdateCompressionStats(CompressionResult result = null)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<CompressionResult>(UpdateCompressionStats), result);
                return;
            }

            if (result != null)
            {
                lblOriginalSize.Text = string.Format("Original Size: {0}", FormatFileSize(result.OriginalSize));
                lblCompressedSize.Text = string.Format("Compressed Size: {0}", FormatFileSize(result.CompressedSize));
                lblRatio.Text = string.Format("Compression Ratio: {0:P2}", result.CompressionRatio);
                lblSaved.Text = string.Format("Space Saved: {0}", FormatFileSize(result.OriginalSize - result.CompressedSize));
            }
            else
            {
                lblOriginalSize.Text = "Original Size: 0 MB";
                lblCompressedSize.Text = "Compressed Size: 0 MB";
                lblRatio.Text = "Compression Ratio: -";
                lblSaved.Text = "Space Saved: -";
            }
        }

        private void ResetUI()
        {
            btnStart.Text = "Start Compression";
            btnStart.Enabled = true;
            progressBar.Value = 0;
            progressBar.Style = ProgressBarStyle.Continuous;

            if (!_statusTimer.Enabled)
                lblStatus.Text = "Ready to compress";

            btnAddFiles.Enabled = true;
            btnAddFolder.Enabled = true;
            radioHuffman.Enabled = true;
            radioShannon.Enabled = true;
            togglePassword.Enabled = true;
            txtPassword.Enabled = togglePassword.Checked;

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            
            // مسح مسار الملف المضغوط الحالي
            _currentCompressionPath = null;
        }

        private void OnItemsChanged(object sender, EventArgs e)
        {
            UpdateFileList();
            UpdateSummary();

            if (_lastCompressionResult != null)
                UpdateCompressionStats(_lastCompressionResult);
        }

        private void UpdateFileList()
        {
            _flowPanelFiles.Controls.Clear();

            foreach (var item in _itemManager.Items)
            {
                var control = _controlFactory.CreateControl(item);
                _flowPanelFiles.Controls.Add(control);
            }
        }

        private void UpdateSummary()
        {
            var summary = _summaryService.CalculateSummary(_itemManager.Items);

            lblTotalFiles.Text = string.Format("Total Files: {0}", summary.TotalFiles);
            lblTotalSize.Text = string.Format("Total Size: {0}", summary.FormattedTotalSize);
            lblEstimated.Text = string.Format("Estimated Compression: ~{0}", summary.FormattedEstimatedSize);
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }

        private double CalculateCompressionRatio(long originalSize, long compressedSize)
        {
            if (originalSize == 0) return 0;
            return (double)compressedSize / originalSize;
        }

        private void LoadCompressedArchives()
        {
            string staticFolder = Path.Combine(Application.StartupPath, "CompressedFiles");
            Directory.CreateDirectory(staticFolder);

            listViewArchive.Items.Clear();
            
            // Add columns if they don't exist
            if (listViewArchive.Columns.Count == 0)
            {
                listViewArchive.Columns.Add("Name", 200);
                listViewArchive.Columns.Add("Size", 100);
                listViewArchive.Columns.Add("Created", 150);
                listViewArchive.Columns.Add("Path", 200);
            }

            var files = Directory.GetFiles(staticFolder, "*.cva");
            foreach (var file in files)
            {
                FileInfo info = new FileInfo(file);
                var item = new ListViewItem(info.Name);
                item.SubItems.Add(FormatFileSize(info.Length));
                item.SubItems.Add(info.CreationTime.ToString("g"));
                item.SubItems.Add(file); // Full path
                item.Tag = file; // Store full path in tag
                listViewArchive.Items.Add(item);
            }
        }

        #region Decompression Methods

        private void BtnBrowseArchive_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Compressed Archives (*.cva)|*.cva|All Files (*.*)|*.*";
                openFileDialog.Title = "Select Archive to Extract";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtArchivePath.Text = openFileDialog.FileName;
                    LoadArchiveInfo(openFileDialog.FileName);
                }
            }
        }

        private void BtnBrowseExtractPath_Click(object sender, EventArgs e)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select Extraction Directory";
                folderBrowserDialog.ShowNewFolderButton = true;

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    txtExtractPath.Text = folderBrowserDialog.SelectedPath;
                }
            }
        }

        private async void BtnExtract_Click(object sender, EventArgs e)
        {
            if (btnExtract.Text == "Extract Archive")
            {
                if (string.IsNullOrEmpty(txtArchivePath.Text) || !File.Exists(txtArchivePath.Text))
                {
                    MessageBox.Show("Please select a valid archive file.", "Invalid Archive", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(txtExtractPath.Text))
                {
                    MessageBox.Show("Please select an extraction directory.", "No Extraction Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                await StartDecompression();
            }
            else
            {
                CancelExtraction();
            }
        }

        private async Task StartDecompression()
        {
            try
            {
                // تخزين مسار مجلد الاستخراج الحالي
                _currentExtractionPath = txtExtractPath.Text;
                
                btnExtract.Text = "Cancel Extraction";
                btnExtract.Enabled = true;
                progressBarExtract.Value = 0;
                progressBarExtract.Style = ProgressBarStyle.Continuous;
                lblExtractStatus.Text = "Preparing extraction...";

                btnBrowseArchive.Enabled = false;
                btnBrowseExtractPath.Enabled = false;
                txtArchivePath.Enabled = false;
                txtExtractPath.Enabled = false;
                txtExtractPassword.Enabled = false;

                _cancellationTokenSource = new CancellationTokenSource();
                string password = string.IsNullOrEmpty(txtExtractPassword.Text) ? null : txtExtractPassword.Text;
                var progress = new Progress<DecompressionProgress>(UpdateDecompressionProgress);

                var decompressionInfo = new DecompressionInfo
                {
                    InputPath = txtArchivePath.Text,
                    OutputDirectory = txtExtractPath.Text,
                    Password = password,
                    AutoDetectAlgorithm = true
                };

                var result = await _decompressionManager.DecompressAsync(decompressionInfo, progress, _cancellationTokenSource.Token);

                if (result.Success)
                {
                    _lastDecompressionResult = result;
                    UpdateDecompressionStats(result);
                                    lblExtractStatus.Text = string.Format("Extraction completed successfully! Extracted: {0} files, Time: {1:mm\\:ss}", result.ExtractedFiles.Count, result.Duration);
                MessageBox.Show(string.Format("Extraction completed successfully!\n\nExtracted {0} files to:\n{1}", result.ExtractedFiles.Count, txtExtractPath.Text), "Extraction Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // حذف الملفات المستخرجة في حالة الفشل
                    if (!string.IsNullOrEmpty(_currentExtractionPath) && Directory.Exists(_currentExtractionPath))
                    {
                        try
                        {
                            CleanupExtractedFiles(_currentExtractionPath);
                        }
                        catch { /* تجاهل أخطاء الحذف */ }
                    }
                    
                    MessageBox.Show(string.Format("Extraction failed: {0}", result.ErrorMessage), "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Extraction was cancelled.", "Extraction Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // حذف الملفات المستخرجة في حالة حدوث خطأ
                if (!string.IsNullOrEmpty(_currentExtractionPath) && Directory.Exists(_currentExtractionPath))
                {
                    try
                    {
                        CleanupExtractedFiles(_currentExtractionPath);
                    }
                    catch { /* تجاهل أخطاء الحذف */ }
                }
                
                MessageBox.Show(string.Format("An error occurred: {0}", ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ResetDecompressionUI();
            }
        }

        private void UpdateDecompressionProgress(DecompressionProgress progress)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<DecompressionProgress>(UpdateDecompressionProgress), progress);
                return;
            }

            int clampedValue = Math.Max(progressBarExtract.Minimum, Math.Min(progressBarExtract.Maximum, (int)progress.Percentage));
            progressBarExtract.Value = clampedValue;
            lblExtractStatus.Text = progress.Status;
        }

        private void UpdateDecompressionStats(DecompressionResult result = null)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<DecompressionResult>(UpdateDecompressionStats), result);
                return;
            }

            if (result != null)
            {
                lblExtractedFiles.Text = string.Format("Extracted Files: {0}", result.ExtractedFiles.Count);
                lblExtractedSize.Text = string.Format("Extracted Size: {0}", FormatFileSize(result.DecompressedSize));
                lblExtractRatio.Text = string.Format("Decompression Ratio: {0:P2}", result.DecompressionRatio);
            }
            else
            {
                lblExtractedFiles.Text = "Extracted Files: 0";
                lblExtractedSize.Text = "Extracted Size: 0 MB";
                lblExtractRatio.Text = "Decompression Ratio: -";
            }
        }

        private void ResetDecompressionUI()
        {
            btnExtract.Text = "Extract Archive";
            btnExtract.Enabled = true;
            progressBarExtract.Value = 0;
            progressBarExtract.Style = ProgressBarStyle.Continuous;

            btnBrowseArchive.Enabled = true;
            btnBrowseExtractPath.Enabled = true;
            txtArchivePath.Enabled = true;
            txtExtractPath.Enabled = true;
            txtExtractPassword.Enabled = true;

            if (!_statusTimer.Enabled)
                lblExtractStatus.Text = "Ready to extract";

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            
            // مسح مسار الاستخراج الحالي
            _currentExtractionPath = null;
        }

        private async void LoadArchiveInfo(string filePath)
        {
            try
            {
                var fileInfo = await _decompressionManager.GetCompressedFileInfoAsync(filePath);
                if (fileInfo != null && string.IsNullOrEmpty(fileInfo.ErrorMessage))
                {
                                    lblArchiveAlgorithm.Text = string.Format("Algorithm: {0}", fileInfo.Algorithm);
                lblArchiveSize.Text = string.Format("Archive Size: {0}", FormatFileSize(fileInfo.CompressedSize));
                lblArchiveItems.Text = string.Format("Items: {0}", fileInfo.ItemCount);
                lblArchivePassword.Text = string.Format("Password Protected: {0}", fileInfo.HasPassword ? "Yes" : "No");
                    
                    // Enable/disable password field
                    txtExtractPassword.Enabled = fileInfo.HasPassword;
                    if (!fileInfo.HasPassword)
                    {
                        txtExtractPassword.Text = string.Empty;
                    }
                }
                else
                {
                    lblArchiveAlgorithm.Text = "Algorithm: Unknown";
                    lblArchiveSize.Text = "Archive Size: Unknown";
                    lblArchiveItems.Text = "Items: Unknown";
                    lblArchivePassword.Text = "Password Protected: Unknown";
                    txtExtractPassword.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                lblArchiveAlgorithm.Text = "Algorithm: Error";
                lblArchiveSize.Text = "Archive Size: Error";
                lblArchiveItems.Text = "Items: Error";
                lblArchivePassword.Text = "Password Protected: Error";
                txtExtractPassword.Enabled = false;
            }
        }

        private void ListViewArchive_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewArchive.SelectedItems.Count > 0)
            {
                var selectedItem = listViewArchive.SelectedItems[0];
                string filePath = selectedItem.Tag as string;
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    txtArchivePath.Text = filePath;
                    LoadArchiveInfo(filePath);
                }
            }
        }

        private async void ExtractSingleFile()
        {
            if (string.IsNullOrEmpty(txtArchivePath.Text) || !File.Exists(txtArchivePath.Text))
            {
                MessageBox.Show("Please select a valid archive file.", "Invalid Archive", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(txtExtractPath.Text))
            {
                MessageBox.Show("Please select an extraction directory.", "No Extraction Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Prompt user for file name to extract
            string fileNameToExtract = PromptForFileNameToExtract();
            if (string.IsNullOrWhiteSpace(fileNameToExtract))
            {
                MessageBox.Show("No file name provided for extraction.", "No File Name", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            await ExtractSingleFileAsync(fileNameToExtract);
        }

        private async Task ExtractSingleFileAsync(string fileName)
        {
            try
            {
                // تخزين مسار مجلد الاستخراج الحالي
                _currentExtractionPath = txtExtractPath.Text;
                
                btnExtract.Text = "Extracting...";
                btnExtract.Enabled = false;
                progressBarExtract.Value = 0;
                progressBarExtract.Style = ProgressBarStyle.Continuous;
                lblExtractStatus.Text = "Extracting single file...";

                btnBrowseArchive.Enabled = false;
                btnBrowseExtractPath.Enabled = false;
                txtArchivePath.Enabled = false;
                txtExtractPath.Enabled = false;
                txtExtractPassword.Enabled = false;

                _cancellationTokenSource = new CancellationTokenSource();
                string password = string.IsNullOrEmpty(txtExtractPassword.Text) ? null : txtExtractPassword.Text;
                var progress = new Progress<DecompressionProgress>(UpdateDecompressionProgress);

                var result = await _decompressionService.ExtractSingleFileAsync(
                    txtArchivePath.Text,
                    fileName,
                    txtExtractPath.Text,
                    password,
                    progress,
                    _cancellationTokenSource.Token);

                if (result.Success)
                {
                    _lastDecompressionResult = result;
                    UpdateDecompressionStats(result);
                    lblExtractStatus.Text = string.Format("Single file extraction completed! Extracted: {0}", fileName);
                    MessageBox.Show(string.Format("File '{0}' extracted successfully to:\n{1}", fileName, txtExtractPath.Text), "Extraction Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // حذف الملفات المستخرجة في حالة الفشل
                    if (!string.IsNullOrEmpty(_currentExtractionPath) && Directory.Exists(_currentExtractionPath))
                    {
                        try
                        {
                            CleanupExtractedFiles(_currentExtractionPath);
                        }
                        catch { /* تجاهل أخطاء الحذف */ }
                    }
                    
                    MessageBox.Show(string.Format("Single file extraction failed: {0}", result.ErrorMessage), "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Single file extraction was cancelled.", "Extraction Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // حذف الملفات المستخرجة في حالة حدوث خطأ
                if (!string.IsNullOrEmpty(_currentExtractionPath) && Directory.Exists(_currentExtractionPath))
                {
                    try
                    {
                        CleanupExtractedFiles(_currentExtractionPath);
                    }
                    catch { /* تجاهل أخطاء الحذف */ }
                }
                
                MessageBox.Show(string.Format("An error occurred: {0}", ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ResetDecompressionUI();
            }
        }

        private string PromptForFileNameToExtract()
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 400;
                prompt.Height = 150;
                prompt.Text = "Extract Single File";

                Label textLabel = new Label() { Left = 20, Top = 20, Text = "Enter file name to extract:", Width = 340 };
                TextBox inputBox = new TextBox() { Left = 20, Top = 50, Width = 340 };

                Button confirmation = new Button() { Text = "OK", Left = 270, Width = 90, Top = 80, DialogResult = DialogResult.OK };
                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(inputBox);
                prompt.Controls.Add(confirmation);
                prompt.AcceptButton = confirmation;

                return prompt.ShowDialog() == DialogResult.OK ? inputBox.Text.Trim() : null;
            }
        }

        #endregion

        private string PromptForArchiveName()
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 400;
                prompt.Height = 150;
                prompt.Text = "Name Your Archive";

                Label textLabel = new Label() { Left = 20, Top = 20, Text = "Enter archive name:", Width = 340 };
                TextBox inputBox = new TextBox() { Left = 20, Top = 50, Width = 340 };

                Button confirmation = new Button() { Text = "OK", Left = 270, Width = 90, Top = 80, DialogResult = DialogResult.OK };
                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(inputBox);
                prompt.Controls.Add(confirmation);
                prompt.AcceptButton = confirmation;

                return prompt.ShowDialog() == DialogResult.OK ? inputBox.Text.Trim() : null;
            }
        }

    }
}
