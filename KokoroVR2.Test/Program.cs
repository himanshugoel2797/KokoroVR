using Kokoro.Graphics;
using System;

namespace KokoroVR2.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Engine.AppName = "Test";
            Engine.EnableValidation = true;
            Engine.Initialize();
            Engine.OnRebuildGraph += Engine_OnRebuildGraph;
            Engine.Start(0);
        }

        private static void Engine_OnRebuildGraph(double time_ms, double delta_ms)
        {
            ShaderSource vert = ShaderSource.Load(ShaderType.VertexShader, "FullScreenTriangle/vertex.glsl");
            ShaderSource frag = ShaderSource.Load(ShaderType.FragmentShader, "UVRenderer/fragment.glsl");

            var fbuf = Engine.Graph;
            fbuf.RegisterAttachment(new AttachmentInfo()
            {
                Name = "output",
                BaseSize = SizeClass.ScreenRelative,
                SizeX = 1,
                SizeY = 1,
                Format = ImageFormat.B8G8R8A8Unorm,
                Layers = 1,
                Levels = 1,
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
                Levels = 1,
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
                PassDependencies = new string[] { Engine.Graph.GlobalParametersName },
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
            fbuf.SetOutputPass("main_pass", "output");
        }
    }
}
