using System;
using System.Collections.Generic;
using System.Text;

namespace Kokoro.Graphics.Framegraph
{
    public abstract class ComputePass
    {
        public string Name { get; set; }

        public bool IsAsync { get; set; }
        public string Shader { get; set; }
        public DescriptorSetup DescriptorSetup { get; set; }
    }
}
