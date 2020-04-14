using System;
using System.Collections.Generic;
using System.Text;

namespace Kokoro.Graphics
{
    public struct ShaderResourceSetReference
    {
        public string Name { get; set; }
        public ShaderType[] ReadStages { get; set; }
        public ShaderType[] WriteStage { get; set; }
    }

    public class GraphicsRenderState
    {
        public GraphicsRenderState(ShaderSource[] shaders, PrimitiveType topology, bool depthClamp, bool rasterizerDiscard, CullMode cullMode, DepthTest depthTest, ShaderResourceSetReference[] resourceSets)
        {
            Shaders = shaders ?? throw new ArgumentNullException(nameof(shaders));
            Topology = topology;
            DepthClamp = depthClamp;
            RasterizerDiscard = rasterizerDiscard;
            CullMode = cullMode;
            DepthTest = depthTest;
            ResourceSets = resourceSets ?? throw new ArgumentNullException(nameof(resourceSets));
        }

        public ShaderSource[] Shaders { get; private set; }
        public PrimitiveType Topology { get; private set; }
        public bool DepthClamp { get; private set; }
        public bool RasterizerDiscard { get; private set; }
        public CullMode CullMode { get; private set; }
        public DepthTest DepthTest { get; private set; }
        public ShaderResourceSetReference[] ResourceSets { get; private set; }
    }
}
