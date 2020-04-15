using Kokoro.Graphics;
using System.Collections.Generic;
using System.Text;

namespace Kokoro.Graphics.Framegraph
{

    public class GraphicsPass : UniquelyNamedObject
    {
        public string[] Shaders { get; set; }
        public DescriptorSetup DescriptorSetup { get; set; }
        public RenderLayout RenderLayout { get; set; }
        public PrimitiveType Topology { get; set; } = PrimitiveType.Triangle;
        public bool RasterizerDiscard { get; set; } = false;
        public float LineWidth { get; set; } = 1.0f;
        public CullMode CullMode { get; set; } = CullMode.Back;

        public bool EnableBlending { get; set; } = false;

        public DepthTest DepthTest { get; set; } = DepthTest.Greater;
        public bool DepthWriteEnable { get; set; } = true;
        public bool DepthClamp { get; set; } = false;

        public bool ViewportDynamic { get; set; } = false;
        public uint ViewportX { get; set; } = 0;
        public uint ViewportY { get; set; } = 0;
        public uint ViewportWidth { get; set; }
        public uint ViewportHeight { get; set; }
        public uint ViewportMinDepth { get; set; } = 0;
        public uint ViewportMaxDepth { get; set; } = 1;
    }
}
