using Kokoro.Graphics;
using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Text;

namespace KokoroVR.Graphics
{
    public class DeferredRenderer : IRenderer
    {
        private class ViewData
        {
            public Framebuffer gbuffer;
            public Texture infoTex;
            public TextureView infoView;
            public Texture depthBuf;
            public TextureView depthView;
            public RenderState state;
            public ShaderProgram program;
        }

        public Framebuffer[] Framebuffers { get; }

        private LightManager lMan;
        private CommandBuffer cBuffer;
        private ViewData[] views;

        //Lighting is purely photon mapping
        //Multiple passes:
        // - Process bundles of photons on the cpu
        // - Precompute BRDF per material in a table, or compute them on the gpu
        // - double buffer photon map data in gpu mapped memory
        // - split across all cores with explicit simd
        // - gpu side reads the photon map and applies lighting to the voxel face based on the voxel's ID
        // - voxel ID = 14:6:6:6, may consider 64-bit ID

        public DeferredRenderer(Framebuffer[] fbufs, LightManager man)
        {
            Framebuffers = fbufs;
            lMan = man;
            cBuffer = new CommandBuffer();
            
            views = new ViewData[fbufs.Length];
            for (int i = 0; i < views.Length; i++)
            {
                views[i] = new ViewData()
                {
                    depthBuf = new Texture()
                    {
                        Width = fbufs[i].Width,
                        Height = fbufs[i].Height,
                        Depth = 1,
                        Format = PixelInternalFormat.DepthComponent32f,
                        GenerateMipmaps = false,
                        LayerCount = 1,
                        LevelCount = 1,
                        Target = TextureTarget.Texture2D
                    }.Build(),
                    infoTex = new Texture()
                    {
                        Width = fbufs[i].Width,
                        Height = fbufs[i].Height,
                        Depth = 1,
                        Format = PixelInternalFormat.Rgba16f,
                        GenerateMipmaps = false,
                        LayerCount = 1,
                        LevelCount = 1,
                        Target = TextureTarget.Texture2D
                    }.Build(),
                    gbuffer = new Framebuffer(fbufs[i].Width, fbufs[i].Height),
                };

                views[i].infoView = new TextureView()
                {
                    BaseLayer = 0,
                    BaseLevel = 0,
                    Format = PixelInternalFormat.Rgba16f,
                    LayerCount = 1,
                    LevelCount = 1,
                    Target = TextureTarget.Texture2D
                }.Build(views[i].infoTex);

                views[i].depthView = new TextureView()
                {
                    BaseLayer = 0,
                    BaseLevel = 0,
                    Format = PixelInternalFormat.DepthComponent32f,
                    LayerCount = 1,
                    LevelCount = 1,
                    Target = TextureTarget.Texture2D
                }.Build(views[i].depthBuf);

                views[i].gbuffer[FramebufferAttachment.ColorAttachment0] = views[i].infoView;
                views[i].gbuffer[FramebufferAttachment.DepthAttachment] = views[i].depthView;

                views[i].program = new ShaderProgram(
                    ShaderSource.Load(ShaderType.VertexShader, "RenderToTexture/FrameBufferTriangle/vertex.glsl"), 
                    ShaderSource.Load(ShaderType.FragmentShader, "RenderToTexture/FrameBufferTriangle/fragment.glsl")
                    );

                views[i].state = new RenderState(fbufs[i], views[i].program, null, null, false, true, DepthFunc.Always, InverseDepth.Far, InverseDepth.Near, BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.None);
                //Set the appropriate texture handles in a ubo

                cBuffer.SetRenderState(views[i].state);
                cBuffer.Draw(PrimitiveType.Triangles, 0, 3, 1, 0);
            }
        }

        public void Clear()
        {

        }

        public void Submit(Matrix4[] v, Matrix4[] p, Vector3 position)
        {
            //Set v[], p[] and position in uniform buffers
            cBuffer.Submit();
        }
    }
}
