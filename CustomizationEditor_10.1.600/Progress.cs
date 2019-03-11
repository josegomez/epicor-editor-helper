using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomizationEditor
{
    public partial class Progress : Form
    {
        public Progress()
        {
            InitializeComponent();
        }

        private void Progress_Load(object sender, EventArgs e)
        {
            Application.EnableVisualStyles();
            pbProgress.Style = ProgressBarStyle.Marquee;
            pbProgress.MarqueeAnimationSpeed = 30;
        }

    }
}
