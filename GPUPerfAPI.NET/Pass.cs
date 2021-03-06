﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPUPerfAPI.NET
{
    public class Pass
    {
        internal ulong list_id;
        private uint pass_idx;

        public int SampleIndex { get; private set; }
        public bool IsSampling { get; private set; }
        internal Pass(uint pass_idx, ulong list_id)
        {
            this.list_id = list_id;
            this.pass_idx = pass_idx;
            SampleIndex = -1;
        }
        public void BeginSample()
        {
            SampleIndex++;
            IsSampling = true;
            var ret = Binding.BeginSampleGPA((uint)SampleIndex, list_id);
            if (ret != 0)
                throw new Exception("GPA Failed.");
        }

        public void EndSample()
        {
            var ret = Binding.EndSampleGPA(list_id);
            if (ret != 0)
                throw new Exception("GPA Failed.");
            IsSampling = false;
        }


    }
}
