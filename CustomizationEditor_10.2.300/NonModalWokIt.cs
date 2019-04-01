using CommonCode;
using Ice.Core;
using System;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace CustomizationEditor
{
    public partial class NonModalWokIt : Form
    {

        object session;
        EpicorLauncher l;
        CommandLineParams o;
        public bool Sync = false;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        private const UInt32 SWP_NOSIZE = 0x0001;
        private const UInt32 SWP_NOMOVE = 0x0002;
        private const UInt32 TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;


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
            Thread.Sleep(1000);
            CheckTM();
        }

        private void btnDebug_Click(object sender, EventArgs e)
        {
            if (chkSyncUp.Checked)
            {
                l.UpdateCustomization(o, (Session)this.session);
            }

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
            Console.WriteLine("EDITMODE");
            if (chkSyncUp.Checked)
            {
                l.UpdateCustomization(o, (Session)this.session);
            }
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
            CheckTM();
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

        private void btnObjectExplorer_Click(object sender, EventArgs e)
        {
            l.LaunchObjectExplorer(o,session);
        }

        private void chkAOT_CheckedChanged(object sender, EventArgs e)
        {
            CheckTM();
        }

        private void CheckTM()
        {
            if (chkAOT.Checked)
                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
            else
                SetWindowPos(this.Handle, HWND_NOTOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private void btnRefs_Click(object sender, EventArgs e)
        {
            Console.WriteLine("EDITMODE");
            if (chkSyncUp.Checked)
            {
                l.UpdateCustomization(o, (Session)this.session);
            }
            l.LaunchReferences(o, session);
            if (o.Key2.Contains("MainController"))//Dashboard
            {
                l.DownloadAndSyncDashboard((Session)this.session, o);
            }
            else
            {
                l.DownloadAndSync((Session)this.session, o);
            }
        }

        private void btnCodeWizard_Click(object sender, EventArgs e)
        {
            Console.WriteLine("EDITMODE");
            if (chkSyncUp.Checked)
            {
                l.UpdateCustomization(o, (Session)this.session);
            }
            l.LaunchWizard(o, session);
            if (o.Key2.Contains("MainController"))//Dashboard
            {
                l.DownloadAndSyncDashboard((Session)this.session, o);
            }
            else
            {
                l.DownloadAndSync((Session)this.session, o);
            }
        }

        private void btnDataTools_Click(object sender, EventArgs e)
        {
            Console.WriteLine("EDITMODE");
            if (chkSyncUp.Checked)
            {
                l.UpdateCustomization(o, (Session)this.session);
            }
            l.LaunchDataTools(o, session);
            if (o.Key2.Contains("MainController"))//Dashboard
            {
                l.DownloadAndSyncDashboard((Session)this.session, o);
            }
            else
            {
                l.DownloadAndSync((Session)this.session, o);
            }
        }
    }
}
