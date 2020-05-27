namespace cAlgo
{
    partial class FrmWrapper
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.mybrowser = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // mybrowser
            // 
            this.mybrowser.AllowWebBrowserDrop = false;
            this.mybrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mybrowser.IsWebBrowserContextMenuEnabled = false;
            this.mybrowser.Location = new System.Drawing.Point(0, 0);
            this.mybrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.mybrowser.Name = "mybrowser";
            this.mybrowser.ScriptErrorsSuppressed = true;
            this.mybrowser.ScrollBarsEnabled = false;
            this.mybrowser.Size = new System.Drawing.Size(384, 641);
            this.mybrowser.TabIndex = 0;
            this.mybrowser.WebBrowserShortcutsEnabled = false;
            // 
            // FrmWrapper
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 641);
            this.Controls.Add(this.mybrowser);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmWrapper";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "TRENDLINE POWER";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.FrmWrapper_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.WebBrowser mybrowser;
    }
}