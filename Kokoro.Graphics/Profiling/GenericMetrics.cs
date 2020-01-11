using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics.Profiling
{
    public struct GenericMeasurement
    {
        public long CPUTimestamp;
        public long Timestamp;
        public long Elapsed;
        public long VerticesSubmitted;
        public long PrimitivesSubmitted;
        public long VertexShaderInvocations;
        public long TessControlPatches;
        public long TessEvalInvocations;
        public long GeometryShaderInvocations;
        public long GeometryShaderPrimitivesEmitted;
        public long FragmentShaderInvocations;
        public long ComputeShaderInvocations;
        public long ClippingInputPrimitives;
        public long ClippingOutputPrimitives;
    }

    public static class GenericMetrics
    {
        static StreamWriter logFile;
        static Queue<PerfTimer[]> pending;
        static long start_ticks;

        public static bool MetricsEnabled { get; set; } = false;

        static GenericMetrics()
        {
            start_ticks = DateTime.Now.Ticks;
            pending = new Queue<PerfTimer[]>();
        }

        private static void InitFile()
        {
            if (logFile == null)
            {
                logFile = new StreamWriter($"generic_perf_log_{DateTime.Now.Ticks}.csv", false);

                string hdr = "";
                var members = typeof(GenericMeasurement).GetFields();
                for (int i = 0; i < members.Length; i++)
                    hdr += members[i].Name + ",";

                hdr = hdr.Substring(0, hdr.Length - 1);
                logFile.WriteLine(hdr);
            }
        }

        public static void StartMeasurement()
        {
            if (!MetricsEnabled) return;

            var timers = new PerfTimer[] {
                new PerfTimer(QueryType.Time),
                new PerfTimer(QueryType.VerticesSubmitted),
                new PerfTimer(QueryType.PrimitivesSubmitted),
                new PerfTimer(QueryType.VertexShaderInvocations),
                new PerfTimer(QueryType.TessControlShaderPatches),
                new PerfTimer(QueryType.TessEvalShaderInvocations),
                new PerfTimer(QueryType.GeometryShaderInvocations),
                new PerfTimer(QueryType.GeometryShaderPrimitivesEmitted),
                new PerfTimer(QueryType.FragmentShaderInvocations),
                new PerfTimer(QueryType.ComputeShaderInvocations),
                new PerfTimer(QueryType.ClippingInputPrimitives),
                new PerfTimer(QueryType.ClippingOutputPrimitives)
            };

            for (int i = 0; i < timers.Length; i++)
                timers[i].Start();

            pending.Enqueue(timers);
        }

        public static void StopMeasurement()
        {
            if (!MetricsEnabled) return;
            var p = pending.Peek();
            for (int i = 0; i < p.Length; i++)
                p[i].Stop();
        }

        public static void EndFrame()
        {
            if (!MetricsEnabled) return;
            InitFile();
            var m = new GenericMeasurement()
            {
                CPUTimestamp = DateTime.Now.Ticks - start_ticks,
                Elapsed = -1
            };
            var hdr = "";
            var fields = typeof(GenericMeasurement).GetFields();
            for (int i = 0; i < fields.Length; i++)
                hdr += (long)fields[i].GetValue(m) + ",";
            hdr = hdr.Substring(0, hdr.Length - 1);
            logFile.WriteLine(hdr);
            logFile.Flush();
        }

        internal static void UpdateLog()
        {
            if (!MetricsEnabled) return;
            if (pending.Count == 0) return;

            InitFile();
            bool loop = true;
            do
            {
                var p = pending.Peek();
                if (p[p.Length - 1].IsReady())
                {
                    var m = new GenericMeasurement()
                    {
                        CPUTimestamp = DateTime.Now.Ticks - start_ticks,
                        Elapsed = p[0].Read(),
                        Timestamp = p[0].Timestamp(),
                        VerticesSubmitted = p[1].Read(),
                        PrimitivesSubmitted = p[2].Read(),
                        VertexShaderInvocations = p[3].Read(),
                        TessControlPatches = p[4].Read(),
                        TessEvalInvocations = p[5].Read(),
                        GeometryShaderInvocations = p[6].Read(),
                        GeometryShaderPrimitivesEmitted = p[7].Read(),
                        FragmentShaderInvocations = p[8].Read(),
                        ComputeShaderInvocations = p[9].Read(),
                        ClippingInputPrimitives = p[10].Read(),
                        ClippingOutputPrimitives = p[11].Read()
                    };
                    
                    var hdr = "";
                    var fields = typeof(GenericMeasurement).GetFields();
                    for (int i = 0; i < fields.Length; i++)
                        hdr += (long)fields[i].GetValue(m) + ",";
                    hdr = hdr.Substring(0, hdr.Length - 1);
                    logFile.WriteLine(hdr);
                    logFile.Flush();

                    pending.Dequeue();
                    loop = true;
                }
                else
                    loop = false;
                if (pending.Count == 0)
                    loop = false;
            } while (loop);
        }


    }
}
