using System;
using System.Drawing;
using System.Windows.Forms;
using Compression_Vault.Models;

namespace Compression_Vault.Controls
{
    public abstract class BaseItemControl : UserControl
    {
        protected readonly ICompressibleItem Item;
        protected Label lblName;
        protected Label lblInfo;
        protected Button btnRemove;

        protected BaseItemControl(ICompressibleItem item)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            InitializeBaseComponents();
            UpdateDisplay();
        }

        private void InitializeBaseComponents()
        {
            this.lblName = new Label();
            this.lblInfo = new Label();
            this.btnRemove = new Button();
            this.SuspendLayout();

            // lblName
            this.lblName.AutoSize = true;
            this.lblName.Location = new Point(5, 5);
            this.lblName.Name = "lblName";
            this.lblName.Size = new Size(200, 13);
            this.lblName.TabIndex = 0;

            // lblInfo
            this.lblInfo.AutoSize = true;
            this.lblInfo.Location = new Point(210, 5);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new Size(80, 13);
            this.lblInfo.TabIndex = 1;

            // btnRemove
            this.btnRemove.Location = new Point(300, 2);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new Size(20, 20);
            this.btnRemove.TabIndex = 2;
            this.btnRemove.Text = "X";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += BtnRemove_Click;

            // BaseItemControl
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.btnRemove);
            this.Name = "BaseItemControl";
            this.Size = new Size(325, 25);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            Item.RaiseRemoveRequest();
        }

        protected virtual void UpdateDisplay()
        {
            lblName.Text = Item.Name;
            lblInfo.Text = GetInfoText();
        }

        protected abstract string GetInfoText();
    }
} 