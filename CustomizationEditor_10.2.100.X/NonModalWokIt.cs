using CommonCode;
using Ice.Core;
using System;
using System.Threading;
using System.Windows.Forms;

namespace CustomizationEditor
{
    public partial class NonModalWokIt : Form
    {

        object session;
        EpicorLauncher l;
        CommandLineParams o;
        public bool Sync = false;
        public NonModalWokIt(object session, CommandLineParams o)
        {
            InitializeComponent();

            this.session = session;

            this.o = o;
            l = new EpicorLauncher();
        }

        private void NonModalWokIt_Load(object sender, EventArgs e)
        {
            lblCustom.Text = $"{o.Key1}-{o.Key2}";
        }

        private void btnTracing_Click(object sender, EventArgs e)
        {
            l.LaunchTracingOptions(this.session);
        }

        private void btnDataDic_Click(object sender, EventArgs e)
        {
            l.LaunchMenuOptions(this.session, "Ice.UI.DataDictViewer");
        }

        private void btnEP_Click(object sender, EventArgs e)
        {
            l.LaunchMenuOptions(this.session, "Ice.UI.UDExtendedProps");
        }

        private void btnBAQ_Click(object sender, EventArgs e)
        {
            l.LaunchMenuOptions(this.session, "XABA3040");
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            if (chkSyncUp.Checked)
            {
                l.UpdateCustomization(o, (Session)this.session);
            }

            l.LaunchInEpicor(o, (Session)this.session, false, false);
        }

        private void btnDebug_Click(object sender, EventArgs e)
        {
            
            
            if (!string.IsNullOrEmpty(o.DNSpy))
            {
                if (chkSyncUp.Checked)
                {
                    l.UpdateCustomization(o, (Session)this.session);
                }
                l.LaunchInEpicor(o, (Session)this.session, true, false);
                

                if (!string.IsNullOrEmpty(o.DNSpy))
                {
                    l.RunDnSpy(o);
                }

             
            }
            else
            {
                MessageBox.Show("No DNSpy Location was Supplied", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            l.LaunchInEpicor(o, (Session)this.session, true, true);
            Sync = true;
            if (o.Key2.Contains("MainController"))//Dashboard
            {
                l.DownloadAndSyncDashboard((Session)this.session, o);
            }
            else
            {
                l.DownloadAndSync((Session)this.session, o);
            }
        }

        private void NonModalWokIt_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void btnMenu_Click(object sender, EventArgs e)
        {
            l.LaunchMenuOptions(this.session, "Ice.UI.MenuMEntry");
        }

        private void NonModalWokIt_Shown(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }
    }
}
