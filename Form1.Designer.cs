using System.Windows.Forms;
using System.Drawing;

namespace Compression_Vault
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                _statusTimer?.Dispose();
                _cancellationTokenSource?.Dispose();
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabCompress = new System.Windows.Forms.TabPage();
            this.btnAddFiles = new System.Windows.Forms.Button();
            this.btnAddFolder = new System.Windows.Forms.Button();
            this.listBoxFiles = new System.Windows.Forms.ListBox();
            this.groupSummary = new System.Windows.Forms.GroupBox();
            this.lblTotalFiles = new System.Windows.Forms.Label();
            this.lblTotalSize = new System.Windows.Forms.Label();
            this.lblEstimated = new System.Windows.Forms.Label();
            this.groupSettings = new System.Windows.Forms.GroupBox();
            this.radioHuffman = new System.Windows.Forms.RadioButton();
            this.radioShannon = new System.Windows.Forms.RadioButton();
            this.togglePassword = new System.Windows.Forms.CheckBox();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.groupStats = new System.Windows.Forms.GroupBox();
            this.lblOriginalSize = new System.Windows.Forms.Label();
            this.lblCompressedSize = new System.Windows.Forms.Label();
            this.lblRatio = new System.Windows.Forms.Label();
            this.lblSaved = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.tabExtract = new System.Windows.Forms.TabPage();
            this.lblArchiveManager = new System.Windows.Forms.Label();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnExtractAll = new System.Windows.Forms.Button();
            this.listViewArchive = new System.Windows.Forms.ListView();
            this.btnBrowseArchive = new System.Windows.Forms.Button();
            this.txtArchivePath = new System.Windows.Forms.TextBox();
            this.btnBrowseExtractPath = new System.Windows.Forms.Button();
            this.txtExtractPath = new System.Windows.Forms.TextBox();
            this.txtExtractPassword = new System.Windows.Forms.TextBox();
            this.btnExtract = new System.Windows.Forms.Button();
            this.progressBarExtract = new System.Windows.Forms.ProgressBar();
            this.lblExtractStatus = new System.Windows.Forms.Label();
            this.lblArchiveAlgorithm = new System.Windows.Forms.Label();
            this.lblArchiveSize = new System.Windows.Forms.Label();
            this.lblArchiveItems = new System.Windows.Forms.Label();
            this.lblArchivePassword = new System.Windows.Forms.Label();
            this.lblExtractedFiles = new System.Windows.Forms.Label();
            this.lblExtractedSize = new System.Windows.Forms.Label();
            this.lblExtractRatio = new System.Windows.Forms.Label();
            this.lblPassword = new System.Windows.Forms.Label();
            this.tabControl.SuspendLayout();
            this.tabCompress.SuspendLayout();
            this.groupSummary.SuspendLayout();
            this.groupSettings.SuspendLayout();
            this.groupStats.SuspendLayout();
            this.tabExtract.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabCompress);
            this.tabControl.Controls.Add(this.tabExtract);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Top;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(639, 550);
            this.tabControl.TabIndex = 0;
            // 
            // tabCompress
            // 
            this.tabCompress.Controls.Add(this.btnAddFiles);
            this.tabCompress.Controls.Add(this.btnAddFolder);
            this.tabCompress.Controls.Add(this.listBoxFiles);
            this.tabCompress.Controls.Add(this.groupSummary);
            this.tabCompress.Controls.Add(this.groupSettings);
            this.tabCompress.Controls.Add(this.groupStats);
            this.tabCompress.Controls.Add(this.progressBar);
            this.tabCompress.Controls.Add(this.lblStatus);
            this.tabCompress.Controls.Add(this.btnStart);
            this.tabCompress.Location = new System.Drawing.Point(4, 22);
            this.tabCompress.Name = "tabCompress";
            this.tabCompress.Size = new System.Drawing.Size(631, 524);
            this.tabCompress.TabIndex = 0;
            this.tabCompress.Text = "Compress";
            // 
            // btnAddFiles
            // 
            this.btnAddFiles.Location = new System.Drawing.Point(20, 20);
            this.btnAddFiles.Name = "btnAddFiles";
            this.btnAddFiles.Size = new System.Drawing.Size(75, 23);
            this.btnAddFiles.TabIndex = 0;
            this.btnAddFiles.Text = "Add Files";
            // 
            // btnAddFolder
            // 
            this.btnAddFolder.Location = new System.Drawing.Point(120, 20);
            this.btnAddFolder.Name = "btnAddFolder";
            this.btnAddFolder.Size = new System.Drawing.Size(75, 23);
            this.btnAddFolder.TabIndex = 1;
            this.btnAddFolder.Text = "Add Folder";
            // 
            // listBoxFiles
            // 
            this.listBoxFiles.Location = new System.Drawing.Point(20, 60);
            this.listBoxFiles.Name = "listBoxFiles";
            this.listBoxFiles.Size = new System.Drawing.Size(300, 199);
            this.listBoxFiles.TabIndex = 2;
            // 
            // groupSummary
            // 
            this.groupSummary.Controls.Add(this.lblTotalFiles);
            this.groupSummary.Controls.Add(this.lblTotalSize);
            this.groupSummary.Controls.Add(this.lblEstimated);
            this.groupSummary.Location = new System.Drawing.Point(350, 20);
            this.groupSummary.Name = "groupSummary";
            this.groupSummary.Size = new System.Drawing.Size(250, 120);
            this.groupSummary.TabIndex = 3;
            this.groupSummary.TabStop = false;
            this.groupSummary.Text = "Selection Summary";
            // 
            // lblTotalFiles
            // 
            this.lblTotalFiles.Location = new System.Drawing.Point(10, 20);
            this.lblTotalFiles.Name = "lblTotalFiles";
            this.lblTotalFiles.Size = new System.Drawing.Size(200, 23);
            this.lblTotalFiles.TabIndex = 0;
            this.lblTotalFiles.Text = "Total Files: 0";
            // 
            // lblTotalSize
            // 
            this.lblTotalSize.Location = new System.Drawing.Point(10, 45);
            this.lblTotalSize.Name = "lblTotalSize";
            this.lblTotalSize.Size = new System.Drawing.Size(200, 23);
            this.lblTotalSize.TabIndex = 1;
            this.lblTotalSize.Text = "Total Size: 0 MB";
            // 
            // lblEstimated
            // 
            this.lblEstimated.Location = new System.Drawing.Point(10, 70);
            this.lblEstimated.Name = "lblEstimated";
            this.lblEstimated.Size = new System.Drawing.Size(200, 23);
            this.lblEstimated.TabIndex = 2;
            this.lblEstimated.Text = "Estimated Compression: ~0 MB";
            // 
            // groupSettings
            // 
            this.groupSettings.Controls.Add(this.radioHuffman);
            this.groupSettings.Controls.Add(this.radioShannon);
            this.groupSettings.Controls.Add(this.togglePassword);
            this.groupSettings.Controls.Add(this.txtPassword);
            this.groupSettings.Location = new System.Drawing.Point(20, 280);
            this.groupSettings.Name = "groupSettings";
            this.groupSettings.Size = new System.Drawing.Size(300, 120);
            this.groupSettings.TabIndex = 4;
            this.groupSettings.TabStop = false;
            this.groupSettings.Text = "Compression Settings";
            // 
            // radioHuffman
            // 
            this.radioHuffman.Checked = true;
            this.radioHuffman.Location = new System.Drawing.Point(10, 20);
            this.radioHuffman.Name = "radioHuffman";
            this.radioHuffman.Size = new System.Drawing.Size(130, 24);
            this.radioHuffman.TabIndex = 0;
            this.radioHuffman.TabStop = true;
            this.radioHuffman.Text = "Huffman Coding";
            // 
            // radioShannon
            // 
            this.radioShannon.Location = new System.Drawing.Point(150, 20);
            this.radioShannon.Name = "radioShannon";
            this.radioShannon.Size = new System.Drawing.Size(120, 24);
            this.radioShannon.TabIndex = 1;
            this.radioShannon.Text = "Shannon-Fano";
            // 
            // togglePassword
            // 
            this.togglePassword.Location = new System.Drawing.Point(10, 50);
            this.togglePassword.Name = "togglePassword";
            this.togglePassword.Size = new System.Drawing.Size(150, 24);
            this.togglePassword.TabIndex = 2;
            this.togglePassword.Text = "Password Protection";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(10, 75);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(250, 20);
            this.txtPassword.TabIndex = 3;
            // 
            // groupStats
            // 
            this.groupStats.Controls.Add(this.lblOriginalSize);
            this.groupStats.Controls.Add(this.lblCompressedSize);
            this.groupStats.Controls.Add(this.lblRatio);
            this.groupStats.Controls.Add(this.lblSaved);
            this.groupStats.Location = new System.Drawing.Point(350, 160);
            this.groupStats.Name = "groupStats";
            this.groupStats.Size = new System.Drawing.Size(250, 120);
            this.groupStats.TabIndex = 5;
            this.groupStats.TabStop = false;
            this.groupStats.Text = "Compression Statistics";
            // 
            // lblOriginalSize
            // 
            this.lblOriginalSize.Location = new System.Drawing.Point(10, 20);
            this.lblOriginalSize.Name = "lblOriginalSize";
            this.lblOriginalSize.Size = new System.Drawing.Size(200, 23);
            this.lblOriginalSize.TabIndex = 0;
            this.lblOriginalSize.Text = "Original Size: 0 MB";
            // 
            // lblCompressedSize
            // 
            this.lblCompressedSize.Location = new System.Drawing.Point(10, 45);
            this.lblCompressedSize.Name = "lblCompressedSize";
            this.lblCompressedSize.Size = new System.Drawing.Size(200, 23);
            this.lblCompressedSize.TabIndex = 1;
            this.lblCompressedSize.Text = "Compressed Size: 0 MB";
            // 
            // lblRatio
            // 
            this.lblRatio.Location = new System.Drawing.Point(10, 70);
            this.lblRatio.Name = "lblRatio";
            this.lblRatio.Size = new System.Drawing.Size(200, 23);
            this.lblRatio.TabIndex = 2;
            this.lblRatio.Text = "Compression Ratio: -";
            // 
            // lblSaved
            // 
            this.lblSaved.Location = new System.Drawing.Point(10, 95);
            this.lblSaved.Name = "lblSaved";
            this.lblSaved.Size = new System.Drawing.Size(200, 23);
            this.lblSaved.TabIndex = 3;
            this.lblSaved.Text = "Space Saved: -";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(20, 420);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(580, 23);
            this.progressBar.TabIndex = 6;
            // 
            // lblStatus
            // 
            this.lblStatus.Location = new System.Drawing.Point(20, 446);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(300, 13);
            this.lblStatus.TabIndex = 7;
            this.lblStatus.Text = "Ready to compress";
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(20, 460);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(580, 40);
            this.btnStart.TabIndex = 8;
            this.btnStart.Text = "Start Compression";
            // 
            // tabExtract
            // 
            this.tabExtract.Controls.Add(this.lblArchiveManager);
            this.tabExtract.Controls.Add(this.txtSearch);
            this.tabExtract.Controls.Add(this.btnExtractAll);
            this.tabExtract.Controls.Add(this.listViewArchive);
            this.tabExtract.Controls.Add(this.btnBrowseArchive);
            this.tabExtract.Controls.Add(this.txtArchivePath);
            this.tabExtract.Controls.Add(this.btnBrowseExtractPath);
            this.tabExtract.Controls.Add(this.txtExtractPath);
            this.tabExtract.Controls.Add(this.txtExtractPassword);
            this.tabExtract.Controls.Add(this.btnExtract);
            this.tabExtract.Controls.Add(this.progressBarExtract);
            this.tabExtract.Controls.Add(this.lblExtractStatus);
            this.tabExtract.Controls.Add(this.lblArchiveAlgorithm);
            this.tabExtract.Controls.Add(this.lblArchiveSize);
            this.tabExtract.Controls.Add(this.lblArchiveItems);
            this.tabExtract.Controls.Add(this.lblArchivePassword);
            this.tabExtract.Controls.Add(this.lblExtractedFiles);
            this.tabExtract.Controls.Add(this.lblExtractedSize);
            this.tabExtract.Controls.Add(this.lblExtractRatio);
            this.tabExtract.Controls.Add(this.lblPassword);
            this.tabExtract.Location = new System.Drawing.Point(4, 22);
            this.tabExtract.Name = "tabExtract";
            this.tabExtract.Size = new System.Drawing.Size(631, 524);
            this.tabExtract.TabIndex = 1;
            this.tabExtract.Text = "Extract";
            // 
            // lblArchiveManager
            // 
            this.lblArchiveManager.AutoSize = true;
            this.lblArchiveManager.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblArchiveManager.Location = new System.Drawing.Point(15, 15);
            this.lblArchiveManager.Name = "lblArchiveManager";
            this.lblArchiveManager.Size = new System.Drawing.Size(140, 21);
            this.lblArchiveManager.TabIndex = 0;
            this.lblArchiveManager.Text = "Archive Manager";
            // 
            // txtSearch
            // 
            this.txtSearch.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.txtSearch.Location = new System.Drawing.Point(15, 45);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(400, 21);
            this.txtSearch.TabIndex = 1;
            this.txtSearch.Text = "Search files in archive...";
            // 
            // btnExtractAll
            // 
            this.btnExtractAll.Location = new System.Drawing.Point(425, 44);
            this.btnExtractAll.Name = "btnExtractAll";
            this.btnExtractAll.Size = new System.Drawing.Size(80, 23);
            this.btnExtractAll.TabIndex = 2;
            this.btnExtractAll.Text = "Extract All";
            // 
            // listViewArchive
            // 
            this.listViewArchive.FullRowSelect = true;
            this.listViewArchive.HideSelection = false;
            this.listViewArchive.Location = new System.Drawing.Point(15, 75);
            this.listViewArchive.Name = "listViewArchive";
            this.listViewArchive.Size = new System.Drawing.Size(600, 200);
            this.listViewArchive.TabIndex = 3;
            this.listViewArchive.UseCompatibleStateImageBehavior = false;
            this.listViewArchive.View = System.Windows.Forms.View.Details;
            // 
            // btnBrowseArchive
            // 
            this.btnBrowseArchive.Location = new System.Drawing.Point(15, 290);
            this.btnBrowseArchive.Name = "btnBrowseArchive";
            this.btnBrowseArchive.Size = new System.Drawing.Size(100, 23);
            this.btnBrowseArchive.TabIndex = 5;
            this.btnBrowseArchive.Text = "Browse Archive";
            // 
            // txtArchivePath
            // 
            this.txtArchivePath.Location = new System.Drawing.Point(125, 290);
            this.txtArchivePath.Name = "txtArchivePath";
            this.txtArchivePath.Size = new System.Drawing.Size(300, 20);
            this.txtArchivePath.TabIndex = 6;
            // 
            // btnBrowseExtractPath
            // 
            this.btnBrowseExtractPath.Location = new System.Drawing.Point(15, 320);
            this.btnBrowseExtractPath.Name = "btnBrowseExtractPath";
            this.btnBrowseExtractPath.Size = new System.Drawing.Size(100, 23);
            this.btnBrowseExtractPath.TabIndex = 7;
            this.btnBrowseExtractPath.Text = "Extract To";
            // 
            // txtExtractPath
            // 
            this.txtExtractPath.Location = new System.Drawing.Point(125, 320);
            this.txtExtractPath.Name = "txtExtractPath";
            this.txtExtractPath.Size = new System.Drawing.Size(300, 20);
            this.txtExtractPath.TabIndex = 8;
            // 
            // txtExtractPassword
            // 
            this.txtExtractPassword.Location = new System.Drawing.Point(125, 350);
            this.txtExtractPassword.Name = "txtExtractPassword";
            this.txtExtractPassword.PasswordChar = '*';
            this.txtExtractPassword.Size = new System.Drawing.Size(200, 20);
            this.txtExtractPassword.TabIndex = 9;
            // 
            // btnExtract
            // 
            this.btnExtract.Location = new System.Drawing.Point(15, 380);
            this.btnExtract.Name = "btnExtract";
            this.btnExtract.Size = new System.Drawing.Size(100, 30);
            this.btnExtract.TabIndex = 10;
            this.btnExtract.Text = "Extract Archive";
            // 
            // progressBarExtract
            // 
            this.progressBarExtract.Location = new System.Drawing.Point(125, 380);
            this.progressBarExtract.Name = "progressBarExtract";
            this.progressBarExtract.Size = new System.Drawing.Size(300, 30);
            this.progressBarExtract.TabIndex = 11;
            // 
            // lblExtractStatus
            // 
            this.lblExtractStatus.Location = new System.Drawing.Point(15, 420);
            this.lblExtractStatus.Name = "lblExtractStatus";
            this.lblExtractStatus.Size = new System.Drawing.Size(400, 20);
            this.lblExtractStatus.TabIndex = 12;
            this.lblExtractStatus.Text = "Ready to extract";
            // 
            // lblArchiveAlgorithm
            // 
            this.lblArchiveAlgorithm.Location = new System.Drawing.Point(440, 290);
            this.lblArchiveAlgorithm.Name = "lblArchiveAlgorithm";
            this.lblArchiveAlgorithm.Size = new System.Drawing.Size(180, 20);
            this.lblArchiveAlgorithm.TabIndex = 13;
            this.lblArchiveAlgorithm.Text = "Algorithm: Unknown";
            // 
            // lblArchiveSize
            // 
            this.lblArchiveSize.Location = new System.Drawing.Point(440, 310);
            this.lblArchiveSize.Name = "lblArchiveSize";
            this.lblArchiveSize.Size = new System.Drawing.Size(180, 20);
            this.lblArchiveSize.TabIndex = 14;
            this.lblArchiveSize.Text = "Archive Size: Unknown";
            // 
            // lblArchiveItems
            // 
            this.lblArchiveItems.Location = new System.Drawing.Point(440, 330);
            this.lblArchiveItems.Name = "lblArchiveItems";
            this.lblArchiveItems.Size = new System.Drawing.Size(180, 20);
            this.lblArchiveItems.TabIndex = 15;
            this.lblArchiveItems.Text = "Items: Unknown";
            // 
            // lblArchivePassword
            // 
            this.lblArchivePassword.Location = new System.Drawing.Point(440, 350);
            this.lblArchivePassword.Name = "lblArchivePassword";
            this.lblArchivePassword.Size = new System.Drawing.Size(180, 20);
            this.lblArchivePassword.TabIndex = 16;
            this.lblArchivePassword.Text = "Password Protected: Unknown";
            // 
            // lblExtractedFiles
            // 
            this.lblExtractedFiles.Location = new System.Drawing.Point(15, 450);
            this.lblExtractedFiles.Name = "lblExtractedFiles";
            this.lblExtractedFiles.Size = new System.Drawing.Size(200, 20);
            this.lblExtractedFiles.TabIndex = 17;
            this.lblExtractedFiles.Text = "Extracted Files: 0";
            // 
            // lblExtractedSize
            // 
            this.lblExtractedSize.Location = new System.Drawing.Point(220, 450);
            this.lblExtractedSize.Name = "lblExtractedSize";
            this.lblExtractedSize.Size = new System.Drawing.Size(200, 20);
            this.lblExtractedSize.TabIndex = 18;
            this.lblExtractedSize.Text = "Extracted Size: 0 MB";
            // 
            // lblExtractRatio
            // 
            this.lblExtractRatio.Location = new System.Drawing.Point(425, 450);
            this.lblExtractRatio.Name = "lblExtractRatio";
            this.lblExtractRatio.Size = new System.Drawing.Size(200, 20);
            this.lblExtractRatio.TabIndex = 19;
            this.lblExtractRatio.Text = "Decompression Ratio: -";
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(15, 353);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(56, 13);
            this.lblPassword.TabIndex = 20;
            this.lblPassword.Text = "Password:";
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(639, 532);
            this.Controls.Add(this.tabControl);
            this.Name = "Form1";
            this.Text = "AeroCompress Vault";
            this.tabControl.ResumeLayout(false);
            this.tabCompress.ResumeLayout(false);
            this.groupSummary.ResumeLayout(false);
            this.groupSettings.ResumeLayout(false);
            this.groupSettings.PerformLayout();
            this.groupStats.ResumeLayout(false);
            this.tabExtract.ResumeLayout(false);
            this.tabExtract.PerformLayout();
            this.ResumeLayout(false);

        }

        // Control declarations
        private TabControl tabControl;
        private TabPage tabCompress, tabExtract;

        private Button btnAddFiles, btnAddFolder, btnStart, btnExtractAll;
        private ListBox listBoxFiles;

        private GroupBox groupSummary, groupSettings, groupStats;
        private Label lblTotalFiles, lblTotalSize, lblEstimated;
        private RadioButton radioHuffman, radioShannon;
        private CheckBox togglePassword;
        private TextBox txtPassword;

        private Label lblOriginalSize, lblCompressedSize, lblRatio, lblSaved;
        private ProgressBar progressBar;
        private Label lblStatus;

        private Label lblArchiveManager;
        private TextBox txtSearch;
        private ListView listViewArchive;

        // Decompression controls
        private Button btnBrowseArchive, btnBrowseExtractPath, btnExtract;
        private TextBox txtArchivePath, txtExtractPath, txtExtractPassword;
        private ProgressBar progressBarExtract;
        private Label lblExtractStatus, lblArchiveAlgorithm, lblArchiveSize, lblArchiveItems, lblArchivePassword;
        private Label lblExtractedFiles, lblExtractedSize, lblExtractRatio;
        private Label lblPassword;
    }
}
