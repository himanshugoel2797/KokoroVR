using Kokoro.Graphics;
using Kokoro.Math;
using KokoroVR2.Graphics;
using Kokoro.Graphics.Framegraph;
//using KokoroVR2.Graphics.Voxel;
using System;
using KokoroVR2.Graphics.Planet;

namespace KokoroVR2.Test
{
    class Program
    {
        static SpecializedShader vertS, fragS;
        static TerrainFace[] face;
        static Image[] depthImages;
        static ImageView[] depthImageViews;
        static void Main(string[] args)
        {
            Engine.AppName = "Test";
            Engine.EnableValidation = true;
            Engine.Initialize();

            var graph = new FrameGraph(0);
            Engine.RenderGraph = graph;


            vertS = new SpecializedShader()
            {
                Name = "FST_Vert",
                Shader = ShaderSource.Load(ShaderType.VertexShader, "FullScreenTriangle/vertex.glsl"),
                SpecializationData = null
            };

            fragS = new SpecializedShader()
            {
                Name = "UVR_Frag",
                Shader = ShaderSource.Load(ShaderType.FragmentShader, "UVRenderer/fragment.glsl"),
                SpecializationData = null,
            };

            depthImages = new Image[GraphicsDevice.MaxFramesInFlight];
            depthImageViews = new ImageView[GraphicsDevice.MaxFramesInFlight];

            for (int i = 0; i < GraphicsDevice.MaxFramesInFlight; i++)
            {
                depthImages[i] = new Image()
                {
                    Name = $"depthImage_{i}",
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

                depthImageViews[i] = new ImageView()
                {
                    Name = $"depthImageView_{i}",
                    BaseLayer = 0,
                    BaseLevel = 0,
                    Format = ImageFormat.Depth32f,
                    LayerCount = 1,
                    LevelCount = 1,
                    ViewType = ImageViewType.View2D,
                };
                depthImageViews[i].Build(depthImages[i]);
            }

            face = new TerrainFace[6];
            for (int i = 0; i < 6; i++)
                face[i] = new TerrainFace("terrain_" + i, (TerrainFaceIndex)i, 100);

            Engine.OnRebuildGraph += Engine_OnRebuildGraph;
            Engine.OnRender += Engine_OnRender;
            Engine.OnUpdate += Engine_OnUpdate;
            Engine.Start(0);
        }

        private static void Engine_OnUpdate(double time_ms, double delta_ms)
        {
            //dictionary.Update();
            //streamer.InitialUpdate(delta_ms);
            //obj.Render(delta_ms);
            //streamer.FinalUpdate(delta_ms);

            for (int i = 0; i < 6; i++)
                face[i].Update();
        }

        private static void Engine_OnRender(double time_ms, double delta_ms)
        {
            //Acquire the frame
            GraphicsDevice.AcquireFrame();

            Engine.RenderGraph.QueueOp(new GpuOp()
            {
                ColorAttachments = new string[] { GraphicsDevice.DefaultFramebuffer[GraphicsDevice.CurrentFrameID].ColorAttachments[0].Name },
                DepthAttachment = depthImageViews[GraphicsDevice.CurrentFrameID].Name,
                PassName = "main_pass",
                Resources = new string[]{
                        Engine.GlobalParameters.Name
                    },
                Cmd = GpuCmd.Draw,
                VertexCount = 3,
            });

            for (int i = 0; i < 6; i++)
                face[i].Render(GraphicsDevice.DefaultFramebuffer[GraphicsDevice.CurrentFrameID].ColorAttachments[0].Name, depthImageViews[GraphicsDevice.CurrentFrameID].Name);

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
                        }
                    },
                    PushConstants = null,
                },
                Resources = new ResourceUsageEntry[]{
                    new BufferUsageEntry(){
                        StartStage = PipelineStage.VertShader,
                        StartAccesses = AccessFlags.ShaderRead,
                        FinalStage = PipelineStage.VertShader,
                        FinalAccesses = AccessFlags.None
                    }
                }
            };
            graph.RegisterGraphicsPass(gpass);

            for (int i = 0; i < 6; i++)
                face[i].RebuildGraph();
            graph.GatherDescriptors();
        }
    }
}
