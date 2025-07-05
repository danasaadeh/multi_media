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
            this.tabControl = new TabControl();
            this.tabCompress = new TabPage();
            this.tabExtract = new TabPage();

            this.btnAddFiles = new Button();
            this.btnAddFolder = new Button();
            this.listBoxFiles = new ListBox();

            this.groupSummary = new GroupBox();
            this.lblTotalFiles = new Label();
            this.lblTotalSize = new Label();
            this.lblEstimated = new Label();

            this.groupSettings = new GroupBox();
            this.radioHuffman = new RadioButton();
            this.radioShannon = new RadioButton();
            this.togglePassword = new CheckBox();
            this.txtPassword = new TextBox();

            this.groupStats = new GroupBox();
            this.lblOriginalSize = new Label();
            this.lblCompressedSize = new Label();
            this.lblRatio = new Label();
            this.lblSaved = new Label();

            this.progressBar = new ProgressBar();
            this.lblStatus = new Label();
            this.btnStart = new Button();

            this.lblArchiveManager = new Label();
            this.txtSearch = new TextBox();
            this.btnExtractAll = new Button();
            this.listViewArchive = new ListView();

            this.tabControl.SuspendLayout();
            this.tabCompress.SuspendLayout();
            this.tabExtract.SuspendLayout();
            this.groupSummary.SuspendLayout();
            this.groupSettings.SuspendLayout();
            this.groupStats.SuspendLayout();
            this.SuspendLayout();

            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabCompress);
            this.tabControl.Controls.Add(this.tabExtract);
            this.tabControl.Dock = DockStyle.Top;
            this.tabControl.Location = new Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new Size(639, 550);
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
            this.tabCompress.Location = new Point(4, 22);
            this.tabCompress.Name = "tabCompress";
            this.tabCompress.Size = new Size(631, 524);
            this.tabCompress.TabIndex = 0;
            this.tabCompress.Text = "Compress";

            // 
            // btnAddFiles
            // 
            this.btnAddFiles.Location = new Point(20, 20);
            this.btnAddFiles.Name = "btnAddFiles";
            this.btnAddFiles.Size = new Size(75, 23);
            this.btnAddFiles.TabIndex = 0;
            this.btnAddFiles.Text = "Add Files";

            // 
            // btnAddFolder
            // 
            this.btnAddFolder.Location = new Point(120, 20);
            this.btnAddFolder.Name = "btnAddFolder";
            this.btnAddFolder.Size = new Size(75, 23);
            this.btnAddFolder.TabIndex = 1;
            this.btnAddFolder.Text = "Add Folder";

            // 
            // listBoxFiles
            // 
            this.listBoxFiles.Location = new Point(20, 60);
            this.listBoxFiles.Name = "listBoxFiles";
            this.listBoxFiles.Size = new Size(300, 199);
            this.listBoxFiles.TabIndex = 2;

            // 
            // groupSummary
            // 
            this.groupSummary.Controls.Add(this.lblTotalFiles);
            this.groupSummary.Controls.Add(this.lblTotalSize);
            this.groupSummary.Controls.Add(this.lblEstimated);
            this.groupSummary.Location = new Point(350, 20);
            this.groupSummary.Name = "groupSummary";
            this.groupSummary.Size = new Size(250, 120);
            this.groupSummary.TabIndex = 3;
            this.groupSummary.TabStop = false;
            this.groupSummary.Text = "Selection Summary";

            // 
            // lblTotalFiles
            // 
            this.lblTotalFiles.Location = new Point(10, 20);
            this.lblTotalFiles.Name = "lblTotalFiles";
            this.lblTotalFiles.Size = new Size(200, 23);
            this.lblTotalFiles.Text = "Total Files: 0";

            // 
            // lblTotalSize
            // 
            this.lblTotalSize.Location = new Point(10, 45);
            this.lblTotalSize.Name = "lblTotalSize";
            this.lblTotalSize.Size = new Size(200, 23);
            this.lblTotalSize.Text = "Total Size: 0 MB";

            // 
            // lblEstimated
            // 
            this.lblEstimated.Location = new Point(10, 70);
            this.lblEstimated.Name = "lblEstimated";
            this.lblEstimated.Size = new Size(200, 23);
            this.lblEstimated.Text = "Estimated Compression: ~0 MB";

            // 
            // groupSettings
            // 
            this.groupSettings.Controls.Add(this.radioHuffman);
            this.groupSettings.Controls.Add(this.radioShannon);
            this.groupSettings.Controls.Add(this.togglePassword);
            this.groupSettings.Controls.Add(this.txtPassword);
            this.groupSettings.Location = new Point(20, 280);
            this.groupSettings.Name = "groupSettings";
            this.groupSettings.Size = new Size(300, 120);
            this.groupSettings.TabIndex = 4;
            this.groupSettings.TabStop = false;
            this.groupSettings.Text = "Compression Settings";

            // 
            // radioHuffman
            // 
            this.radioHuffman.Checked = true;
            this.radioHuffman.Location = new Point(10, 20);
            this.radioHuffman.Name = "radioHuffman";
            this.radioHuffman.Size = new Size(130, 24);
            this.radioHuffman.TabIndex = 0;
            this.radioHuffman.TabStop = true;
            this.radioHuffman.Text = "Huffman Coding";

            // 
            // radioShannon
            // 
            this.radioShannon.Location = new Point(150, 20);
            this.radioShannon.Name = "radioShannon";
            this.radioShannon.Size = new Size(120, 24);
            this.radioShannon.TabIndex = 1;
            this.radioShannon.Text = "Shannon-Fano";

            // 
            // togglePassword
            // 
            this.togglePassword.Location = new Point(10, 50);
            this.togglePassword.Name = "togglePassword";
            this.togglePassword.Size = new Size(150, 24);
            this.togglePassword.Text = "Password Protection";

            // 
            // txtPassword
            // 
            this.txtPassword.Location = new Point(10, 75);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new Size(250, 20);

            // 
            // groupStats
            // 
            this.groupStats.Controls.Add(this.lblOriginalSize);
            this.groupStats.Controls.Add(this.lblCompressedSize);
            this.groupStats.Controls.Add(this.lblRatio);
            this.groupStats.Controls.Add(this.lblSaved);
            this.groupStats.Location = new Point(350, 160);
            this.groupStats.Name = "groupStats";
            this.groupStats.Size = new Size(250, 120);
            this.groupStats.TabIndex = 5;
            this.groupStats.TabStop = false;
            this.groupStats.Text = "Compression Statistics";

            // 
            // lblOriginalSize
            // 
            this.lblOriginalSize.Location = new Point(10, 20);
            this.lblOriginalSize.Size = new Size(200, 23);
            this.lblOriginalSize.Text = "Original Size: 0 MB";

            // 
            // lblCompressedSize
            // 
            this.lblCompressedSize.Location = new Point(10, 45);
            this.lblCompressedSize.Size = new Size(200, 23);
            this.lblCompressedSize.Text = "Compressed Size: 0 MB";

            // 
            // lblRatio
            // 
            this.lblRatio.Location = new Point(10, 70);
            this.lblRatio.Size = new Size(200, 23);
            this.lblRatio.Text = "Compression Ratio: -";

            // 
            // lblSaved
            // 
            this.lblSaved.Location = new Point(10, 95);
            this.lblSaved.Size = new Size(200, 23);
            this.lblSaved.Text = "Space Saved: -";

            // 
            // progressBar
            // 
            this.progressBar.Location = new Point(20, 420);
            this.progressBar.Size = new Size(580, 23);

            // 
            // lblStatus
            // 
            this.lblStatus.Location = new Point(20, 446);
            this.lblStatus.Size = new Size(300, 13);
            this.lblStatus.Text = "Ready to compress";

            // 
            // btnStart
            // 
            this.btnStart.Location = new Point(20, 460);
            this.btnStart.Size = new Size(580, 40);
            this.btnStart.Text = "Start Compression";

            // 
            // tabExtract
            // 
            this.tabExtract.Controls.Add(this.lblArchiveManager);
            this.tabExtract.Controls.Add(this.txtSearch);
            this.tabExtract.Controls.Add(this.btnExtractAll);
            this.tabExtract.Controls.Add(this.listViewArchive);
            this.tabExtract.Location = new Point(4, 22);
            this.tabExtract.Name = "tabExtract";
            this.tabExtract.Size = new Size(631, 524);
            this.tabExtract.TabIndex = 1;
            this.tabExtract.Text = "Extract";

            // 
            // lblArchiveManager
            // 
            this.lblArchiveManager.AutoSize = true;
            this.lblArchiveManager.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            this.lblArchiveManager.Location = new Point(20, 20);
            this.lblArchiveManager.Text = "Archive Manager";

            // 
            // txtSearch
            // 
            this.txtSearch.Font = new Font("Microsoft Sans Serif", 11.25F);
            this.txtSearch.Location = new Point(15, 57);
            this.txtSearch.Size = new Size(483, 24);
            this.txtSearch.Text = "Search files in archive...";

            // 
            // btnExtractAll
            // 
            this.btnExtractAll.Location = new Point(509, 54);
            this.btnExtractAll.Size = new Size(100, 30);
            this.btnExtractAll.Text = "Extract All";

            // 
            // listViewArchive
            // 
            this.listViewArchive.Location = new Point(15, 90);
            this.listViewArchive.Size = new Size(608, 350);
            this.listViewArchive.View = View.Details;
            this.listViewArchive.FullRowSelect = true;
            this.listViewArchive.HideSelection = false;

            // 
            // Form1
            // 
            this.ClientSize = new Size(639, 532);
            this.Controls.Add(this.tabControl);
            this.Name = "Form1";
            this.Text = "AeroCompress Vault";

            this.tabControl.ResumeLayout(false);
            this.tabCompress.ResumeLayout(false);
            this.tabCompress.PerformLayout();
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
    }
}
