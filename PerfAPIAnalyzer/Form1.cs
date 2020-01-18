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
        TimelineControl ctrl;

        private void openPerformanceLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog2.Filter = "Performance Logs|*.csv";
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                log = new PerformanceLogParser(openFileDialog2.FileName, curArch);

                for (int i = 0; i < log.Frames.Length; i++)
                    frameListBox.Items.Add(log.Frames[i]);
            }
        }

        private void frameListBox_SelectedValueChanged(object sender, EventArgs e)
        {
            if (frameListBox.SelectedItem == null)
                return;
            var cur_frame = (Frame)frameListBox.SelectedItem;
            eventListBox.Items.Clear();
            for (int i = 0; i < cur_frame.Calls.Length; i++)
                if (cur_frame.Calls[i].Type != CallType.FenceRaised && cur_frame.Calls[i].Type != CallType.PlaceFence)
                    eventListBox.Items.Add(cur_frame.Calls[i]);

            //Update timeline view
            ctrl.Entries.Clear();
            for(int i = 0; i < cur_frame.Calls.Length; i++)
            {
                if (cur_frame.Calls[i].Type == CallType.FenceRaised || cur_frame.Calls[i].Type == CallType.PlaceFence)
                {
                    ctrl.Entries.Add(new TimelineControl.Entry()
                    {
                        StartTime = cur_frame.Calls[i].Counters["GPUTimestamp"],
                        EndTime = cur_frame.Calls[i].Counters["GPUTimestamp"] + 5,
                        Height = 1,
                        Name = cur_frame.Calls[i].Name,
                        Row = 0,
                        LinkIndex = -1,
                        Kind = (int)cur_frame.Calls[i].Type - 1,
                        Type = TimelineControl.EntryType.Line,
                        ToolTip = "None"
                    });
                    ctrl.Entries.Add(new TimelineControl.Entry()
                    {
                        StartTime = cur_frame.Calls[i].Counters["CPUTime"],
                        EndTime = cur_frame.Calls[i].Counters["CPUEnd"],
                        Height = 1,
                        Name = cur_frame.Calls[i].Name,
                        Row = 1,
                        LinkIndex = ctrl.Entries.Count - 1,
                        Kind = (int)cur_frame.Calls[i].Type - 1,
                        Type = TimelineControl.EntryType.Line,
                        ToolTip = "None"
                    });
                }
                else
                {
                    ctrl.Entries.Add(new TimelineControl.Entry()
                    {
                        StartTime = cur_frame.Calls[i].Counters["GPUTimestamp"],
                        EndTime = cur_frame.Calls[i].Counters["GPUTimestamp"] + cur_frame.Calls[i].Counters["GPUTime"],
                        Height = cur_frame.Calls[i].Counters["GPUBusy"] / 100.0f,
                        Name = cur_frame.Calls[i].Name,
                        Row = 0,
                        LinkIndex = -1,
                        Kind = (int)cur_frame.Calls[i].Type - 1,
                        Type = TimelineControl.EntryType.Bar,
                        ToolTip = "None"
                    });
                    ctrl.Entries.Add(new TimelineControl.Entry()
                    {
                        StartTime = cur_frame.Calls[i].Counters["CPUTime"],
                        EndTime = cur_frame.Calls[i].Counters["CPUEnd"],
                        Height = 1,
                        Name = cur_frame.Calls[i].Name,
                        Row = 1,
                        LinkIndex = -1,
                        Kind = (int)cur_frame.Calls[i].Type - 1,
                        Type = TimelineControl.EntryType.Bar,
                        ToolTip = "None"
                    });
                }
            }
            ctrl.Invalidate();
        }

        private void eventListBox_SelectedValueChanged(object sender, EventArgs e)
        {
            if (eventListBox.SelectedItem == null)
                return;
            var cur_event = (CallLog)eventListBox.SelectedItem;

            //Update the plots
            if (cur_event.Type == CallType.Compute)
            {
                infoTabs.SelectedTab = computeTab;

                computeChart.Series[0].Points.Clear();
                computeChart.Series[0].Points.AddXY("VALU Utilization", new object[] { cur_event.Counters["CSVALUUtilization"] });
                computeChart.Series[0].Points.AddXY("VALU Busy", new object[] { cur_event.Counters["CSVALUBusy"] });
                computeChart.Series[0].Points.AddXY("SALU Busy", new object[] { cur_event.Counters["CSSALUBusy"] });
                computeChart.Series[0].Points.AddXY("MemUnit Busy", new object[] { cur_event.Counters["CSMemUnitBusy"] });
                computeChart.Series[0].Points.AddXY("MemUnit Stalled", new object[] { cur_event.Counters["CSMemUnitStalled"] });
                computeChart.Series[0].Points.AddXY("WriteUnit Stalled", new object[] { cur_event.Counters["CSWriteUnitStalled"] });
                computeChart.Series[0].Points.AddXY("ALU Stalled By LDS", new object[] { cur_event.Counters["CSALUStalledByLDS"] });
                computeChart.Series[0].Points.AddXY("LDS Bank Conflict", new object[] { cur_event.Counters["CSLDSBankConflict"] });

                computeText.Text = $"GPU Time: {cur_event.Counters["GPUTime"] / 1000.0f}us\nThread Groups: {cur_event.Counters["CSThreadGroups"]}\nWavefronts: {cur_event.Counters["CSWavefronts"]}\nThreads: {cur_event.Counters["CSThreads"]}";
            }
            else if (cur_event.Type == CallType.MultiDrawIndirectCount)
            {
                infoTabs.SelectedTab = overviewTab;

                //Overview is a plot of the busy percentage per stage and the execution times
                overviewChart.Series[0].Points.Clear();
                foreach (string stage in log.PipelineBusyStages)
                    overviewChart.Series[0].Points.AddXY(stage + "Busy", new object[] { cur_event.Counters[stage + "Busy"] });

                overviewText.Text = $"GPU Time: {cur_event.Counters["GPUTime"] / 1000.0f}us\nVertices In: {cur_event.Counters["VSVerticesIn"]}\nPrimitives In: {cur_event.Counters["PrimitivesIn"]}\nCulled Primitives: {cur_event.Counters["CulledPrims"]}\nClipped Primitives: {cur_event.Counters["ClippedPrims"]}\nPixels Out: {cur_event.Counters["PSPixelsOut"]}";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ctrl = new TimelineControl();
            ctrl.Dock = DockStyle.Fill;
            splitContainer3.Panel2.Controls.Add(ctrl);
        }
    }
}
