using RadeonRaysSharp.Raw;
using System;
using System.Collections.Generic;
using System.Text;
using static RadeonRaysSharp.Raw.RadeonRays;

namespace Kokoro.Graphics.Framegraph
{
    public class RayPass : ComputePass
    {
        public RayPass(string name) : base(name)
        {
        }
    }
}
