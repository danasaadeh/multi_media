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
        private FlowLayoutPanel _flowPanelFiles;
        private CancellationTokenSource _cancellationTokenSource;
        private System.Windows.Forms.Timer _statusTimer;
        private CompressionResult _lastCompressionResult;

        public Form1()
        {
            InitializeComponent();
            tabControl.SelectedIndexChanged += (s, e) =>
            {
                if (tabControl.SelectedTab == tabExtract)
                    LoadCompressedArchives();
            };

           


            _fileSelectionService = new FileSelectionService();
            _summaryService = new CompressionSummaryService();
            _itemManager = new CompressionItemManager();
            _controlFactory = new ItemControlFactory();
            _compressionService = new CompressionService();

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
        }

        private void WireUpEvents()
        {
            btnAddFiles.Click += BtnAddFiles_Click;
            btnAddFolder.Click += BtnAddFolder_Click;
            btnStart.Click += BtnStart_Click;
            _itemManager.ItemsChanged += OnItemsChanged;
            togglePassword.CheckedChanged += TogglePassword_CheckedChanged;
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
                    lblStatus.Text = $"Compression completed successfully! Original: {FormatFileSize(result.OriginalSize)}, Compressed: {FormatFileSize(result.CompressedSize)}, Ratio: {ratio:P2}, Time: {result.Duration:mm\\:ss}";
                    _statusTimer.Start();
                    LoadCompressedArchives(); // Refresh list after new file saved

                }

                else
                {
                    MessageBox.Show($"Compression failed: {result.ErrorMessage}", "Compression Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Compression was cancelled.", "Compression Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                lblOriginalSize.Text = $"Original Size: {FormatFileSize(result.OriginalSize)}";
                lblCompressedSize.Text = $"Compressed Size: {FormatFileSize(result.CompressedSize)}";
                lblRatio.Text = $"Compression Ratio: {result.CompressionRatio:P2}";
                lblSaved.Text = $"Space Saved: {FormatFileSize(result.OriginalSize - result.CompressedSize)}";
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

            lblTotalFiles.Text = $"Total Files: {summary.TotalFiles}";
            lblTotalSize.Text = $"Total Size: {summary.FormattedTotalSize}";
            lblEstimated.Text = $"Estimated Compression: ~{summary.FormattedEstimatedSize}";
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
            return $"{len:0.##} {sizes[order]}";
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

            var files = Directory.GetFiles(staticFolder, "*.cva");
            foreach (var file in files)
            {
                FileInfo info = new FileInfo(file);
                var item = new ListViewItem(info.Name);
                item.SubItems.Add(info.Length.ToString("N0") + " bytes");
                item.SubItems.Add(info.CreationTime.ToString("g"));
                item.Tag = file; // optional: store full path
                listViewArchive.Items.Add(item);
            }
        }


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
