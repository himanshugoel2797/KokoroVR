using Kokoro.Graphics;
using System.Collections.Generic;
using System.Text;
using Kokoro.Common;

namespace Kokoro.Graphics.Framegraph
{
    public enum ResourceKind
    {
        None = 0,
        Buffer = 1,
        BufferView = 2,
        ImageView = 3,
    }

    public abstract class ResourceUsageEntry
    {
        public ResourceKind Kind { get; protected set; }
        public PipelineStage StartStage { get; set; }
        public AccessFlags StartAccesses { get; set; }

        public PipelineStage FinalStage { get; set; }
        public AccessFlags FinalAccesses { get; set; }
    }

    public class BufferUsageEntry : ResourceUsageEntry
    {
        public BufferUsageEntry()
        {
            Kind = ResourceKind.Buffer;
        }
    }

    public class BufferViewUsageEntry : ResourceUsageEntry
    {
        public BufferViewUsageEntry()
        {
            Kind = ResourceKind.BufferView;
        }
    }

    public class ImageViewUsageEntry : ResourceUsageEntry
    {
        public ImageLayout StartLayout { get; set; }
        public ImageLayout FinalLayout { get; set; }
        public ImageViewUsageEntry()
        {
            Kind = ResourceKind.ImageView;
        }
    }

    public class GraphicsPass : UniquelyNamedObject
    {
        public string[] Shaders { get; set; }
        public ResourceUsageEntry[] Resources { get; set; }
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

        public GraphicsPass(string name) : base(name) { }
    }
}
