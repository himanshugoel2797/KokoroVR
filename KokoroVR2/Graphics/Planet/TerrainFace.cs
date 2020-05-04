using System.Runtime.InteropServices;
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
        Bottom = 1,
        Front = 2,
        Back = 3,
        Left = 4,
        Right = 5,
    }

    public class TerrainFace : UniquelyNamedObject
    {
        static StreamableBuffer indexBuffer;
        static StreamableBuffer paramBuffer;
        static SpecializedShader vertexShader;
        static SpecializedShader fragmentShader;
        static uint indexCount;
        static int cntr = 0;

        readonly IntPtr pushConstants;
        readonly uint pushConstantsLen;
        readonly int cur_cntr;
        readonly TerrainCache cache;
        Vector3 normal;

        public string heightDataBufferName;
        private static void Initialize(string name)
        {
            if (cntr == 0)
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
                    Name = name + "_Vert",
                    Shader = ShaderSource.Load(ShaderType.VertexShader, "PlanetTerrain/vertex.glsl"),
                    SpecializationData = null
                };

                fragmentShader = new SpecializedShader()
                {
                    Name = name + "_Frag",
                    Shader = ShaderSource.Load(ShaderType.FragmentShader, "UVRenderer/fragment.glsl"),
                    SpecializationData = null,
                };
            }
        }

        public TerrainFace(string name, TerrainFaceIndex faceIndex, float radius, TerrainCache cache) : base(name)
        {
            Initialize("TerrainFace");
            cur_cntr = cntr++;
            this.cache = cache;
            uint v0 = 0, v1 = 0, v2 = 0;

            switch (faceIndex)
            {
                case TerrainFaceIndex.Top:
                    normal = new Vector3(0, 1, 0);
                    v0 = 0;
                    v1 = 2;
                    v2 = 1;
                    break;
                case TerrainFaceIndex.Bottom:
                    normal = new Vector3(0, -1, 0);
                    v0 = 0;
                    v1 = 2;
                    v2 = 1;
                    break;
                case TerrainFaceIndex.Front:
                    normal = new Vector3(0, 0, 1);
                    v0 = 0;
                    v1 = 1;
                    v2 = 2;
                    break;
                case TerrainFaceIndex.Back:
                    normal = new Vector3(0, 0, -1);
                    v0 = 0;
                    v1 = 1;
                    v2 = 2;
                    break;
                case TerrainFaceIndex.Left:
                    normal = new Vector3(1, 0, 0);
                    v0 = 1;
                    v1 = 2;
                    v2 = 0;
                    break;
                case TerrainFaceIndex.Right:
                    normal = new Vector3(-1, 0, 0);
                    v0 = 1;
                    v1 = 2;
                    v2 = 0;
                    break;
            }

            unsafe
            {
                pushConstantsLen = 4 * 4 + 4 * 4 + 4;
                pushConstants = Marshal.AllocHGlobal((int)pushConstantsLen);
                float* f_ptr = (float*)pushConstants;
                uint* ui_ptr = (uint*)f_ptr;
                f_ptr[0] = normal.X;
                f_ptr[1] = normal.Y;
                f_ptr[2] = normal.Z;
                f_ptr[3] = 0.0f;
                ui_ptr[4] = v0;
                ui_ptr[5] = v1;
                ui_ptr[6] = v2;
                f_ptr[7] = radius;
                ui_ptr[8] = (uint)faceIndex * 2049 * 2049;
            }
        }

        public void RebuildGraph()
        {
            if (cur_cntr == 0)
            {
                indexBuffer.RebuildGraph();
                paramBuffer.RebuildGraph();

                Engine.RenderGraph.RegisterShader(vertexShader);
                Engine.RenderGraph.RegisterShader(fragmentShader);

                Engine.RenderGraph.RegisterGraphicsPass(new GraphicsPass("TerrainFace_pass")
                {
                    Shaders = new string[] { vertexShader.Name, fragmentShader.Name },
                    ViewportWidth = Engine.Width,
                    ViewportHeight = Engine.Height,
                    ViewportDynamic = false,
                    DepthWriteEnable = true,
                    DepthTest = DepthTest.Greater,
                    CullMode = CullMode.None,
                    Fill = FillMode.Fill,
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
                            LoadOp = AttachmentLoadOp.Load,
                            StoreOp = AttachmentStoreOp.Store,
                        },
                        },
                        Depth = new RenderLayoutEntry()
                        {
                            DesiredLayout = ImageLayout.DepthAttachmentOptimal,
                            FirstLoadStage = PipelineStage.EarlyFragTests,
                            Format = ImageFormat.Depth32f,
                            LastStoreStage = PipelineStage.LateFragTests,
                            LoadOp = AttachmentLoadOp.Load,
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
                        new DescriptorConfig()
                        {
                            Count = 1,
                            Index = 2,
                            DescriptorType = DescriptorType.StorageBuffer
                        }
                    },
                        PushConstants = new PushConstantConfig[]{
                        new PushConstantConfig(){
                            Offset = 0,
                            Size = pushConstantsLen,
                            Stages = ShaderType.All
                        },
                    },
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
                    new BufferUsageEntry()
                    {
                        StartStage = PipelineStage.VertShader,
                        StartAccesses = AccessFlags.ShaderRead,
                        FinalStage = PipelineStage.VertShader,
                        FinalAccesses = AccessFlags.None,
                    }
                }
                });
            }
        }

        public void Update()
        {
        }

        public void Render(string targetImage, string depthTarget)
        {
            if (cur_cntr == 0)
            {
                indexBuffer.Update();
                paramBuffer.Update();
            }
            Engine.RenderGraph.QueueOp(new GpuOp()
            {
                ColorAttachments = new string[] { targetImage },
                DepthAttachment = depthTarget,
                PassName = "TerrainFace_pass",
                Resources = new string[]{
                        Engine.GlobalParameters.Name,
                        paramBuffer.Name,
                        heightDataBufferName,
                    },
                Cmd = GpuCmd.DrawIndexed,
                IndexCount = indexCount,
                IndexBuffer = indexBuffer.Name,
                IndexType = IndexType.U32,
                PushConstantsLen = pushConstantsLen,
                PushConstants = pushConstants,
            });
        }
    }
}
