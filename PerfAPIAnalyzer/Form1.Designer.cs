namespace PerfAPIAnalyzer
{
    partial class Form1
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea7 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series7 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea8 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series8 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openShaderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openPerformanceLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.architectureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.splitContainer4 = new System.Windows.Forms.SplitContainer();
            this.splitContainer5 = new System.Windows.Forms.SplitContainer();
            this.frameListBox = new System.Windows.Forms.ListBox();
            this.eventListBox = new System.Windows.Forms.ListBox();
            this.infoTabs = new System.Windows.Forms.TabControl();
            this.overviewTab = new System.Windows.Forms.TabPage();
            this.overviewText = new System.Windows.Forms.RichTextBox();
            this.overviewChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.tabPage6 = new System.Windows.Forms.TabPage();
            this.tabPage7 = new System.Windows.Forms.TabPage();
            this.tabPage8 = new System.Windows.Forms.TabPage();
            this.computeTab = new System.Windows.Forms.TabPage();
            this.computeText = new System.Windows.Forms.RichTextBox();
            this.computeChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.tabPage10 = new System.Windows.Forms.TabPage();
            this.tabPage11 = new System.Windows.Forms.TabPage();
            this.tabPage12 = new System.Windows.Forms.TabPage();
            this.tabPage13 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.listBox2 = new System.Windows.Forms.ListBox();
            this.listBox3 = new System.Windows.Forms.ListBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.openFileDialog2 = new System.Windows.Forms.OpenFileDialog();
            this.menuStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).BeginInit();
            this.splitContainer4.Panel1.SuspendLayout();
            this.splitContainer4.Panel2.SuspendLayout();
            this.splitContainer4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer5)).BeginInit();
            this.splitContainer5.Panel1.SuspendLayout();
            this.splitContainer5.Panel2.SuspendLayout();
            this.splitContainer5.SuspendLayout();
            this.infoTabs.SuspendLayout();
            this.overviewTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.overviewChart)).BeginInit();
            this.computeTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.computeChart)).BeginInit();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.architectureToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1503, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openShaderToolStripMenuItem,
            this.openPerformanceLogToolStripMenuItem,
            this.toolStripSeparator1,
            this.settingsToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openShaderToolStripMenuItem
            // 
            this.openShaderToolStripMenuItem.Name = "openShaderToolStripMenuItem";
            this.openShaderToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
            this.openShaderToolStripMenuItem.Text = "Open Shader";
            this.openShaderToolStripMenuItem.Click += new System.EventHandler(this.openShaderToolStripMenuItem_Click);
            // 
            // openPerformanceLogToolStripMenuItem
            // 
            this.openPerformanceLogToolStripMenuItem.Name = "openPerformanceLogToolStripMenuItem";
            this.openPerformanceLogToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
            this.openPerformanceLogToolStripMenuItem.Text = "Open Performance Log";
            this.openPerformanceLogToolStripMenuItem.Click += new System.EventHandler(this.openPerformanceLogToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(194, 6);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
            this.settingsToolStripMenuItem.Text = "Settings";
            // 
            // architectureToolStripMenuItem
            // 
            this.architectureToolStripMenuItem.Name = "architectureToolStripMenuItem";
            this.architectureToolStripMenuItem.Size = new System.Drawing.Size(84, 20);
            this.architectureToolStripMenuItem.Text = "Architecture";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 24);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1503, 938);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.splitContainer3);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1495, 912);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Counters";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(3, 3);
            this.splitContainer3.Name = "splitContainer3";
            this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.splitContainer4);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.AutoScroll = true;
            this.splitContainer3.Size = new System.Drawing.Size(1489, 906);
            this.splitContainer3.SplitterDistance = 496;
            this.splitContainer3.TabIndex = 0;
            // 
            // splitContainer4
            // 
            this.splitContainer4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer4.Location = new System.Drawing.Point(0, 0);
            this.splitContainer4.Name = "splitContainer4";
            // 
            // splitContainer4.Panel1
            // 
            this.splitContainer4.Panel1.Controls.Add(this.splitContainer5);
            // 
            // splitContainer4.Panel2
            // 
            this.splitContainer4.Panel2.Controls.Add(this.infoTabs);
            this.splitContainer4.Size = new System.Drawing.Size(1489, 496);
            this.splitContainer4.SplitterDistance = 406;
            this.splitContainer4.TabIndex = 0;
            // 
            // splitContainer5
            // 
            this.splitContainer5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer5.Location = new System.Drawing.Point(0, 0);
            this.splitContainer5.Name = "splitContainer5";
            this.splitContainer5.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer5.Panel1
            // 
            this.splitContainer5.Panel1.Controls.Add(this.frameListBox);
            // 
            // splitContainer5.Panel2
            // 
            this.splitContainer5.Panel2.Controls.Add(this.eventListBox);
            this.splitContainer5.Size = new System.Drawing.Size(406, 496);
            this.splitContainer5.SplitterDistance = 208;
            this.splitContainer5.TabIndex = 0;
            // 
            // frameListBox
            // 
            this.frameListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.frameListBox.FormattingEnabled = true;
            this.frameListBox.Location = new System.Drawing.Point(0, 0);
            this.frameListBox.Name = "frameListBox";
            this.frameListBox.Size = new System.Drawing.Size(406, 208);
            this.frameListBox.TabIndex = 0;
            this.frameListBox.SelectedValueChanged += new System.EventHandler(this.frameListBox_SelectedValueChanged);
            // 
            // eventListBox
            // 
            this.eventListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.eventListBox.FormattingEnabled = true;
            this.eventListBox.Location = new System.Drawing.Point(0, 0);
            this.eventListBox.Name = "eventListBox";
            this.eventListBox.Size = new System.Drawing.Size(406, 284);
            this.eventListBox.TabIndex = 1;
            this.eventListBox.SelectedValueChanged += new System.EventHandler(this.eventListBox_SelectedValueChanged);
            // 
            // infoTabs
            // 
            this.infoTabs.Controls.Add(this.overviewTab);
            this.infoTabs.Controls.Add(this.tabPage4);
            this.infoTabs.Controls.Add(this.tabPage5);
            this.infoTabs.Controls.Add(this.tabPage6);
            this.infoTabs.Controls.Add(this.tabPage7);
            this.infoTabs.Controls.Add(this.tabPage8);
            this.infoTabs.Controls.Add(this.computeTab);
            this.infoTabs.Controls.Add(this.tabPage10);
            this.infoTabs.Controls.Add(this.tabPage11);
            this.infoTabs.Controls.Add(this.tabPage12);
            this.infoTabs.Controls.Add(this.tabPage13);
            this.infoTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infoTabs.Location = new System.Drawing.Point(0, 0);
            this.infoTabs.Name = "infoTabs";
            this.infoTabs.SelectedIndex = 0;
            this.infoTabs.Size = new System.Drawing.Size(1079, 496);
            this.infoTabs.TabIndex = 0;
            // 
            // overviewTab
            // 
            this.overviewTab.Controls.Add(this.overviewText);
            this.overviewTab.Controls.Add(this.overviewChart);
            this.overviewTab.Location = new System.Drawing.Point(4, 22);
            this.overviewTab.Name = "overviewTab";
            this.overviewTab.Padding = new System.Windows.Forms.Padding(3);
            this.overviewTab.Size = new System.Drawing.Size(1071, 470);
            this.overviewTab.TabIndex = 0;
            this.overviewTab.Text = "Overview";
            this.overviewTab.UseVisualStyleBackColor = true;
            // 
            // overviewText
            // 
            this.overviewText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.overviewText.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.overviewText.Location = new System.Drawing.Point(611, 7);
            this.overviewText.Name = "overviewText";
            this.overviewText.Size = new System.Drawing.Size(454, 457);
            this.overviewText.TabIndex = 1;
            this.overviewText.Text = "";
            // 
            // overviewChart
            // 
            this.overviewChart.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            chartArea7.AxisY.Maximum = 100D;
            chartArea7.AxisY.Minimum = 0D;
            chartArea7.Name = "ChartArea1";
            this.overviewChart.ChartAreas.Add(chartArea7);
            this.overviewChart.Location = new System.Drawing.Point(6, 6);
            this.overviewChart.Name = "overviewChart";
            series7.ChartArea = "ChartArea1";
            series7.Name = "Series1";
            series7.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.String;
            this.overviewChart.Series.Add(series7);
            this.overviewChart.Size = new System.Drawing.Size(598, 458);
            this.overviewChart.TabIndex = 0;
            this.overviewChart.Text = "chart1";
            // 
            // tabPage4
            // 
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(1071, 470);
            this.tabPage4.TabIndex = 1;
            this.tabPage4.Text = "Vertex";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // tabPage5
            // 
            this.tabPage5.Location = new System.Drawing.Point(4, 22);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage5.Size = new System.Drawing.Size(1071, 470);
            this.tabPage5.TabIndex = 2;
            this.tabPage5.Text = "TCS";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // tabPage6
            // 
            this.tabPage6.Location = new System.Drawing.Point(4, 22);
            this.tabPage6.Name = "tabPage6";
            this.tabPage6.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage6.Size = new System.Drawing.Size(1071, 470);
            this.tabPage6.TabIndex = 3;
            this.tabPage6.Text = "TES";
            this.tabPage6.UseVisualStyleBackColor = true;
            // 
            // tabPage7
            // 
            this.tabPage7.Location = new System.Drawing.Point(4, 22);
            this.tabPage7.Name = "tabPage7";
            this.tabPage7.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage7.Size = new System.Drawing.Size(1071, 470);
            this.tabPage7.TabIndex = 4;
            this.tabPage7.Text = "Geometry";
            this.tabPage7.UseVisualStyleBackColor = true;
            // 
            // tabPage8
            // 
            this.tabPage8.Location = new System.Drawing.Point(4, 22);
            this.tabPage8.Name = "tabPage8";
            this.tabPage8.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage8.Size = new System.Drawing.Size(1071, 470);
            this.tabPage8.TabIndex = 5;
            this.tabPage8.Text = "Fragment";
            this.tabPage8.UseVisualStyleBackColor = true;
            // 
            // computeTab
            // 
            this.computeTab.Controls.Add(this.computeText);
            this.computeTab.Controls.Add(this.computeChart);
            this.computeTab.Location = new System.Drawing.Point(4, 22);
            this.computeTab.Name = "computeTab";
            this.computeTab.Padding = new System.Windows.Forms.Padding(3);
            this.computeTab.Size = new System.Drawing.Size(1071, 470);
            this.computeTab.TabIndex = 6;
            this.computeTab.Text = "Compute";
            this.computeTab.UseVisualStyleBackColor = true;
            // 
            // computeText
            // 
            this.computeText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.computeText.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.computeText.Location = new System.Drawing.Point(611, 7);
            this.computeText.Name = "computeText";
            this.computeText.Size = new System.Drawing.Size(454, 457);
            this.computeText.TabIndex = 3;
            this.computeText.Text = "";
            // 
            // computeChart
            // 
            this.computeChart.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            chartArea8.AxisY.Maximum = 100D;
            chartArea8.AxisY.Minimum = 0D;
            chartArea8.Name = "ChartArea1";
            this.computeChart.ChartAreas.Add(chartArea8);
            this.computeChart.Location = new System.Drawing.Point(6, 6);
            this.computeChart.Name = "computeChart";
            series8.ChartArea = "ChartArea1";
            series8.Name = "Series1";
            series8.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.String;
            this.computeChart.Series.Add(series8);
            this.computeChart.Size = new System.Drawing.Size(598, 458);
            this.computeChart.TabIndex = 2;
            this.computeChart.Text = "chart1";
            // 
            // tabPage10
            // 
            this.tabPage10.Location = new System.Drawing.Point(4, 22);
            this.tabPage10.Name = "tabPage10";
            this.tabPage10.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage10.Size = new System.Drawing.Size(1071, 470);
            this.tabPage10.TabIndex = 7;
            this.tabPage10.Text = "Primitive Assembly";
            this.tabPage10.UseVisualStyleBackColor = true;
            // 
            // tabPage11
            // 
            this.tabPage11.Location = new System.Drawing.Point(4, 22);
            this.tabPage11.Name = "tabPage11";
            this.tabPage11.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage11.Size = new System.Drawing.Size(1071, 470);
            this.tabPage11.TabIndex = 8;
            this.tabPage11.Text = "Depth Stencil";
            this.tabPage11.UseVisualStyleBackColor = true;
            // 
            // tabPage12
            // 
            this.tabPage12.Location = new System.Drawing.Point(4, 22);
            this.tabPage12.Name = "tabPage12";
            this.tabPage12.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage12.Size = new System.Drawing.Size(1071, 470);
            this.tabPage12.TabIndex = 9;
            this.tabPage12.Text = "Texture";
            this.tabPage12.UseVisualStyleBackColor = true;
            // 
            // tabPage13
            // 
            this.tabPage13.Location = new System.Drawing.Point(4, 22);
            this.tabPage13.Name = "tabPage13";
            this.tabPage13.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage13.Size = new System.Drawing.Size(1071, 470);
            this.tabPage13.TabIndex = 10;
            this.tabPage13.Text = "Memory";
            this.tabPage13.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.splitContainer1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1495, 912);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Shader Analyzer";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.listBox1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(1489, 906);
            this.splitContainer1.SplitterDistance = 494;
            this.splitContainer1.TabIndex = 2;
            // 
            // listBox1
            // 
            this.listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(0, 0);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(494, 906);
            this.listBox1.TabIndex = 0;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.listBox2);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.listBox3);
            this.splitContainer2.Size = new System.Drawing.Size(991, 906);
            this.splitContainer2.SplitterDistance = 486;
            this.splitContainer2.TabIndex = 0;
            // 
            // listBox2
            // 
            this.listBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox2.FormattingEnabled = true;
            this.listBox2.Location = new System.Drawing.Point(0, 0);
            this.listBox2.Name = "listBox2";
            this.listBox2.Size = new System.Drawing.Size(486, 906);
            this.listBox2.TabIndex = 1;
            // 
            // listBox3
            // 
            this.listBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox3.FormattingEnabled = true;
            this.listBox3.Location = new System.Drawing.Point(0, 0);
            this.listBox3.Name = "listBox3";
            this.listBox3.Size = new System.Drawing.Size(501, 906);
            this.listBox3.TabIndex = 1;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // openFileDialog2
            // 
            this.openFileDialog2.FileName = "openFileDialog2";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1503, 962);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "PerfAPIAnalyzer";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
            this.splitContainer3.ResumeLayout(false);
            this.splitContainer4.Panel1.ResumeLayout(false);
            this.splitContainer4.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).EndInit();
            this.splitContainer4.ResumeLayout(false);
            this.splitContainer5.Panel1.ResumeLayout(false);
            this.splitContainer5.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer5)).EndInit();
            this.splitContainer5.ResumeLayout(false);
            this.infoTabs.ResumeLayout(false);
            this.overviewTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.overviewChart)).EndInit();
            this.computeTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.computeChart)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ListBox listBox2;
        private System.Windows.Forms.ListBox listBox3;
        private System.Windows.Forms.ToolStripMenuItem architectureToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openShaderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openPerformanceLogToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.OpenFileDialog openFileDialog2;
        private System.Windows.Forms.SplitContainer splitContainer4;
        private System.Windows.Forms.ListBox frameListBox;
        private System.Windows.Forms.TabControl infoTabs;
        private System.Windows.Forms.TabPage overviewTab;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.TabPage tabPage5;
        private System.Windows.Forms.TabPage tabPage6;
        private System.Windows.Forms.TabPage tabPage7;
        private System.Windows.Forms.TabPage tabPage8;
        private System.Windows.Forms.TabPage computeTab;
        private System.Windows.Forms.TabPage tabPage10;
        private System.Windows.Forms.TabPage tabPage11;
        private System.Windows.Forms.TabPage tabPage12;
        private System.Windows.Forms.TabPage tabPage13;
        private System.Windows.Forms.SplitContainer splitContainer5;
        private System.Windows.Forms.ListBox eventListBox;
        private System.Windows.Forms.DataVisualization.Charting.Chart overviewChart;
        private System.Windows.Forms.RichTextBox overviewText;
        private System.Windows.Forms.RichTextBox computeText;
        private System.Windows.Forms.DataVisualization.Charting.Chart computeChart;
    }
}

