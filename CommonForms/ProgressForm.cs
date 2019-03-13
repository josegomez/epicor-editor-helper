using System;
using System.Windows.Forms;

namespace CustomizationEditor
{
    public partial class ProgressForm : Form
    {
        public ProgressForm()
        {
            InitializeComponent();
        }

        public ProgressForm(string iTitleText)
        {
            InitializeComponent();
            this.Text = iTitleText;
        }

        private void Progress_Load(object sender, EventArgs e)
        {
            Application.EnableVisualStyles();
            pbProgress.Style = ProgressBarStyle.Marquee;
            pbProgress.MarqueeAnimationSpeed = 30;
        }

    }
}
