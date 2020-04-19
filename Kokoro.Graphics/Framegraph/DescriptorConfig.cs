using Kokoro.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kokoro.Graphics.Framegraph
{
    public class DescriptorConfig
    {
        public uint Index { get; set; }
        public uint Count { get; set; }
        public DescriptorType DescriptorType { get; set; }
        public Sampler ImmutableSampler { get; set; }
    }

    public class PushConstantConfig
    {
        public uint Offset { get; set; }
        public uint Size { get; set; }
        public ShaderType Stages { get; set; }
    }

    public class DescriptorSetup
    {
        public DescriptorConfig[] Descriptors { get; set; }
        public PushConstantConfig[] PushConstants { get; set; }
    }
}
