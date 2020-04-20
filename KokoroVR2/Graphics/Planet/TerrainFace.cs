using System;
using Kokoro.Math;
using Kokoro.Graphics;
using Kokoro.Common;
using Kokoro.Graphics.Framegraph;

namespace KokoroVR2.Graphics.Planet
{
    public enum TerrainFaceIndex
    {
        Top = 0,
        Front = 1,
        Left = 2,
        Bottom = 3,
        Back = 4,
        Right = 5,
    }

    public class TerrainFace : UniquelyNamedObject
    {
        StreamableBuffer indexBuffer;
        StreamableBuffer paramBuffer;
        SpecializedShader vertexShader;
        SpecializedShader fragmentShader;
        uint indexCount;

        public TerrainFace(string name, TerrainFaceIndex faceIndex, float radius) : base(name)
        {
            TerrainTileMesh.Create(2, TerrainTileEdge.None, out var indices);
            indexBuffer = new StreamableBuffer(name + "_indices", (ulong)indices.Length * sizeof(uint), BufferUsage.Index);
            paramBuffer = new StreamableBuffer(name + "_params", 4096, BufferUsage.Uniform);

            indexCount = (uint)indices.Length;

            unsafe
            {
                var i_ptr = (uint*)indexBuffer.BeginBufferUpdate();
                for (int i = 0; i < indices.Length; i++)
                    i_ptr[i] = indices[i];
                indexBuffer.EndBufferUpdate();
            }

            vertexShader = new SpecializedShader()
            {
                Name = Name + "_Vert",
                Shader = ShaderSource.Load(ShaderType.VertexShader, "PlanetTerrain/vertex.glsl"),
                SpecializationData = null
            };

            fragmentShader = new SpecializedShader()
            {
                Name = Name + "_Frag",
                Shader = ShaderSource.Load(ShaderType.FragmentShader, "UVRenderer/fragment.glsl"),
                SpecializationData = null,
            };
        }

        public void RebuildGraph()
        {
            indexBuffer.RebuildGraph();
            paramBuffer.RebuildGraph();

            Engine.RenderGraph.RegisterShader(vertexShader);
            Engine.RenderGraph.RegisterShader(fragmentShader);

            Engine.RenderGraph.RegisterGraphicsPass(new GraphicsPass(Name + "_pass")
            {
                Shaders = new string[] { vertexShader.Name, fragmentShader.Name },
                ViewportWidth = Engine.Width,
                ViewportHeight = Engine.Height,
                ViewportDynamic = false,
                DepthWriteEnable = true,
                DepthTest = DepthTest.Greater,
                CullMode = CullMode.Back,
                ViewportMinDepth = 0,
                ViewportMaxDepth = 1,
                RenderLayout = new RenderLayout()
                {
                    Color = new RenderLayoutEntry[]
                    {
                        new RenderLayoutEntry()
                        {
                            DesiredLayout = ImageLayout.ColorAttachmentOptimal,
                            FirstLoadStage = PipelineStage.ColorAttachOut,
                            Format = GraphicsDevice.DefaultFramebuffer[GraphicsDevice.CurrentFrameID].ColorAttachments[0].Format,
                            LastStoreStage = PipelineStage.ColorAttachOut,
                            LoadOp = AttachmentLoadOp.Clear,
                            StoreOp = AttachmentStoreOp.Store,
                        },
                    },
                    Depth = new RenderLayoutEntry()
                    {
                        DesiredLayout = ImageLayout.DepthAttachmentOptimal,
                        FirstLoadStage = PipelineStage.EarlyFragTests,
                        Format = ImageFormat.Depth32f,
                        LastStoreStage = PipelineStage.ColorAttachOut,
                        LoadOp = AttachmentLoadOp.Clear,
                        StoreOp = AttachmentStoreOp.Store,
                    },
                },
                DescriptorSetup = new DescriptorSetup()
                {
                    Descriptors = new DescriptorConfig[]{
                        new DescriptorConfig(){
                            Count = 1,
                            Index = 0,
                            DescriptorType = DescriptorType.UniformBuffer,
                        },
                        new DescriptorConfig(){
                            Count = 1,
                            Index = 1,
                            DescriptorType = DescriptorType.UniformBuffer
                        },
                    },
                    PushConstants = null,
                },
                Resources = new ResourceUsageEntry[]{
                    new BufferUsageEntry(){
                        StartStage = PipelineStage.VertShader,
                        StartAccesses = AccessFlags.ShaderRead,
                        FinalStage = PipelineStage.VertShader,
                        FinalAccesses = AccessFlags.None
                    },
                    new BufferUsageEntry(){
                        StartStage = PipelineStage.VertShader,
                        StartAccesses = AccessFlags.ShaderRead,
                        FinalStage = PipelineStage.FragShader,
                        FinalAccesses = AccessFlags.None,
                    },
                }
            });
        }

        public void Update()
        {
        }

        public void Render(string targetImage, string depthTarget)
        {
            indexBuffer.Update();
            paramBuffer.Update();
            Engine.RenderGraph.QueueOp(new GpuOp()
            {
                ColorAttachments = new string[] { targetImage },
                DepthAttachment = depthTarget,
                PassName = Name + "_pass",
                Resources = new string[]{
                        Engine.GlobalParameters.Name,
                        paramBuffer.Name,
                    },
                Cmd = GpuCmd.DrawIndexed,
                IndexCount = indexCount,
                IndexBuffer = indexBuffer.Name,
                IndexType = IndexType.U32,
            });
        }
    }
}
