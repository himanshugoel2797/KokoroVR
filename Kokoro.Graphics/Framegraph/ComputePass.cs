using Kokoro.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kokoro.Graphics.Framegraph
{
    public class ComputePass : UniquelyNamedObject
    {
        public ComputePass(string name) : base(name)
        {
        }

        public bool IsAsync { get; set; }
        public string Shader { get; set; }
        public DescriptorSetup DescriptorSetup { get; set; }
        public ResourceUsageEntry[] Resources { get; set; }
    }
}
