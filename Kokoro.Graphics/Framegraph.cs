using System.Net.Mime;
using System.Collections.Generic;
using System;

namespace Kokoro.Graphics
{
    public enum SizeClass
    {
        ScreenRelative,
        Constant,
    }

    public enum AttachmentUsage
    {
        WriteOnly,
        WriteOnlyClear,
        ReadWrite,
        ReadOnly,
    }

    public interface INamedResource
    {
        string Name { get; }
    }

    public class AttachmentInfo : INamedResource
    {
        public string Name { get; set; }
        public SizeClass BaseSize { get; set; } = SizeClass.ScreenRelative;
        public ImageFormat Format { get; set; } = ImageFormat.B8G8R8A8Unorm;
        public float SizeX { get; set; } = 1;
        public float SizeY { get; set; } = 1;
        public int Levels { get; set; } = 1;
        public int Layers { get; set; } = 1;
    }

    public class AttachmentUsageInfo : INamedResource
    {
        public string Name { get; set; }
        public AttachmentUsage Usage { get; set; }
    }

    public class SampledAttachmentUsageInfo : INamedResource
    {
        public string Name { get; set; }
        public AttachmentUsage Usage { get; set; }
        public Sampler ImageSampler { get; set; }
    }

    public class TextureAttachmentInfo : INamedResource
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int BaseLevel { get; set; }
        public int BaseLayer { get; set; }
        public int Levels { get; set; }
        public int Layers { get; set; }
        public Sampler ImageSampler { get; set; }
        public ImageView View { get; set; }
    }

    public class BufferInfo : INamedResource
    {
        public string Name { get; set; }
        public int BindingIndex { get; set; }
        public GpuBuffer DeviceBuffer { get; set; }
        public GpuBufferView View { get; set; }
    }

    public interface IBasePass
    {
        string Name { get; }
        PassType PassType { get; }
        string[] PassDependencies { get; }
    }

    public enum PassType
    {
        Unknown,
        Graphics,
        BufferUpload,
        ImageUpload,
        Compute,
        AsyncCompute,
    }

    public class BufferUploadPass : IBasePass
    {
        public PassType PassType { get; } = PassType.BufferUpload;
        public string Name { get; set; }
        public GpuBuffer SourceBuffer { get; set; }
        public GpuBuffer DestBuffer { get; set; }
        public ulong LocalOffset { get; set; }
        public ulong DeviceOffset { get; set; }
        public ulong Size { get; set; }
        public string[] PassDependencies { get; set; }
    }

    public class ImageUploadPass : IBasePass
    {
        public PassType PassType { get; } = PassType.ImageUpload;
        public string Name { get; set; }
        public GpuBuffer SourceBuffer { get; set; }
        public Image DestBuffer { get; set; }
        public string[] PassDependencies { get; set; }
    }

    public class GraphicsPass : IBasePass
    {
        public PassType PassType { get; } = PassType.Graphics;
        public string Name { get; set; }
        public PrimitiveType Topology { get; set; } = PrimitiveType.Triangle;
        public bool DepthClamp { get; set; }
        public bool RasterizerDiscard { get; set; } = false;
        public float LineWidth { get; set; } = 1.0f;
        public CullMode CullMode { get; set; } = CullMode.None;
        public bool EnableBlending { get; set; }
        public DepthTest DepthTest { get; set; } = DepthTest.Greater;
        public AttachmentUsageInfo[] AttachmentUsage { get; set; }
        public AttachmentUsageInfo DepthAttachment { get; set; }
        public SampledAttachmentUsageInfo[] SampledAttachments { get; set; }
        public ShaderSource[] Shaders { get; set; }
        public TextureAttachmentInfo[] Textures { get; set; }
        public BufferInfo[] Buffers { get; set; }
        public string[] PassDependencies { get; set; }
        public Action PassHandler { get; set; }
    }

    public class ComputePass : IBasePass
    {
        public PassType PassType { get; } = PassType.Compute;
        public string Name { get; set; }
        public int GroupX { get; set; }
        public int GroupY { get; set; }
        public int GroupZ { get; set; }
        public AttachmentUsageInfo[] AttachmentUsage { get; set; }
        public SampledAttachmentUsageInfo[] SampledAttachments { get; set; }
        public ShaderSource Shader { get; set; }
        public TextureAttachmentInfo[] Textures { get; set; }
        public BufferInfo[] Buffers { get; set; }
        public string[] PassDependencies { get; set; }
    }

    public class AsyncComputePass : IBasePass
    {
        public PassType PassType { get; } = PassType.AsyncCompute;
        public string Name { get; set; }
        public int GroupX { get; set; }
        public int GroupY { get; set; }
        public int GroupZ { get; set; }
        public AttachmentUsageInfo[] AttachmentUsage { get; set; }
        public SampledAttachmentUsageInfo[] SampledAttachments { get; set; }
        public ShaderSource Shader { get; set; }
        public TextureAttachmentInfo[] Textures { get; set; }
        public BufferInfo[] Buffers { get; set; }
        public string[] PassDependencies { get; set; }
    }

    public class Framegraph
    {
        public string PreviousDepth { get; } = "prev_depth";
        public string PreviousGBuffer { get; } = "prev_gbuffer";
        public string CurrentDepth { get; } = "cur_depth";
        public string CurrentGBuffer { get; } = "cur_gbuffer";

        public Dictionary<string, IBasePass> Passes { get; private set; }

        private readonly Dictionary<string, BuiltAttachment> Attachments;
        private readonly List<BuiltPass> GraphicsPasses;
        private readonly List<BuiltPass> TransferPasses;
        private readonly List<BuiltPass> AsyncComputePasses;
        private readonly int DeviceIndex;

        public Framegraph(int device_index)
        {
            DeviceIndex = device_index;
            Passes = new Dictionary<string, IBasePass>();
            Attachments = new Dictionary<string, BuiltAttachment>();
            GraphicsPasses = new List<BuiltPass>();
            TransferPasses = new List<BuiltPass>();
            AsyncComputePasses = new List<BuiltPass>();
        }

        public void Reset()
        {
            //var prev_depth = Passes[PreviousDepth];
            //var prev_gbuffer = Passes[PreviousGBuffer];
            //var cur_depth = Passes[CurrentDepth];
            //var cur_gbuffer = Passes[CurrentGBuffer];
            Passes.Clear();
            //Passes[PreviousDepth] = prev_depth;
            //Passes[PreviousGBuffer] = prev_gbuffer;
            //Passes[CurrentDepth] = cur_depth;
            //Passes[CurrentGBuffer] = cur_gbuffer;
        }

        public void RegisterAttachment(AttachmentInfo attachment)
        {
            //Add this attachment's description to the collection
        }

        public void RegisterPass(IBasePass pass)
        {
            if (!Passes.ContainsKey(pass.Name))
                Passes[pass.Name] = pass;
            else
                throw new Exception("A pass already exists with the same name.");
        }

        public void Compile()
        {
            var ProcessedPasses = new HashSet<string>();
            var AvailablePasses = new List<string>();
            var semaphoreDict = new Dictionary<string, GpuSemaphore>();

            //Process passes for which all dependent passes have been processed
            for (int i = 0; i < AvailablePasses.Count; i++)
            {
                var pass = Passes[AvailablePasses[i]];
                bool canRun = true;
                for (int j = 0; j < pass.PassDependencies.Length; j++)
                    if (!ProcessedPasses.Contains(pass.PassDependencies[j]))
                    {
                        canRun = false;
                        break;
                    }
                if (canRun)
                {
                    //Setup this pass
                    var bp = new BuiltPass
                    {
                        Pass = pass,
                        SignalSemaphores = new GpuSemaphore(),
                        WaitSemaphores = new GpuSemaphore[pass.PassDependencies.Length]
                    };
                    bp.SignalSemaphores.Build(0, true, GraphicsDevice.CurrentFrameCount);
                    //Add the timeline semaphores of each previous pass as dependent for the current pass
                    for (int j = 0; j < pass.PassDependencies.Length; j++)
                        bp.WaitSemaphores[j] = semaphoreDict[pass.PassDependencies[j]];

                    //Add the semaphore for the current pass to the list
                    semaphoreDict[bp.Name] = bp.SignalSemaphores;

                    switch (pass.PassType)
                    {
                        case PassType.Graphics:
                            {
                                var p = pass as GraphicsPass;
                                //Compute width and height of render target
                                uint w = 0, h = 0;
                                for (int j = 0; j < p.AttachmentUsage.Length; j++)
                                {
                                    uint t_w, t_h;
                                    var attachInfo = Attachments[p.AttachmentUsage[j].Name].Attachment;
                                    if (attachInfo.BaseSize == SizeClass.ScreenRelative)
                                    {
                                        t_w = (uint)(attachInfo.SizeX * GraphicsDevice.Width);
                                        t_h = (uint)(attachInfo.SizeY * GraphicsDevice.Height);
                                    }
                                    else
                                    {
                                        t_w = (uint)(attachInfo.SizeX);
                                        t_h = (uint)(attachInfo.SizeY);
                                    }

                                    if (w == 0 && h == 0)
                                    {
                                        w = t_w;
                                        h = t_h;
                                    }
                                    else if (w != t_w | h != t_h)
                                        throw new Exception("Attachment sizes must match!");
                                }

                                //Setup descriptors

                                //Setup the framebuffer
                                bp.Framebuffer = new Framebuffer(w, h);

                                //Setup the renderpass
                                bp.RenderPass = new RenderPass();   //Track layout states
                                if (p.DepthAttachment != null)
                                {
                                    bp.RenderPass.Formats[AttachmentKind.DepthAttachment] = Attachments[p.DepthAttachment.Name].Attachment.Format;
                                    bp.RenderPass.InitialLayout[AttachmentKind.DepthAttachment] = Attachments[p.DepthAttachment.Name].CurrentLayout;
                                    switch (p.DepthAttachment.Usage)
                                    {
                                        case AttachmentUsage.ReadOnly:
                                            bp.RenderPass.StartLayout[AttachmentKind.DepthAttachment] = ImageLayout.DepthReadOnlyOptimal;
                                            bp.RenderPass.FinalLayout[AttachmentKind.DepthAttachment] = ImageLayout.DepthReadOnlyOptimal;
                                            bp.RenderPass.LoadOp[AttachmentKind.DepthAttachment] = AttachmentLoadOp.Load;
                                            bp.RenderPass.StoreOp[AttachmentKind.DepthAttachment] = AttachmentStoreOp.DoneCare;
                                            break;
                                        case AttachmentUsage.ReadWrite:
                                            bp.RenderPass.StartLayout[AttachmentKind.DepthAttachment] = ImageLayout.DepthAttachmentOptimal;
                                            bp.RenderPass.FinalLayout[AttachmentKind.DepthAttachment] = ImageLayout.DepthAttachmentOptimal;
                                            bp.RenderPass.LoadOp[AttachmentKind.DepthAttachment] = AttachmentLoadOp.Load;
                                            bp.RenderPass.StoreOp[AttachmentKind.DepthAttachment] = AttachmentStoreOp.Store;
                                            break;
                                        case AttachmentUsage.WriteOnly:
                                            bp.RenderPass.StartLayout[AttachmentKind.DepthAttachment] = ImageLayout.DepthAttachmentOptimal;
                                            bp.RenderPass.FinalLayout[AttachmentKind.DepthAttachment] = ImageLayout.DepthAttachmentOptimal;
                                            bp.RenderPass.LoadOp[AttachmentKind.DepthAttachment] = AttachmentLoadOp.DoneCare;
                                            bp.RenderPass.StoreOp[AttachmentKind.DepthAttachment] = AttachmentStoreOp.Store;
                                            break;
                                        case AttachmentUsage.WriteOnlyClear:
                                            bp.RenderPass.StartLayout[AttachmentKind.DepthAttachment] = ImageLayout.DepthAttachmentOptimal;
                                            bp.RenderPass.FinalLayout[AttachmentKind.DepthAttachment] = ImageLayout.DepthAttachmentOptimal;
                                            bp.RenderPass.LoadOp[AttachmentKind.DepthAttachment] = AttachmentLoadOp.Clear;
                                            bp.RenderPass.StoreOp[AttachmentKind.DepthAttachment] = AttachmentStoreOp.Store;
                                            break;
                                    }
                                    bp.Framebuffer[AttachmentKind.DepthAttachment] = Attachments[p.DepthAttachment.Name].View;
                                }
                                for (int j = 0; j < p.AttachmentUsage.Length; j++)
                                {
                                    bp.RenderPass.Formats[AttachmentKind.ColorAttachment0 + j] = Attachments[p.AttachmentUsage[j].Name].Attachment.Format;
                                    bp.RenderPass.InitialLayout[AttachmentKind.ColorAttachment0 + j] = Attachments[p.AttachmentUsage[j].Name].CurrentLayout;
                                    bp.RenderPass.StartLayout[AttachmentKind.ColorAttachment0 + j] = ImageLayout.ColorAttachmentOptimal;
                                    bp.RenderPass.FinalLayout[AttachmentKind.ColorAttachment0 + j] = ImageLayout.ColorAttachmentOptimal;    //TODO: Update flags so layout respects next desired layout
                                    switch (p.AttachmentUsage[j].Usage)
                                    {
                                        case AttachmentUsage.ReadOnly:
                                            bp.RenderPass.LoadOp[AttachmentKind.ColorAttachment0 + j] = AttachmentLoadOp.Load;
                                            bp.RenderPass.StoreOp[AttachmentKind.ColorAttachment0 + j] = AttachmentStoreOp.DoneCare;
                                            break;
                                        case AttachmentUsage.ReadWrite:
                                            bp.RenderPass.LoadOp[AttachmentKind.ColorAttachment0 + j] = AttachmentLoadOp.Load;
                                            bp.RenderPass.StoreOp[AttachmentKind.ColorAttachment0 + j] = AttachmentStoreOp.Store;
                                            break;
                                        case AttachmentUsage.WriteOnly:
                                            bp.RenderPass.LoadOp[AttachmentKind.ColorAttachment0 + j] = AttachmentLoadOp.DoneCare;
                                            bp.RenderPass.StoreOp[AttachmentKind.ColorAttachment0 + j] = AttachmentStoreOp.Store;
                                            break;
                                        case AttachmentUsage.WriteOnlyClear:
                                            bp.RenderPass.LoadOp[AttachmentKind.ColorAttachment0 + j] = AttachmentLoadOp.Clear;
                                            bp.RenderPass.StoreOp[AttachmentKind.ColorAttachment0 + j] = AttachmentStoreOp.Store;
                                            break;
                                    }
                                    bp.Framebuffer[AttachmentKind.ColorAttachment0 + j] = Attachments[p.AttachmentUsage[j].Name].View;
                                }
                                bp.RenderPass.Build(DeviceIndex); //TODO: Add multi-device support

                                //Setup the pipeline
                                bp.Pipeline = new PipelineLayout();
                                bp.Pipeline.Shaders.AddRange(p.Shaders);
                                bp.Pipeline.Topology = p.Topology;
                                bp.Pipeline.DepthClamp = p.DepthClamp;
                                bp.Pipeline.RasterizerDiscard = p.RasterizerDiscard;
                                bp.Pipeline.LineWidth = p.LineWidth;
                                bp.Pipeline.CullMode = p.CullMode;
                                bp.Pipeline.EnableBlending = p.EnableBlending;
                                bp.Pipeline.DepthTest = p.DepthTest;
                                bp.Pipeline.RenderPass = bp.RenderPass;
                                bp.Pipeline.Framebuffer = bp.Framebuffer;
                                bp.Pipeline.Build(DeviceIndex);

                                GraphicsPasses.Add(bp);
                            }
                            break;
                        case PassType.Compute:
                            {
                                //Should be a compute pass?

                                GraphicsPasses.Add(bp);
                            }
                            break;
                        case PassType.AsyncCompute:
                            {
                                AsyncComputePasses.Add(bp);
                            }
                            break;
                        case PassType.ImageUpload:
                            {
                                //Nothing to do right now
                                TransferPasses.Add(bp);
                            }
                            break;
                        case PassType.BufferUpload:
                            {
                                //Nothing to do right now
                                TransferPasses.Add(bp);
                            }
                            break;
                    }
                }
            }
        }

        struct BuiltAttachment
        {
            public string Name { get => Attachment.Name; }
            public AttachmentInfo Attachment;
            public ImageLayout CurrentLayout;
            public ImageView View;
        }

        struct BuiltPass
        {
            public string Name { get => Pass.Name; }
            public GpuSemaphore[] WaitSemaphores;
            public GpuSemaphore SignalSemaphores;
            public IBasePass Pass;
            public Framebuffer Framebuffer;
            public RenderPass RenderPass;
            public PipelineLayout Pipeline;
        }
    }
}
