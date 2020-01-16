using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPUPerfAPI.NET
{
    public class Pass
    {
        internal ulong list_id;
        private ulong session_id;
        private uint pass_idx;

        public int SampleIndex { get; private set; }

        internal Pass(uint pass_idx, ulong list_id, ulong sess_id)
        {
            this.list_id = list_id;
            this.pass_idx = pass_idx;
            this.session_id = sess_id;
            SampleIndex = -1;
        }
        public void BeginSample()
        {
            SampleIndex = 0;
            Binding.BeginSampleGPA((uint)SampleIndex, list_id);
        }

        public void EndSample()
        {
            Binding.EndSampleGPA(list_id);
        }

        public SessionCounters[] GetResults()
        {
            var ses = new List<SessionCounters>();
            for (uint i = 0; i < SampleIndex; i++)
            {
                var cv = new List<CounterValue>();
                Binding.GetSampleResultSizeGPA(session_id, i, out var sz);
                var buffer = new ulong[sz / sizeof(ulong)];
                Binding.GetSampleResultGPA(session_id, i, sz, buffer);

                Binding.GetNumEnabledCountersGPA(session_id, out var num_cntrs);
                for (uint j = 0; j < num_cntrs; j++)
                {
                    var sb = new StringBuilder(512);
                    Binding.GetEnabledIndexGPA(session_id, j, out var enabled_idx);
                    Binding.GetCounterNameGPA(enabled_idx, sb);
                    Binding.GetCounterUsageTypeGPA(enabled_idx, out var usage_type);
                    Binding.GetCounterDataTypeGPA(enabled_idx, out var data_type);

                    if (data_type == DataType.Double64)
                    {
                        cv.Add(new CounterValue()
                        {
                            Name = sb.ToString(),
                            IsDouble = true,
                            DoubleValue = BitConverter.ToDouble(BitConverter.GetBytes(buffer[j]), 0),
                            Usage = usage_type
                        });
                    }
                    else if (data_type == DataType.Ulong64)
                    {
                        cv.Add(new CounterValue()
                        {
                            Name = sb.ToString(),
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
    }
}
