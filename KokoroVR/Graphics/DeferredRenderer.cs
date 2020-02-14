using Kokoro.Graphics;
using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace KokoroVR.Graphics
{
    public class DeferredRenderer : IRenderer
    {
        private class ViewData
        {
            public Framebuffer gbuffer;
            public Texture infoTex;
            public TextureView infoView;
            public Texture infoTex2;
            public TextureView infoView2;
            public Texture depthBuf;
            public TextureView depthView;
            public RenderState state;
            public RenderState copyState;
            public RenderState mipchainState;
            public ShaderProgram program;
            public ShaderProgram copy;
            public ShaderProgram mipchain;
            public CommandBuffer cBuffer;
            public Texture hiZ;
            public TextureView hiZTex;
            public TextureBinding hiZBinding;
            public TextureView[] hiZView;
        }

        public Framebuffer[] Framebuffers { get; }
        public TextureBinding[] InfoBindings { get; private set; }
        public TextureBinding[] InfoBindings2 { get; private set; }
        public TextureBinding[] DepthBindings { get; private set; }
        public TextureView[][] HiZMap { get; private set; }
        public UniformBuffer HiZMapUBO { get; }

        private LightManager lMan;
        private ViewData[] views;
        private bool firstRun = true;

        //Deferred Renderer Rework - New GBuffer - 
        //32:32:32 WorldPos, (16b:SPARE, 16b:materialIdx)
        //8:8:8 Norm, 8:SPARE
        //reproject z buffer and recompute chain for culling

        public DeferredRenderer(Framebuffer[] fbufs, LightManager man)
        {
            lMan = man;

            InfoBindings = new TextureBinding[fbufs.Length];
            InfoBindings2 = new TextureBinding[fbufs.Length];
            DepthBindings = new TextureBinding[fbufs.Length];
            Framebuffers = new Framebuffer[fbufs.Length];
            HiZMap = new TextureView[fbufs.Length][];
            views = new ViewData[fbufs.Length];
            HiZMapUBO = new UniformBuffer(false);
            unsafe
            {
                int off = 0;
                float* fp = (float*)HiZMapUBO.Update();
                fp += 4;
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
                            Format = PixelInternalFormat.Rgba32f,
                            GenerateMipmaps = false,
                            LayerCount = 1,
                            LevelCount = 1,
                            Target = TextureTarget.Texture2D
                        }.Build(),
                        infoTex2 = new Texture()
                        {
                            Width = fbufs[i].Width,
                            Height = fbufs[i].Height,
                            Depth = 1,
                            Format = PixelInternalFormat.Rgba8,
                            GenerateMipmaps = false,
                            LayerCount = 1,
                            LevelCount = 1,
                            Target = TextureTarget.Texture2D
                        }.Build(),
                        hiZ = new Texture()
                        {
                            Width = fbufs[i].Width,
                            Height = fbufs[i].Height,
                            Depth = 1,
                            Format = PixelInternalFormat.Rg32f,
                            GenerateMipmaps = false,
                            LayerCount = 1,
                            LevelCount = (int)(MathHelper.Log2((ulong)Math.Max(fbufs[i].Width, fbufs[i].Height)) + 1),
                            Target = TextureTarget.Texture2D
                        }.Build(),
                        gbuffer = new Framebuffer(fbufs[i].Width, fbufs[i].Height),
                    };

                    views[i].depthView = new TextureView()
                    {
                        BaseLayer = 0,
                        BaseLevel = 0,
                        Format = PixelInternalFormat.DepthComponent32f,
                        LayerCount = 1,
                        LevelCount = 1,
                        Target = TextureTarget.Texture2D
                    }.Build(views[i].depthBuf);

                    views[i].infoView = new TextureView()
                    {
                        BaseLayer = 0,
                        BaseLevel = 0,
                        Format = PixelInternalFormat.Rgba32f,
                        LayerCount = 1,
                        LevelCount = 1,
                        Target = TextureTarget.Texture2D
                    }.Build(views[i].infoTex);

                    views[i].infoView2 = new TextureView()
                    {
                        BaseLayer = 0,
                        BaseLevel = 0,
                        Format = PixelInternalFormat.Rgba8,
                        LayerCount = 1,
                        LevelCount = 1,
                        Target = TextureTarget.Texture2D
                    }.Build(views[i].infoTex2);

                    views[i].hiZView = new TextureView[views[i].hiZ.LevelCount];
                    for (int j = 0; j < views[i].hiZView.Length; j++)
                    {
                        views[i].hiZView[j] = new TextureView()
                        {
                            BaseLayer = 0,
                            BaseLevel = j,
                            Format = PixelInternalFormat.Rg32f,
                            LayerCount = 1,
                            LevelCount = 1,
                            Target = TextureTarget.Texture2D,
                        }.Build(views[i].hiZ);
                        var f_arr = (float[])views[i].hiZView[j].GetImageHandle().SetResidency(Residency.Resident, AccessMode.ReadWrite);
                        for (int q = 0; q < f_arr.Length; q++)
                            *(fp++) = f_arr[q];
                        fp += 2;
                    }
                    views[i].hiZTex = new TextureView()
                    {
                        BaseLayer = 0,
                        BaseLevel = 0,
                        Format = PixelInternalFormat.Rg32f,
                        LayerCount = 1,
                        LevelCount = views[i].hiZView.Length,
                        Target = TextureTarget.Texture2D
                    }.Build(views[i].hiZ);

                    var sampler = new TextureSampler();
                    sampler.SetEnableLinearFilter(true, true, true);
                    sampler.SetTileMode(false, false);

                    views[i].hiZBinding = new TextureBinding()
                    {
                        View = views[i].hiZTex,
                        Sampler = sampler
                    };
                    var f_arr_ = (float[])views[i].hiZBinding.GetTextureHandle().SetResidency(Residency.Resident);
                    for (int q = 0; q < f_arr_.Length; q++)
                        *(fp++) = f_arr_[q];
                    fp += 2;

                    views[i].gbuffer[FramebufferAttachment.DepthAttachment] = views[i].depthView;
                    views[i].gbuffer[FramebufferAttachment.ColorAttachment0] = views[i].infoView;
                    views[i].gbuffer[FramebufferAttachment.ColorAttachment1] = views[i].infoView2;
                    Framebuffers[i] = views[i].gbuffer;

                    views[i].program = new ShaderProgram(
                        ShaderSource.Load(ShaderType.VertexShader, "Shaders\\RenderToTexture\\FrameBufferTriangle\\vertex.glsl"),
                        ShaderSource.Load(ShaderType.FragmentShader, "Shaders\\RenderToTexture\\FrameBufferTriangle\\fragment.glsl")
                        );
                    views[i].copy = new ShaderProgram(
                        ShaderSource.Load(ShaderType.ComputeShader, "Shaders\\HiZ\\copy.glsl", $"#define MIP_COUNT {views[i].hiZView.Length}")
                        );
                    views[i].copyState = new RenderState(null, views[i].copy, null, new UniformBuffer[] { Engine.GlobalParameters, HiZMapUBO }, false, false, DepthFunc.Always, InverseDepth.Far, InverseDepth.Near, BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.Back);
                    views[i].mipchain = new ShaderProgram(
                        ShaderSource.Load(ShaderType.ComputeShader, "Shaders\\HiZ\\mipchain.glsl", $"#define MIP_COUNT {views[i].hiZView.Length}")
                        );
                    views[i].mipchainState = new RenderState(null, views[i].mipchain, null, new UniformBuffer[] { Engine.GlobalParameters, HiZMapUBO }, false, false, DepthFunc.Always, InverseDepth.Far, InverseDepth.Near, BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.Back);

                    views[i].state = new RenderState(fbufs[i], views[i].program, null, new UniformBuffer[] { Engine.GlobalParameters }, false, true, DepthFunc.Always, InverseDepth.Far, InverseDepth.Near, BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.None);

                    InfoBindings[i] = new TextureBinding()
                    {
                        View = views[i].infoView,
                        Sampler = TextureSampler.Default
                    };
                    InfoBindings[i].GetTextureHandle().SetResidency(Residency.Resident);

                    InfoBindings2[i] = new TextureBinding()
                    {
                        View = views[i].infoView2,
                        Sampler = TextureSampler.Default
                    };
                    InfoBindings2[i].GetTextureHandle().SetResidency(Residency.Resident);

                    DepthBindings[i] = new TextureBinding()
                    {
                        View = views[i].depthView,
                        Sampler = TextureSampler.Default,
                    };
                    DepthBindings[i].GetTextureHandle().SetResidency(Residency.Resident);

                    HiZMap[i] = views[i].hiZView;

                    views[i].cBuffer = new CommandBuffer();
                    views[i].cBuffer.SetRenderState(views[i].state);
                    views[i].cBuffer.Draw(PrimitiveType.Triangles, 0, 3, 1, 0);
                }
                HiZMapUBO.UpdateDone();
            }
        }

        public void FrameStart()
        {
            //clear depth to InverseDepth.Far
            CommandBuffer tmpcmd = new CommandBuffer();
            for (int i = 0; i < Engine.EyeCount; i++)
            {
                if (firstRun)
                {
                    RenderState tmpState = new RenderState(views[i].gbuffer, null, null, null, true, false, DepthFunc.Always, InverseDepth.Far, InverseDepth.Near, BlendFactor.One, BlendFactor.OneMinusSrcAlpha, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.Back);
                    tmpcmd.SetRenderState(tmpState);
                    tmpcmd.Clear(false, true);
                    firstRun = false;
                }

                //copy depth buffer into top of Hi-Z chain
                tmpcmd.SetRenderState(views[i].copyState);
                tmpcmd.Barrier(BarrierType.All);
                tmpcmd.Dispatch(views[i].depthBuf.Width / 8, views[i].depthBuf.Height / 8, 1);
                tmpcmd.Barrier(BarrierType.All);
                tmpcmd.Submit();

                //build mip pyramid
                unsafe
                {
                    //return;
                    int w = views[i].depthBuf.Width / 2;
                    int h = views[i].depthBuf.Height / 2;
                    for (int j = 1; j < views[i].hiZView.Length; j++)
                    {
                        int* p = (int*)HiZMapUBO.Update();
                        p[0] = j - 1;
                        HiZMapUBO.UpdateDone();

                        tmpcmd.Reset();
                        tmpcmd.SetRenderState(views[i].mipchainState);
                        tmpcmd.Barrier(BarrierType.ShaderImageAccess | BarrierType.BufferUpdate);
                        tmpcmd.Dispatch(w / 8 + 1, h / 8 + 1, 1);
                        tmpcmd.Barrier(BarrierType.ShaderImageAccess | BarrierType.BufferUpdate);
                        tmpcmd.Submit();
                        
                        w = w / 2;
                        h = h / 2;
                    }
                }
            }
        }

        public void Submit()
        {
            for (int i = 0; i < Engine.EyeCount; i++)
                views[i].cBuffer.Submit();
        }
    }
}
