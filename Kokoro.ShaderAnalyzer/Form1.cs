using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kokoro.ShaderAnalyzer
{
    public partial class Form1 : Form
    {
        private AMDShaderAnalyzer shader;
        private GPUArch curArch;

        public Form1()
        {
            InitializeComponent();

            curArch = GPUArch.gfx1010;
            for (int i = 0; i < (int)GPUArch.ArchCount; i++)
            {
                var item = new ToolStripMenuItem()
                {
                    Text = ((GPUArch)i).ToString(),
                    CheckOnClick = true,
                    CheckState = CheckState.Unchecked,
                };
                if (i == (int)curArch)
                    item.CheckState = CheckState.Checked;
                item.CheckStateChanged += Item_CheckedChanged;
                architectureToolStripMenuItem.DropDownItems.Add(item);
            }
        }

        private void Item_CheckedChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < (int)GPUArch.ArchCount; i++)
                if (architectureToolStripMenuItem.DropDownItems[i].Text != ((ToolStripMenuItem)sender).Text)
                {
                    var item = (ToolStripMenuItem)architectureToolStripMenuItem.DropDownItems[i];
                    item.CheckStateChanged -= Item_CheckedChanged;
                    item.CheckState = CheckState.Unchecked;
                    item.CheckStateChanged += Item_CheckedChanged;
                }
                else
                {
                    curArch = (GPUArch)i;
                }
            shader.InvokeAnalyzer(curArch);
            UpdateDisplay();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Processed Shaders|*.glsl_out";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                shader = new AMDShaderAnalyzer(openFileDialog1.FileName);
                shader.InvokeAnalyzer(curArch);
                UpdateDisplay();
            }
        }
        private void ReloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (shader != null)
            {
                shader.InvokeAnalyzer(curArch);
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            if (shader != null)
            {
                listBox1.Items.Clear();
                listBox2.Items.Clear();
                listBox3.Items.Clear();

                listBox1.Items.AddRange(shader.Lines);
                listBox2.Items.AddRange(shader.Analysis[(int)curArch].ISA);
                listBox3.Items.AddRange(shader.Analysis[(int)curArch].RegisterMap);
            }
        }

    }
}
