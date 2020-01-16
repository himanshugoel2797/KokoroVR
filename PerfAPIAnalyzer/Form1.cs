using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PerfAPIAnalyzer
{
    public partial class Form1 : Form
    {
        PerformanceLogParser log;

        private void openPerformanceLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog2.Filter = "Performance Logs|*.csv";
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                log = new PerformanceLogParser(openFileDialog2.FileName);
            }
        }
    }
}
