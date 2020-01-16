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

        public static bool MetricsEnabled { get; set; }

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

                        string hdr = "";
                        for (int i = 0; i < results[0].Counters.Length; i++)
                            hdr += results[0].Counters[i].Name + ",";

                        hdr = hdr.Substring(0, hdr.Length - 1);
                        logFile.WriteLine(hdr);
                    }

                    for (int i = 0; i < results.Length; i++)
                    {
                        var str = "";
                        for (int j = 0; j < results[i].Counters.Length; j++)
                            str += (results[i].Counters[j].IsDouble ? results[i].Counters[j].DoubleValue : results[i].Counters[j].ULongValue) + ",";

                        str = str.Substring(0, str.Length - 1);
                        logFile.WriteLine(str);
                    }
                }
                cur_session.Dispose();
                cur_session = null;
            }


            if (cur_session == null)
            {
                cur_session = GraphicsDevice.Context.CreateSession();
                cur_session.EnableAllCounters();
                cur_session.Start();
            }
            cur_pass = cur_session.StartPass();
        }

        public static void BeginSample()
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
