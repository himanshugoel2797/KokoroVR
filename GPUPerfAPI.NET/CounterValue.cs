using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPUPerfAPI.NET
{
    public struct SessionCounters
    {
        public uint Index { get; set; }
        public CounterValue[] Counters { get; set; }
    }

    public struct CounterValue
    {
        public string Name { get; set; }
        public bool IsDouble { get; set; }
        public UsageType Usage { get; set; }
        public double DoubleValue { get; set; }
        public ulong ULongValue { get; set; }
    }
}
