using Kokoro.Graphics;
using Kokoro.Math;
using KokoroVR2.Graphics;
using Kokoro.Graphics.Framegraph;
//using KokoroVR2.Graphics.Voxel;
using System;

namespace KokoroVR2.Test
{
    class Program
    {
        //static VoxelDictionary dictionary;
        //static ChunkStreamer streamer;
        //static ChunkObject obj;

        static void Main(string[] args)
        {
            Engine.AppName = "Test";
            Engine.EnableValidation = true;
            Engine.Initialize();

            var graph = new FrameGraph(0);
            Engine.RenderGraph = graph;

            var vertS = new SpecializedShader()
            {
                Name = "FST_Vert",
                Shader = ShaderSource.Load(ShaderType.VertexShader, "FullScreenTriangle/vertex.glsl"),
                SpecializationData = null
            };

            var fragS = new SpecializedShader()
            {
                Name = "UVR_Frag",
                Shader = ShaderSource.Load(ShaderType.FragmentShader, "UVRenderer/fragment.glsl"),
                SpecializationData = null,
            };

            graph.RegisterShader(vertS);
            graph.RegisterShader(fragS);

            for (int i = 0; i < GraphicsDevice.MaxFramesInFlight; i++)
            {
                var out_img = GraphicsDevice.DefaultFramebuffer[i].ColorAttachments[0];
                graph.RegisterResource(out_img);
            }

            var gpass = new GraphicsPass("main_pass")
            {
                Shaders = new string[] { vertS.Name, fragS.Name },
                ViewportWidth = GraphicsDevice.Width,
                ViewportHeight = GraphicsDevice.Height,
                ViewportDynamic = false,
                DepthWriteEnable = false,
                CullMode = CullMode.None,
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
                    Depth = null,
                },
                DescriptorSetup = new DescriptorSetup()
                {
                    Descriptors = null,
                    PushConstants = null,
                },
            };
            graph.RegisterGraphicsPass(gpass);
            graph.GatherDescriptors();

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
        }

        private static void Engine_OnRender(double time_ms, double delta_ms)
        {
            //Acquire the frame
            GraphicsDevice.AcquireFrame();

            Engine.RenderGraph.QueueOp(new GpuOp()
            {
                ColorAttachments = new string[] { GraphicsDevice.DefaultFramebuffer[GraphicsDevice.CurrentFrameID].ColorAttachments[0].Name },
                DepthAttachment = null,
                PassName = "main_pass",
                Resources = new GpuResourceRequest[]
                {
                    new GpuResourceRequest()
                    {
                        Name = GraphicsDevice.DefaultFramebuffer[GraphicsDevice.CurrentFrameID].ColorAttachments[0].Name,
                        Accesses = AccessFlags.None,
                        DesiredLayout = ImageLayout.Undefined,
                        FirstLoadStage = PipelineStage.ColorAttachOut,
                        LastStoreStage = PipelineStage.ColorAttachOut,
                        Stores = AccessFlags.ColorAttachmentWrite,
                    }
                },
                Cmd = GpuCmd.Draw,
                VertexCount = 3,
            });

            Engine.RenderGraph.Build();
            GraphicsDevice.PresentFrame();
        }

        private static void Engine_OnRebuildGraph()
        {
            //dictionary.GenerateRenderGraph();
            //streamer.GenerateRenderGraph();
        }
    }
}
