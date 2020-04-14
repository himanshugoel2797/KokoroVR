using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GPUPerfAPI.NET
{
    public class Session : IDisposable
    {
        private ulong id;

        public int PassIndex { get; private set; }
        public int PassCount { get; private set; }

        internal Session(ulong id)
        {
            this.id = id;
        }

        public void EnableAllCounters()
        {
            var ret = Binding.EnableAllCountersGPA(id);
            if (ret != 0)
                throw new Exception("GPA Failed.");
            Binding.GetPassCountGPA(id, out var pass_cnt);
            PassCount = (int)pass_cnt;
        }

        public void EnableCounter(string name)
        {
            Binding.EnableCounterByNameGPA(id, name);
            Binding.GetPassCountGPA(id, out var pass_cnt);
            PassCount = (int)pass_cnt;
        }

        public void DisableAllCounters()
        {
            var ret = Binding.DisableAllCountersGPA(id);
            if (ret != 0)
                throw new Exception("GPA Failed.");
        }

        public void DisableCounter(string name)
        {
            var ret = Binding.DisableCounterByNameGPA(id, name);
            if (ret != 0)
                throw new Exception("GPA Failed.");
        }

        public void Start()
        {
            PassIndex = -1;
            int ret = Binding.BeginSessionGPA(id);
            if (ret != 0)
                throw new Exception("GPA Failed.");
        }

        public void Stop()
        {
            var ret = Binding.EndSessionGPA(id);
            if (ret != 0)
                throw new Exception("GPA Failed.");
        }

        public SessionCounters[] GetResults(uint SampleCount)
        {
            var ses = new List<SessionCounters>();
            for (uint i = 0; i <= SampleCount; i++)
            {
                var cv = new List<CounterValue>();
                Binding.GetSampleResultSizeGPA(id, i, out var sz);
                var buffer = new ulong[sz / sizeof(ulong)];
                Binding.GetSampleResultGPA(id, i, sz, buffer);

                Binding.GetNumEnabledCountersGPA(id, out var num_cntrs);
                for (uint j = 0; j < num_cntrs; j++)
                {
                    Binding.GetEnabledIndexGPA(id, j, out var enabled_idx);
                    Binding.GetCounterNameGPA(enabled_idx, out var namePtr);
                    string name = Marshal.PtrToStringAnsi(namePtr);

                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    Binding.GetCounterUsageTypeGPA(enabled_idx, out var usage_type);
                    Binding.GetCounterDataTypeGPA(enabled_idx, out var data_type);

                    if (data_type == DataType.Double64)
                    {
                        cv.Add(new CounterValue()
                        {
                            Name = name,
                            IsDouble = true,
                            DoubleValue = BitConverter.ToDouble(BitConverter.GetBytes(buffer[j]), 0),
                            Usage = usage_type
                        });
                    }
                    else if (data_type == DataType.Ulong64)
                    {
                        cv.Add(new CounterValue()
                        {
                            Name = name,
                            IsDouble = false,
                            ULongValue = buffer[j],
                            Usage = usage_type
                        });
                    }
                }

                ses.Add(new SessionCounters()
                {
                    Index = i,
                    Counters = cv.ToArray()
                });
            }
            return ses.ToArray();
        }

        public Pass StartPass()
        {
            PassIndex++;
            var ret = Binding.GLBeginCommandListGPA(id, (uint)PassIndex, out var cmd_list_id);
            return new Pass((uint)PassIndex, cmd_list_id);
        }

        public void EndPass(Pass pass)
        {
            var ret = Binding.GLEndCommandListGPA(pass.list_id);
            if (ret != 0)
                throw new Exception("GPA Failed.");
            while (Binding.IsPassCompleteGPA(id, (uint)PassIndex) != 0)
            {
                System.Threading.Thread.Sleep(1);
            }
        }

        public void Dispose()
        {
            Binding.DeleteSessionGPA(id);
        }
    }
}