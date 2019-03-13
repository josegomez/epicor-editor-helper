using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace CustomizationEditor
{
    public partial class LoginForm : Form
    {
        string clientFolder = "";
        public LoginForm(string clientFolder)
        {
            InitializeComponent();
            this.clientFolder = clientFolder;
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            var sysconfigs = Directory.GetFiles($"{clientFolder}/config/", "*.sysconfig");
            List<Environment> envAry = new List<Environment>();
            foreach (string s in sysconfigs)
            {
                envAry.Add(new Environment() { Path = s, Name = Path.GetFileName(s) });
            }
            cmbEnvironment.DataSource = envAry;
            cmbEnvironment.ValueMember = "Path";
            cmbEnvironment.DisplayMember = "Name";
            if (!string.IsNullOrEmpty(Settings.Default.Environment) && Settings.Default.Remember)
                cmbEnvironment.SelectedValue = Settings.Default.Environment;
            if(Settings.Default.Remember)
            {
                txtUsername.Text = Settings.Default.Username;
                txtPassword.Text = Settings.Default.Password;
                chkRemember.Checked = true;
            }

            ForceFront();
        }

        private void ForceFront()
        {
            this.TopMost = true;
            this.Focus();
            this.BringToFront();
            this.cmbEnvironment.Focus();
            System.Media.SystemSounds.Beep.Play();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            Settings.Default.Remember = chkRemember.Checked;
            if (cmbEnvironment.SelectedValue != null)
                Settings.Default.Environment = ((string)cmbEnvironment.SelectedValue);
            Settings.Default.Username = txtUsername.Text;
            Settings.Default.Password = txtPassword.Text;

            Settings.Default.Save();
            this.DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private bool
    }
}
