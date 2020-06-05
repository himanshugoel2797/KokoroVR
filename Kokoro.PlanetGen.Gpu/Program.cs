using Kokoro.Graphics;
using Kokoro.Math;
using KokoroVR2.Graphics;
using Kokoro.Graphics.Framegraph;
using System;
using KokoroVR2.Graphics.Planet;
using System.IO;
using KokoroVR2;

namespace Kokoro.PlanetGen.Gpu
{
    class Program
    {
        static SpecializedShader vertS, fragS;
        static Image[] depthImages;
        static ImageView[] depthImageViews;

        static GpuBuffer dropletCache;
        static HeightMapGen heightMap;

        const uint dropletCount = 1024;
        const uint terrainSide = 4096;

        static void Main(string[] args)
        {
            Engine.AppName = "Kokoro.PlanetGen.Gpu";
            Engine.EnableValidation = true;
            Engine.RebuildShaders = true;
            Engine.Initialize();

            var graph = new FrameGraph(0);
            Engine.RenderGraph = graph;

            dropletCache = new GpuBuffer("dropletCache")
            {
                Usage = BufferUsage.Storage | BufferUsage.TransferSrc,
                MemoryUsage = MemoryUsage.GpuOnly,
                Mapped = false,
            };
            dropletCache.Build(0);

            //Generate a heightmap
            heightMap = new HeightMapGen("heightMap", terrainSide);

            //Populate particle buffer
            //Iterate particle simulation

            vertS = new SpecializedShader()
            {
                Name = "FST_Vert",
                Shader = ShaderSource.Load(ShaderType.VertexShader, "FullScreenTriangle/vertex.glsl"),
                SpecializationData = null
            };

            fragS = new SpecializedShader()
            {
                Name = "UVR_Frag",
                Shader = ShaderSource.Load(ShaderType.FragmentShader, "FullScreenTriangle/clear_frag.glsl"),
                SpecializationData = null,
            };

            depthImages = new Image[GraphicsDevice.MaxFramesInFlight];
            depthImageViews = new ImageView[GraphicsDevice.MaxFramesInFlight];
            for (int i = 0; i < GraphicsDevice.MaxFramesInFlight; i++)
            {
                depthImages[i] = new Image($"depthImage_{i}")
                {
                    Cubemappable = false,
                    Width = GraphicsDevice.Width,
                    Height = GraphicsDevice.Height,
                    Depth = 1,
                    Dimensions = 2,
                    InitialLayout = ImageLayout.Undefined,
                    Layers = 1,
                    Levels = 1,
                    MemoryUsage = MemoryUsage.GpuOnly,
                    Usage = ImageUsage.DepthAttachment | ImageUsage.Sampled,
                    Format = ImageFormat.Depth32f,
                };
                depthImages[i].Build(0);

                depthImageViews[i] = new ImageView($"depthImageView_{i}")
                {
                    BaseLayer = 0,
                    BaseLevel = 0,
                    Format = ImageFormat.Depth32f,
                    LayerCount = 1,
                    LevelCount = 1,
                    ViewType = ImageViewType.View2D,
                };
                depthImageViews[i].Build(depthImages[i]);
            }

            Engine.OnRebuildGraph += Engine_OnRebuildGraph;
            Engine.OnRender += Engine_OnRender;
            Engine.OnUpdate += Engine_OnUpdate;
            Engine.Start(0);
        }

        private static void Engine_OnUpdate(double time_ms, double delta_ms)
        {

        }

        private static void Engine_OnRender(double time_ms, double delta_ms)
        {
            //Acquire the frame
            GraphicsDevice.AcquireFrame();

            heightMap.Generate();
            Engine.RenderGraph.QueueOp(new GpuOp()
            {
                ColorAttachments = new string[] { GraphicsDevice.DefaultFramebuffer[GraphicsDevice.CurrentFrameID].ColorAttachments[0].Name },
                DepthAttachment = depthImageViews[GraphicsDevice.CurrentFrameID].Name,
                PassName = "main_pass",
                Resources = new string[]{
                        Engine.GlobalParameters.Name,
                    },
                Cmd = GpuCmd.Draw,
                VertexCount = 3,
            });
            Engine.RenderGraph.Build();

            GraphicsDevice.PresentFrame();
        }

        private static void Engine_OnRebuildGraph()
        {
            var graph = Engine.RenderGraph;

            for (int i = 0; i < GraphicsDevice.MaxFramesInFlight; i++)
            {
                var out_img = GraphicsDevice.DefaultFramebuffer[i].ColorAttachments[0];
                graph.RegisterResource(out_img);
                graph.RegisterResource(depthImageViews[i]);
            }

            heightMap.RebuildGraph();

            graph.RegisterShader(vertS);
            graph.RegisterShader(fragS);

            var gpass = new GraphicsPass("main_pass")
            {
                Shaders = new string[] { vertS.Name, fragS.Name },
                ViewportWidth = GraphicsDevice.Width,
                ViewportHeight = GraphicsDevice.Height,
                ViewportDynamic = false,
                DepthWriteEnable = true,
                DepthTest = DepthTest.Always,
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
                            LoadOp = AttachmentLoadOp.DontCare,
                            StoreOp = AttachmentStoreOp.Store,
                        },
                    },
                    Depth = new RenderLayoutEntry()
                    {
                        DesiredLayout = ImageLayout.DepthAttachmentOptimal,
                        FirstLoadStage = PipelineStage.EarlyFragTests,
                        Format = ImageFormat.Depth32f,
                        LastStoreStage = PipelineStage.LateFragTests,
                        LoadOp = AttachmentLoadOp.DontCare,
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
                }
            };
            graph.RegisterGraphicsPass(gpass);

            graph.GatherDescriptors();
        }
    }
}
