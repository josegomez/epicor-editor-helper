using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VSExtension
{
    public partial class frmSettings : Form
    {
        public frmSettings()
        {
            InitializeComponent();
            this.Icon = Resource.logoraw_notext_fw_Twv_icon;
        }

        private void frmSettings_Load(object sender, EventArgs e)
        {
            Settings.Default.Upgrade();
            txtDNSpy.Text = Settings.Default.DnSpy;
            txtDownFldr.Text = Settings.Default.CustomiationPath;
            txtEpicorClientFolder.Text = Settings.Default.EpicorFolder;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Settings.Default.DnSpy=txtDNSpy.Text;
            Settings.Default.CustomiationPath = txtDownFldr.Text;
            Settings.Default.EpicorFolder=txtEpicorClientFolder.Text ;
            Settings.Default.Save();
            this.DialogResult = DialogResult.OK;
        }

        private void btnEpicorClientFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fb = new FolderBrowserDialog())
            {
                fb.ShowDialog();
                txtEpicorClientFolder.Text = fb.SelectedPath;
            }
        }

        private void btnCustDown_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fb = new FolderBrowserDialog())
            {
                fb.ShowDialog();
                txtDownFldr.Text = fb.SelectedPath;
            }
        }

        private void btnDn_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fb = new FolderBrowserDialog())
            {
                fb.ShowDialog();
                txtDNSpy.Text = fb.SelectedPath;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}
