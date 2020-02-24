using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics.VulkanTest
{
    class Program
    {
        static void Main(string[] args)
        {
            GraphicsDevice.AppName = "Vulkan Test";
            GraphicsDevice.EnableValidation = true;
            GraphicsDevice.Init();


            ShaderSource vert = ShaderSource.Load(ShaderType.VertexShader, "FullScreenTriangle/vertex.glsl");
            ShaderSource frag = ShaderSource.Load(ShaderType.FragmentShader, "FullScreenTriangle/fragment.glsl");

            var pool = new CommandPool();
            pool.Transient = false;
            pool.Build(0, CommandQueue.Graphics);

            var pass = new RenderPass[2];
            var pipeline = new PipelineLayout[2];
            var cmdBuf = new CommandBuffer[2];

            for (int i = 0; i < 2; i++)
            {
                pass[i] = new RenderPass();
                pass[i].InitialLayout[AttachmentKind.ColorAttachment0] = ImageLayout.Undefined;
                pass[i].StartLayout[AttachmentKind.ColorAttachment0] = ImageLayout.ColorAttachmentOptimal;
                pass[i].FinalLayout[AttachmentKind.ColorAttachment0] = ImageLayout.PresentSrc;
                pass[i].LoadOp[AttachmentKind.ColorAttachment0] = AttachmentLoadOp.DoneCare;
                pass[i].StoreOp[AttachmentKind.ColorAttachment0] = AttachmentStoreOp.Store;
                pass[i].Formats[AttachmentKind.ColorAttachment0] = ImageFormat.B8G8R8A8Unorm;

                pass[i].InitialLayout[AttachmentKind.DepthAttachment] = ImageLayout.Undefined;
                pass[i].StartLayout[AttachmentKind.DepthAttachment] = ImageLayout.DepthAttachmentOptimal;
                pass[i].FinalLayout[AttachmentKind.DepthAttachment] = ImageLayout.DepthAttachmentOptimal;
                pass[i].LoadOp[AttachmentKind.DepthAttachment] = AttachmentLoadOp.DoneCare;
                pass[i].StoreOp[AttachmentKind.DepthAttachment] = AttachmentStoreOp.Store;
                pass[i].Formats[AttachmentKind.DepthAttachment] = ImageFormat.Depth32f;

                pass[i].Build(0);

                pipeline[i] = new PipelineLayout();
                pipeline[i].Framebuffer = GraphicsDevice.DefaultFramebuffer[i];
                pipeline[i].RenderPass = pass[i];
                pipeline[i].DepthTest = DepthTest.Always;
                pipeline[i].Shaders.Add(vert);
                pipeline[i].Shaders.Add(frag);
                pipeline[i].Build(0);

                cmdBuf[i] = new CommandBuffer();
                cmdBuf[i].Build(pool);
                cmdBuf[i].BeginRecording();
                cmdBuf[i].SetPipeline(pipeline[i]);
                cmdBuf[i].SetViewport(0, 0, GraphicsDevice.DefaultFramebuffer[0].Width, GraphicsDevice.DefaultFramebuffer[0].Height);
                cmdBuf[i].Draw(3, 1, 0, 0);
                cmdBuf[i].EndRenderPass();
                cmdBuf[i].EndRecording();
            }

            while (true)
            {
                GraphicsDevice.Window.PollEvents();
                GraphicsDevice.AcquireFrame();
                GraphicsDevice.SubmitCommandBuffer(cmdBuf[GraphicsDevice.CurrentFrameIndex]);
                GraphicsDevice.PresentFrame();
            }
        }
    }
}
