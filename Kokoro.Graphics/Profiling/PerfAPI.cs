using GPUPerfAPI.NET;
using System;
using System.Collections.Generic;
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
        static List<string> sample_names;

        static int multidrawindirectCount_idx = 0;
        static int compute_idx = 0;
        static int computeIndirect_idx = 0;

        public static bool MetricsEnabled { get; set; }

        static PerfAPI()
        {
            sample_names = new List<string>();
        }

        public static void BeginFrame()
        {
            if (!MetricsEnabled)
                return;

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

                        string hdr = "TaskName,";
                        for (int i = 0; i < results[0].Counters.Length; i++)
                            hdr += results[0].Counters[i].Name + ",";

                        hdr = hdr.Substring(0, hdr.Length - 1);
                        logFile.WriteLine(hdr);

                        hdr = "String,";
                        for (int i = 0; i < results[0].Counters.Length; i++)
                            hdr += results[0].Counters[i].Usage + ",";
                        hdr = hdr.Substring(0, hdr.Length - 1);
                        logFile.WriteLine(hdr);
                    }

                    for (int i = 0; i < results.Length; i++)
                    {
                        var str = sample_names[i] + ",";
                        for (int j = 0; j < results[i].Counters.Length; j++)
                            str += (results[i].Counters[j].IsDouble ? results[i].Counters[j].DoubleValue : results[i].Counters[j].ULongValue) + ",";

                        str = str.Substring(0, str.Length - 1);
                        logFile.WriteLine(str);
                    }

                    var str2 = "FrameEnd,";
                    for (int i = 0; i < results[0].Counters.Length; i++)
                        str2 += ",";
                    str2 = str2.Substring(0, str2.Length - 1);
                    logFile.WriteLine(str2);
                }
                cur_session.Dispose();
                cur_session = null;
            }

            sample_names.Clear();
            multidrawindirectCount_idx = 0;
            compute_idx = 0;
            computeIndirect_idx = 0;

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
            sample_names.Add($"MultiDrawIndirectCount #{multidrawindirectCount_idx++}");
            BeginSample();
        }

        public static void BeginCompute()
        {
            sample_names.Add($"Compute #{compute_idx++}");
            BeginSample();
        }

        public static void BeginComputeIndirect()
        {
            sample_names.Add($"ComputeIndirect #{computeIndirect_idx++}");
            BeginSample();
        }

        public static void BeginSample(string name)
        {
            sample_names.Add(name);
            BeginSample();
        }

        internal static void BeginSample()
        {
            if (!MetricsEnabled)
                return;
            cur_pass.BeginSample();
        }

        public static void EndSample()
        {
            if (!MetricsEnabled)
                return;
            cur_pass.EndSample();
        }

        public static void EndFrame()
        {
            if (!MetricsEnabled)
                return;
            cur_session.EndPass(cur_pass);
        }
    }
}
