using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PerfAPIAnalyzer
{
    internal class TimelineControl : ScrollableControl
    {
        public enum EntryType
        {
            Unknown,
            Bar,
            Line,
        }
        public struct Entry
        {
            public double StartTime { get; set; }
            public double EndTime { get; set; }
            public int Row { get; set; }
            public int Kind { get; set; }
            public int LinkIndex { get; set; }
            public EntryType Type { get; set; }
            public double Height { get; set; }
            public string Name { get; set; }
            public string ToolTip { get; set; }
        }

        private Color[] ColourValues;

        public int BarHeight { get; set; } = 50;
        public int TimelineOffset { get; set; } = 30;
        public int TimelineHeight { get; set; } = 100;
        public int LineHeight { get; set; } = 10;
        public int BarMargin { get; set; } = 50;
        public double TimeScale { get; set; } = 0.001f;
        public List<Entry> Entries { get; private set; }

        public TimelineControl()
        {
            Entries = new List<Entry>();
            DoubleBuffered = true;
            ResizeRedraw = true;

            ColourValues = Extensions.Colors.ChartColorPallets.Pastel.ToArray();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;

            g.Clear(BackColor);
            g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

            //Draw the top and bottom timelines
            {
                TimelineHeight = (int)(2 * (BarHeight + BarMargin + BarMargin));
                Pen pen = new Pen(Color.DarkGray);
                SolidBrush brush = new SolidBrush(Color.DarkGray);
                g.DrawLine(pen, 0, TimelineOffset, AutoScrollMinSize.Width, TimelineOffset);
                g.DrawLine(pen, 0, TimelineOffset, 0, TimelineHeight);
                g.DrawLine(pen, AutoScrollMinSize.Width, TimelineOffset, AutoScrollMinSize.Width, TimelineHeight);
                g.DrawLine(pen, AutoScrollMinSize.Width / 2, TimelineOffset, AutoScrollMinSize.Width / 2, TimelineHeight);
                g.DrawLine(pen, AutoScrollMinSize.Width / 4, TimelineOffset, AutoScrollMinSize.Width / 4, TimelineHeight);
                g.DrawLine(pen, (AutoScrollMinSize.Width * 3) / 4, TimelineOffset, (AutoScrollMinSize.Width * 3) / 4, TimelineHeight);

                for (int i = 0; i < AutoScrollMinSize.Width; i += 100)
                {
                    g.DrawLine(pen, i - 75, TimelineOffset, i - 75, TimelineHeight * 0.75f);
                    g.DrawLine(pen, i - 25, TimelineOffset, i - 25, TimelineHeight * 0.75f);
                    g.DrawLine(pen, i - 50, TimelineOffset, i - 50, TimelineHeight * 0.8f);
                    g.DrawLine(pen, i, TimelineOffset, i, TimelineHeight);
                    g.DrawString($"{(i / TimeScale) / 1000:F3} us", this.Font, brush, i, TimelineHeight - 20);
                }
            }

            for (int i = 0; i < Entries.Count; i++)
            {
                //Compute x-position at which to draw this entry
                int x_b = (int)(Entries[i].StartTime * TimeScale);
                int w_b = (int)(Entries[i].EndTime * TimeScale - x_b);
                //Compute y-position at which to draw this entry
                int y_b_o = Entries[i].Row * (BarHeight + BarMargin) + BarMargin;
                int h_b = (int)(BarHeight * Entries[i].Height);
                int y_b = y_b_o + (BarHeight - h_b);

                int col_idx = Entries[i].Kind % ColourValues.Length;
                Color c = ColourValues[col_idx];
                SolidBrush brush = new SolidBrush(c);
                Pen pen = new Pen(c);

                g.FillRectangle(Brushes.White, x_b, y_b_o, AutoScrollMinSize.Width, BarHeight);

                if (Entries[i].Type == EntryType.Bar)
                    g.FillRectangle(brush, x_b, y_b, w_b, h_b);
                else
                {
                    g.DrawLine(pen, x_b, y_b_o - LineHeight, x_b, y_b_o + BarHeight + LineHeight);
                    if (Entries[i].LinkIndex != -1)
                    {
                        int l = Entries[i].LinkIndex;

                        //Draw a line connecting this link to the other link
                        //Compute x position of link
                        int x_b_l = (int)(Entries[l].StartTime * TimeScale);
                        int y_b_o_l = Entries[l].Row * (BarHeight + BarMargin) + BarMargin;

                        g.DrawLine(pen, x_b, y_b_o - LineHeight, x_b_l, y_b_o_l + BarHeight + LineHeight);
                    }
                }

                if (x_b + w_b > AutoScrollMinSize.Width) AutoScrollMinSize = new Size(x_b + w_b + BarMargin, AutoScrollMinSize.Height);
                if (y_b + h_b > AutoScrollMinSize.Height) AutoScrollMinSize = new Size(AutoScrollMinSize.Width, y_b + h_b);
            }
        }
    }
}
