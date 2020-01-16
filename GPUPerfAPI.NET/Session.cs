using System;

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
            Binding.EnableAllCountersGPA(id);
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
            Binding.DisableAllCountersGPA(id);
        }

        public void DisableCounter(string name)
        {
            Binding.DisableCounterByNameGPA(id, name);
        }

        public void Start()
        {
            PassIndex = -1;
            Binding.BeginSessionGPA(id);
        }

        public void Stop()
        {
            Binding.EndSessionGPA(id);
        }

        public Pass StartPass()
        {
            PassIndex++;
            Binding.GLBeginCommandListGPA(id, (uint)PassIndex, out var cmd_list_id);
            return new Pass((uint)PassIndex, cmd_list_id);
        }

        public void EndPass(Pass pass)
        {
            Binding.GLEndCommandListGPA(pass.list_id);
            while(Binding.IsPassCompleteGPA(id, (uint)PassIndex) != 0)
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