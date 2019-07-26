using CommonForms.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Configuration;

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
                if (Settings.Default.Encrypted)
                    txtPassword.Text = Encoding.Unicode.GetString(ProtectedData.Unprotect(Convert.FromBase64String(Settings.Default.Password), Encoding.Unicode.GetBytes("70A47403717EC0F50E0755B2C4CF8488C8A061F3A694E0D1AB336D672C21781A"), DataProtectionScope.CurrentUser));
                else
                    txtPassword.Text = Settings.Default.Password;

                chkSSO.Checked = Settings.Default.SSO;
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

            byte[] bytes = Encoding.Unicode.GetBytes(txtPassword.Text);
            byte[] protectedPassword = ProtectedData.Protect(bytes, Encoding.Unicode.GetBytes("70A47403717EC0F50E0755B2C4CF8488C8A061F3A694E0D1AB336D672C21781A"), DataProtectionScope.CurrentUser);
            string encryptedString = Convert.ToBase64String(protectedPassword);
            Settings.Default.Encrypted = true;
            Settings.Default.Password = encryptedString;
            Settings.Default.SSO = chkSSO.Checked;
            Settings.Default.Save();
            var path = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;

            this.DialogResult = DialogResult.OK;
        }



        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void chkSSO_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.Enabled = !chkSSO.Checked;
            if (!txtPassword.Enabled)
                txtPassword.Text = "~";
            txtUsername.Enabled = !chkSSO.Checked;
            if (!txtUsername.Enabled)
                txtUsername.Text = "~";

        }
    }
}
