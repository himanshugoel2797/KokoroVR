using Kokoro.Graphics.Framegraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics.VulkanTest
{
    class Program
    {
        static FrameGraph graph;
        static void Main(string[] args)
        {
            GraphicsDevice.AppName = "Vulkan Test";
            GraphicsDevice.EnableValidation = true;
            GraphicsDevice.RebuildShaders = true;
            GraphicsDevice.Init();

            graph = new FrameGraph(0);

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

            for (int i = 0; i < GraphicsDevice.MaxFramesInFlight; i++) {
                var out_img = GraphicsDevice.DefaultFramebuffer[i].ColorAttachments[0];
                graph.RegisterResource(out_img);
            }

            var gpass = new GraphicsPass()
            {
                Name = "main_pass",
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

            GraphicsDevice.Window.Render += Window_Render;
            GraphicsDevice.Window.Run(60);

            /*
            var fbuf = new Framegraph(0);
            fbuf.RegisterAttachment(new AttachmentInfo()
            {
                Name = "output",
                BaseSize = SizeClass.ScreenRelative,
                SizeX = 1,
                SizeY = 1,
                Format = ImageFormat.B8G8R8A8Unorm,
                Layers = 1,
                UseMipMaps = false,
                Usage = ImageUsage.ColorAttachment | ImageUsage.Sampled,
            });
            fbuf.RegisterAttachment(new AttachmentInfo()
            {
                Name = "output_dpth",
                BaseSize = SizeClass.ScreenRelative,
                SizeX = 1,
                SizeY = 1,
                Format = ImageFormat.Depth32f,
                Layers = 1,
                UseMipMaps = false,
                Usage = ImageUsage.DepthAttachment | ImageUsage.TransferSrc,
            });
            fbuf.RegisterShaderParams(new ShaderParameterSet()
            {
                Name = "output_shader_params",
                Buffers = null,
                SampledAttachments = null,
                Textures = null,
            });
            fbuf.RegisterPass(new GraphicsPass()
            {
                Name = "main_pass",
                CullMode = CullMode.None,
                DepthAttachment = new AttachmentUsageInfo()
                {
                    Name = "output_dpth",
                    Usage = AttachmentUsage.WriteOnly
                },
                AttachmentUsage = new AttachmentUsageInfo[]{
                    new AttachmentUsageInfo(){
                        Name = "output",
                        Usage = AttachmentUsage.WriteOnly
                    }
                },
                DepthClamp = false,
                DepthTest = DepthTest.Always,
                EnableBlending = false,
                LineWidth = 1,
                PassDependencies = null,
                RasterizerDiscard = false,
                Shaders = new ShaderSource[] { vert, frag },
                Topology = PrimitiveType.Triangle,
                DrawCmd = new PlainDrawCmd()
                {
                    BaseInstance = 0,
                    BaseVertex = 0,
                    InstanceCount = 1,
                    VertexCount = 3
                }
            });
            fbuf.SetOutputPass("output");
            fbuf.Compile();
            while (!GraphicsDevice.Window.IsExiting)
            {
                fbuf.Execute(true);
                GraphicsDevice.Window.PollEvents();
            }*/
        }

        private static void Window_Render(double time_ms, double delta_ms)
        {
            //Acquire the frame
            GraphicsDevice.AcquireFrame();

            graph.QueueOp(new GpuOp()
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

            graph.Build();
            GraphicsDevice.PresentFrame();
        }
    }
}
