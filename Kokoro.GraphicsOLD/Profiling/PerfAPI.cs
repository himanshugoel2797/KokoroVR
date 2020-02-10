using GPUPerfAPI.NET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics.Profiling
{
    public static class PerfAPI
    {
        static Session cur_session;
        static Pass cur_pass;
        static StreamWriter logFile;
        static readonly List<(string, int, double, TimestampReader)> sample_names;

        static int multidrawindirectCount_idx = 0;
        static int compute_idx = 0;
        static int computeIndirect_idx = 0;
        static Stopwatch watch;
        static long latency = 0;

        public static bool MetricsEnabled { get; set; }

        static PerfAPI()
        {
            sample_names = new List<(string, int, double, TimestampReader)>();
        }

        public static void BeginFrame()
        {
            if (!MetricsEnabled)
                return;

            if(latency == 0)
            {
                var p = new TimestampReader();
                p.Start();
                OpenTK.Graphics.OpenGL4.GL.Finish();
                var latency_0 = OpenTK.Graphics.OpenGL4.GL.GetInteger64(OpenTK.Graphics.OpenGL4.GetPName.Timestamp);
                var latency_1 = p.Timestamp();
                latency = latency_0 - latency_1;
                watch.Restart();
            }

            if (cur_session == null)
            {
                cur_session = GraphicsDevice.Context.CreateSession();
                cur_session.EnableAllCounters();
                cur_session.Start();
            }
            cur_pass = cur_session.StartPass();
        }

        public static void BeginMultiDrawIndirectCount()
        {
            BeginSample($"MultiDrawIndirectCount #{multidrawindirectCount_idx++}");
        }

        public static void BeginMultiDrawIndirectIndexedCount()
        {
            BeginSample($"MultiDrawIndirectIndexedCount #{multidrawindirectCount_idx++}");
        }

        public static void BeginDraw()
        {
            BeginSample($"Draw #{multidrawindirectCount_idx++}");
        }

        public static void BeginDrawIndexed()
        {
            BeginSample($"DrawIndexed #{multidrawindirectCount_idx++}");
        }

        public static void BeginCompute()
        {
            BeginSample($"Compute #{compute_idx++}");
        }

        public static void BeginComputeIndirect()
        {
            BeginSample($"ComputeIndirect #{computeIndirect_idx++}");
        }

        public static void PlaceFence(int id)
        {
            if (!MetricsEnabled)
                return;
            var p = new TimestampReader();
            p.Start();
            if (cur_pass != null && cur_pass.IsSampling) cur_pass.EndSample();
            if (watch == null)watch = Stopwatch.StartNew();
            sample_names.Add(($"PlaceFence #{id}", -1, watch.ElapsedTicks * 1000000000.0f / Stopwatch.Frequency, p));
        }

        public static void FenceRaised(int id)
        {
            if (!MetricsEnabled)
                return;
            var p = new TimestampReader();
            p.Start();
            if (cur_pass != null && cur_pass.IsSampling) cur_pass.EndSample();
            if (watch == null) watch = Stopwatch.StartNew();
            sample_names.Add(($"RaiseFence #{id}", -1, watch.ElapsedTicks * 1000000000.0f / Stopwatch.Frequency, p));
        }

        public static void BeginSample(string name)
        {
            if (!MetricsEnabled)
                return;
            if (cur_pass != null && cur_pass.IsSampling) cur_pass.EndSample();
            var p = new TimestampReader();
            p.Start();
            if (watch == null) watch = Stopwatch.StartNew();
            cur_pass.BeginSample();
            sample_names.Add((name, cur_pass.SampleIndex, watch.ElapsedTicks * 1000000000.0f / Stopwatch.Frequency, p));
        }

        public static void EndSample()
        {
            if (!MetricsEnabled)
                return;
            //cur_pass.EndSample();
        }

        public static void EndFrame()
        {
            if (!MetricsEnabled)
                return;
            if (watch == null) watch = Stopwatch.StartNew();

            //TODO Read the command delay

            var p = new TimestampReader();
            p.Start();
            var ticks = watch.ElapsedTicks;
            
            if (cur_pass != null && cur_pass.IsSampling) cur_pass.EndSample();
            cur_session.EndPass(cur_pass);

            if (cur_session != null && cur_session.PassIndex == cur_session.PassCount - 1)
            {
                //Get the results and serialize them
                cur_session.Stop();
                if (cur_pass.SampleIndex > 0)
                {
                    var results = cur_session.GetResults((uint)cur_pass.SampleIndex);
                    //Serialize results
                    if (results.Length > 0 && logFile == null)
                    {
                        logFile = new StreamWriter($"gpuperfapi_log_{DateTime.Now.Ticks}.csv", false);

                        string hdr = "TaskName,CPUTime,GPUTimestamp,Latency,";
                        for (int i = 0; i < results[0].Counters.Length; i++)
                            hdr += results[0].Counters[i].Name + ",";

                        hdr = hdr[0..^1];
                        logFile.WriteLine(hdr);

                        hdr = "String,Nanoseconds,Nanoseconds,Nanoseconds,";
                        for (int i = 0; i < results[0].Counters.Length; i++)
                            hdr += results[0].Counters[i].Usage + ",";
                        hdr = hdr[0..^1];
                        logFile.WriteLine(hdr);
                    }

                    int res_idx = 0;
                    for (int i = 0; i < sample_names.Count; i++)
                    {
                        var str = sample_names[i].Item1 + "," + sample_names[i].Item3 + "," + (sample_names[i].Item4.Timestamp()) + "," + latency + ",";
                        for (int j = 0; j < results[0].Counters.Length; j++)
                            if (sample_names[i].Item2 == -1)
                                str += ",";
                            else
                            {
                                str += (results[res_idx].Counters[j].IsDouble ? results[res_idx].Counters[j].DoubleValue : results[res_idx].Counters[j].ULongValue) + ",";
                            }
                        if (sample_names[i].Item2 != -1) res_idx++;

                        str = str[0..^1];
                        logFile.WriteLine(str);
                    }

                    var str2 = $"FrameEnd,{ticks * 1000000000.0f / Stopwatch.Frequency},{p.Timestamp() + latency}," + latency + ",";
                    for (int i = 0; i < results[0].Counters.Length; i++)
                        str2 += ",";
                    str2 = str2[0..^1];
                    logFile.WriteLine(str2);
                }
                cur_session.Dispose();
                cur_session = null;
            }

            sample_names.Clear();
            multidrawindirectCount_idx = 0;
            compute_idx = 0;
            computeIndirect_idx = 0;
        }
    }
}
