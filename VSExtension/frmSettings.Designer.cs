namespace VSExtension
{
    partial class frmSettings
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
            this.txtEpicorClientFolder = new System.Windows.Forms.TextBox();
            this.btnEpicorClientFolder = new System.Windows.Forms.Button();
            this.btnCustDown = new System.Windows.Forms.Button();
            this.txtDownFldr = new System.Windows.Forms.TextBox();
            this.btnDn = new System.Windows.Forms.Button();
            this.txtDNSpy = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtEpicorClientFolder
            // 
            this.txtEpicorClientFolder.Location = new System.Drawing.Point(168, 14);
            this.txtEpicorClientFolder.Name = "txtEpicorClientFolder";
            this.txtEpicorClientFolder.Size = new System.Drawing.Size(238, 20);
            this.txtEpicorClientFolder.TabIndex = 0;
            // 
            // btnEpicorClientFolder
            // 
            this.btnEpicorClientFolder.Location = new System.Drawing.Point(12, 12);
            this.btnEpicorClientFolder.Name = "btnEpicorClientFolder";
            this.btnEpicorClientFolder.Size = new System.Drawing.Size(150, 23);
            this.btnEpicorClientFolder.TabIndex = 1;
            this.btnEpicorClientFolder.Text = "Epicor Client Folder...";
            this.btnEpicorClientFolder.UseVisualStyleBackColor = true;
            this.btnEpicorClientFolder.Click += new System.EventHandler(this.btnEpicorClientFolder_Click);
            // 
            // btnCustDown
            // 
            this.btnCustDown.Location = new System.Drawing.Point(12, 41);
            this.btnCustDown.Name = "btnCustDown";
            this.btnCustDown.Size = new System.Drawing.Size(150, 23);
            this.btnCustDown.TabIndex = 2;
            this.btnCustDown.Text = "Download To Folder";
            this.btnCustDown.UseVisualStyleBackColor = true;
            this.btnCustDown.Click += new System.EventHandler(this.btnCustDown_Click);
            // 
            // txtDownFldr
            // 
            this.txtDownFldr.Location = new System.Drawing.Point(168, 44);
            this.txtDownFldr.Name = "txtDownFldr";
            this.txtDownFldr.Size = new System.Drawing.Size(238, 20);
            this.txtDownFldr.TabIndex = 3;
            // 
            // btnDn
            // 
            this.btnDn.Location = new System.Drawing.Point(12, 70);
            this.btnDn.Name = "btnDn";
            this.btnDn.Size = new System.Drawing.Size(150, 23);
            this.btnDn.TabIndex = 4;
            this.btnDn.Text = "DNSpy Location";
            this.btnDn.UseVisualStyleBackColor = true;
            this.btnDn.Click += new System.EventHandler(this.btnDn_Click);
            // 
            // txtDNSpy
            // 
            this.txtDNSpy.Location = new System.Drawing.Point(168, 73);
            this.txtDNSpy.Name = "txtDNSpy";
            this.txtDNSpy.Size = new System.Drawing.Size(238, 20);
            this.txtDNSpy.TabIndex = 5;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(168, 122);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 6;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(249, 122);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // frmSettings
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(418, 163);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.txtDNSpy);
            this.Controls.Add(this.btnDn);
            this.Controls.Add(this.txtDownFldr);
            this.Controls.Add(this.btnCustDown);
            this.Controls.Add(this.btnEpicorClientFolder);
            this.Controls.Add(this.txtEpicorClientFolder);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmSettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Epicor Customization Editor Settings";
            this.Load += new System.EventHandler(this.frmSettings_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtEpicorClientFolder;
        private System.Windows.Forms.Button btnEpicorClientFolder;
        private System.Windows.Forms.Button btnCustDown;
        private System.Windows.Forms.TextBox txtDownFldr;
        private System.Windows.Forms.Button btnDn;
        private System.Windows.Forms.TextBox txtDNSpy;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}