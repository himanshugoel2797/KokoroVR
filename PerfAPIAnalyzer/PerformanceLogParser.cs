using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PerfAPIAnalyzer
{
    enum CallType
    {
        Unknown,
        MultiDrawIndirectCount,
        Compute,
        FrameEnd,
        PlaceFence,
        FenceRaised,
    }

    struct Frame
    {
        public int FrameIndex { get; set; }
        public CallLog[] Calls { get; set; }
        public double FrameStart { get; set; }
        public double FrameEnd { get; set; }
        public double CPUFrameStart { get; set; }
        public double CPUFrameEnd { get; set; }

        public override string ToString()
        {
            return "Frame #" + FrameIndex;
        }
    }

    struct CallLog
    {
        public CallType Type { get; set; }
        public int Index { get; set; }
        public string Name { get; set; }
        public Dictionary<string, double> Counters { get; set; }

        public override string ToString()
        {
            return $"{Name}";
        }
    }

    class PerformanceLogParser
    {
        public Frame[] Frames { get; set; }
        public GPUArch CurrentArch { get; set; }

        public string[] PipelineStages { get; set; }
        public string[] PipelineBusyStages { get; set; }
        public string[] ProgrammableStages { get; set; }
        public string[] LoadProgrammableStages { get; set; }
        public string[] TimedStages { get; set; }

        public PerformanceLogParser(string file, GPUArch curArch)
        {
            switch (curArch)
            {
                case GPUArch.gfx1010:
                    PipelineStages = new string[]   //BusyCycles
                    {
                        "GPU",
                        "VS",
                        "HS",
                        "DS",
                        "PS",
                        "PrimitiveAssembly",
                        "TexUnit",
                    };
                    PipelineBusyStages = new string[]   //Busy
                    {
                        "GPU",
                        "VS",
                        "HS",
                        "DS",
                        "PS",
                        "PrimitiveAssembly",
                        "TexUnit",
                        "DepthStencilTest",
                    };
                    TimedStages = new string[]  //Time
                    {
                        "GPU",
                        "VS",
                        "HS",
                        "DS",
                        "PS",
                    };
                    ProgrammableStages = new string[]   //VALUInstCount, SALUInstCount
                    {
                        "VS",
                        "HS",
                        "DS",
                        "PS",
                    };
                    LoadProgrammableStages = new string[]   //SALUBusy, SALUBusyCycles, VALUBusy, VALUBusyCycles
                    {
                        "VS",
                        "HS",
                        "PS",
                    };
                    break;
                default:
                    MessageBox.Show("Architecture Not Implemented.");
                    break;
            }
            //load the csv
            CurrentArch = curArch;
            var lines = File.ReadAllLines(file);

            //group by call type + index
            var pairs = new Dictionary<string, List<string>>();
            var hdrs = lines[0].Split(',').Select(a => a.Trim()).ToArray();
            for (int j = 0; j < hdrs.Length; j++)
                pairs[hdrs[j]] = new List<string>();

            int llen = 2;
            for (int i = 2; i < lines.Length; i++)
            {
                var res = lines[i].Split(',').Select(a => a.Trim()).ToArray();
                if (res.Length != hdrs.Length)
                {
                    llen++;
                    continue;
                }
                for (int j = 0; j < hdrs.Length; j++)
                    pairs[hdrs[j]].Add(res[j]);
            }

            int last_frame_end_idx = 0;
            for(int i = 0; i < lines.Length - llen; i++)
            {
                if (pairs["TaskName"][i].StartsWith("FrameEnd"))
                    last_frame_end_idx = i;
            }


            var frames = new List<Frame>();
            var ents = new List<CallLog>();
            double frame_cpu_start = 0, frame_gpu_start = 0;
            for (int i = 0; i <= last_frame_end_idx; i++)
            {
                var c_ent = new CallLog()
                {
                    Name = pairs["TaskName"][i],
                    Counters = new Dictionary<string, double>()
                };

                if (ents.Count == 0)
                {
                    frame_cpu_start = double.Parse(pairs["CPUTime"][i]);
                    frame_gpu_start = double.Parse(pairs["GPUTimestamp"][i]);
                }

                var type_index_pair = pairs["TaskName"][i].Split(' ');
                if (type_index_pair[0] == "FrameEnd")
                    c_ent.Type = CallType.FrameEnd;
                else if (type_index_pair[0] == "MultiDrawIndirectCount")
                {
                    c_ent.Type = CallType.MultiDrawIndirectCount;
                    c_ent.Index = int.Parse(type_index_pair[1].Substring(1));
                }
                else if (type_index_pair[0] == "Compute")
                {
                    c_ent.Type = CallType.Compute;
                    c_ent.Index = int.Parse(type_index_pair[1].Substring(1));
                }
                else if (type_index_pair[0] == "PlaceFence")
                {
                    c_ent.Type = CallType.PlaceFence;
                    c_ent.Index = int.Parse(type_index_pair[1].Substring(1));
                }
                else if (type_index_pair[0] == "RaiseFence")
                {
                    c_ent.Type = CallType.FenceRaised;
                    c_ent.Index = int.Parse(type_index_pair[1].Substring(1));
                }
                else throw new Exception();

                c_ent.Counters["CPUTime"] = double.Parse(pairs["CPUTime"][i]) - frame_cpu_start;
                c_ent.Counters["GPUTimestamp"] = double.Parse(pairs["GPUTimestamp"][i]) - frame_gpu_start + double.Parse(pairs["Latency"][i]);

                if (c_ent.Type != CallType.FrameEnd)
                    c_ent.Counters["CPUEnd"] = double.Parse(pairs["CPUTime"][i + 1]) - frame_cpu_start;

                if (c_ent.Type == CallType.MultiDrawIndirectCount)
                {
                    foreach (string stage in PipelineStages)
                        c_ent.Counters[stage + "BusyCycles"] = double.Parse(pairs[stage + "BusyCycles"][i]);

                    foreach (string stage in PipelineBusyStages)
                        c_ent.Counters[stage + "Busy"] = double.Parse(pairs[stage + "Busy"][i]);

                    foreach (string stage in TimedStages)
                        c_ent.Counters[stage + "Time"] = double.Parse(pairs[stage + "Time"][i]);

                    foreach (string stage in ProgrammableStages)
                    {
                        c_ent.Counters[stage + "VALUInstCount"] = double.Parse(pairs[stage + "VALUInstCount"][i]);
                        c_ent.Counters[stage + "SALUInstCount"] = double.Parse(pairs[stage + "SALUInstCount"][i]);
                    }

                    foreach (string stage in LoadProgrammableStages)
                    {
                        c_ent.Counters[stage + "VALUBusy"] = double.Parse(pairs[stage + "VALUBusy"][i]);
                        c_ent.Counters[stage + "SALUBusy"] = double.Parse(pairs[stage + "SALUBusy"][i]);
                        c_ent.Counters[stage + "VALUBusyCycles"] = double.Parse(pairs[stage + "VALUBusyCycles"][i]);
                        c_ent.Counters[stage + "SALUBusyCycles"] = double.Parse(pairs[stage + "SALUBusyCycles"][i]);
                    }
                    c_ent.Counters["VSVerticesIn"] = double.Parse(pairs["VSVerticesIn"][i]);
                    c_ent.Counters["PrimitivesIn"] = double.Parse(pairs["PrimitivesIn"][i]);
                    c_ent.Counters["CulledPrims"] = double.Parse(pairs["CulledPrims"][i]);
                    c_ent.Counters["ClippedPrims"] = double.Parse(pairs["ClippedPrims"][i]);
                    c_ent.Counters["PSPixelsOut"] = double.Parse(pairs["PSPixelsOut"][i]);
                }
                else if (c_ent.Type == CallType.Compute)
                {
                    //TODO add more counters
                    c_ent.Counters["GPUTime"] = double.Parse(pairs["GPUTime"][i]);
                    c_ent.Counters["GPUBusy"] = double.Parse(pairs["GPUBusy"][i]);
                    c_ent.Counters["CSBusy"] = double.Parse(pairs["CSBusy"][i]);
                    c_ent.Counters["CSBusyCycles"] = double.Parse(pairs["CSBusyCycles"][i]);

                    c_ent.Counters["CSTime"] = double.Parse(pairs["CSTime"][i]);
                    c_ent.Counters["CSThreadGroups"] = double.Parse(pairs["CSThreadGroups"][i]);
                    c_ent.Counters["CSWavefronts"] = double.Parse(pairs["CSWavefronts"][i]);
                    c_ent.Counters["CSThreads"] = double.Parse(pairs["CSThreads"][i]);
                    c_ent.Counters["CSVALUInsts"] = double.Parse(pairs["CSVALUInsts"][i]);
                    c_ent.Counters["CSSALUInsts"] = double.Parse(pairs["CSSALUInsts"][i]);
                    c_ent.Counters["CSVALUBusy"] = double.Parse(pairs["CSVALUBusy"][i]);
                    c_ent.Counters["CSSALUBusy"] = double.Parse(pairs["CSSALUBusy"][i]);
                    c_ent.Counters["CSVALUBusyCycles"] = double.Parse(pairs["CSVALUBusyCycles"][i]);
                    c_ent.Counters["CSSALUBusyCycles"] = double.Parse(pairs["CSSALUBusyCycles"][i]);

                    c_ent.Counters["CSMemUnitBusy"] = double.Parse(pairs["CSMemUnitBusy"][i]);
                    c_ent.Counters["CSMemUnitStalled"] = double.Parse(pairs["CSMemUnitStalled"][i]);

                    c_ent.Counters["CSWriteUnitStalled"] = double.Parse(pairs["CSWriteUnitStalled"][i]);
                    c_ent.Counters["CSALUStalledByLDS"] = double.Parse(pairs["CSALUStalledByLDS"][i]);
                    c_ent.Counters["CSLDSBankConflict"] = double.Parse(pairs["CSLDSBankConflict"][i]);
                    c_ent.Counters["CSVALUUtilization"] = double.Parse(pairs["CSVALUUtilization"][i]);
                }

                //Use these to plot a timeline, which can then be subdivided to view individual stage timelines
                if (c_ent.Type != CallType.FrameEnd && c_ent.Type != CallType.FenceRaised && c_ent.Type != CallType.PlaceFence)
                {
                    c_ent.Counters["ExecutionDuration"] = double.Parse(pairs["GPUTime_TOP_TO_BOTTOM_DURATION"][i]);
                    c_ent.Counters["ExecutionStart"] = double.Parse(pairs["GPUTime_TOP_TO_BOTTOM_START"][i]);
                    c_ent.Counters["ExecutionEnd"] = double.Parse(pairs["GPUTime_TOP_TO_BOTTOM_END"][i]);
                    ents.Add(c_ent);
                }
                else if (c_ent.Type == CallType.FenceRaised || c_ent.Type == CallType.PlaceFence)
                {
                    ents.Add(c_ent);
                }

                if (c_ent.Type == CallType.FrameEnd)
                {
                    frames.Add(new Frame()
                    {
                        Calls = ents.ToArray(),
                        FrameIndex = frames.Count,
                        FrameStart = frame_gpu_start,
                        FrameEnd = double.Parse(pairs["GPUTimestamp"][i]) - frame_gpu_start,
                        CPUFrameStart = frame_cpu_start,
                        CPUFrameEnd = double.Parse(pairs["CPUTime"][i]) - frame_cpu_start
                    });
                    ents.Clear();
                }
            }

            Frames = frames.ToArray();
        }
    }
}
