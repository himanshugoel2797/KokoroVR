using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading;

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

    public interface IIndexedResource : INamedResource
    {
        uint BindingIndex { get; }
        DescriptorType DescriptorType { get; set; }
    }

    public class AttachmentInfo : INamedResource
    {
        public string Name { get; set; }
        public SizeClass BaseSize { get; set; } = SizeClass.ScreenRelative;
        public ImageFormat Format { get; set; } = ImageFormat.B8G8R8A8Unorm;
        public ImageUsage Usage { get; set; }
        public float SizeX { get; set; } = 1;
        public float SizeY { get; set; } = 1;
        public uint Levels { get; set; } = 1;
        public uint Layers { get; set; } = 1;
    }

    public class AttachmentUsageInfo : INamedResource
    {
        public string Name { get; set; }
        public AttachmentUsage Usage { get; set; }
    }

    public class SampledAttachmentUsageInfo : IIndexedResource
    {
        public string Name { get; set; }
        public uint BindingIndex { get; set; }
        public DescriptorType DescriptorType { get; set; }
        public AttachmentUsage Usage { get; set; }
        public Sampler ImageSampler { get; set; }
        public bool ImmutableSampler { get; set; }
    }

    public class TextureAttachmentInfo : IIndexedResource
    {
        public string Name { get; set; }
        public uint BindingIndex { get; set; }
        public DescriptorType DescriptorType { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public uint BaseLevel { get; set; }
        public uint BaseLayer { get; set; }
        public uint Levels { get; set; }
        public uint Layers { get; set; }
        public Sampler ImageSampler { get; set; }
        public ImageView View { get; set; }
        public bool ImmutableSampler { get; set; }
    }

    public class BufferInfo : IIndexedResource
    {
        public string Name { get; set; }
        public uint BindingIndex { get; set; }
        public DescriptorType DescriptorType { get; set; }
        public GpuBuffer DeviceBuffer { get; set; }
        public GpuBufferView View { get; set; }
    }

    public interface IBasePass
    {
        string Name { get; }
        PassType PassType { get; }
        string[] PassDependencies { get; }

    }

    public interface IGpuPass : IBasePass
    {
        TextureAttachmentInfo[] Textures { get; set; }
        BufferInfo[] Buffers { get; set; }
        AttachmentUsageInfo[] AttachmentUsage { get; set; }
        SampledAttachmentUsageInfo[] SampledAttachments { get; set; }

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

    public enum DrawCmdType
    {
        Plain,
        Indexed,
        Indirect,
        IndirectIndexed,
    }

    public interface IDrawCmd
    {
        DrawCmdType CmdType { get; }
    }

    public class PlainDrawCmd : IDrawCmd
    {
        public DrawCmdType CmdType { get; } = DrawCmdType.Plain;
        public uint VertexCount { get; set; } = 0;
        public uint InstanceCount { get; set; } = 1;
        public uint BaseVertex { get; set; } = 0;
        public uint BaseInstance { get; set; } = 0;
    }

    public class GraphicsPass : IGpuPass
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
        public IDrawCmd DrawCmd { get; set; }
    }

    public class ComputePass : IGpuPass
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

    public class AsyncComputePass : IGpuPass
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
        public Dictionary<string, IBasePass> Passes { get; private set; }

        private readonly Dictionary<string, BuiltAttachment> Attachments;
        private readonly List<BuiltPass> GraphicsPasses;
        private readonly List<BuiltPass> TransferPasses;
        private readonly List<BuiltPass> AsyncComputePasses;
        private CommandPool[] GraphicsCmdPool;
        private CommandPool[] TransferCmdPool;
        private CommandPool[] AsyncComputeCmdPool;
        private DescriptorPool Pool;
        private DescriptorSet Descriptors;
        private readonly int DeviceIndex;
        private string OutputAttachmentName;

        private RenderPass[] outputPass;
        private PipelineLayout[] pipelineLayouts;
        private Pipeline[] pipelines;
        private GpuSemaphore finalSemaphore;
        private ShaderSource outputV, outputF;
        private CommandBuffer[] outputCmds;
        private DescriptorPool outputPool;
        private DescriptorSet outputDesc;
        private Sampler outputSampler;

        private const int InitialBaseBinding = 0;
        public const string OutputFrame = "output_frame";

        public Framegraph(int device_index)
        {
            DeviceIndex = device_index;
            Passes = new Dictionary<string, IBasePass>();
            Attachments = new Dictionary<string, BuiltAttachment>();
            GraphicsPasses = new List<BuiltPass>();
            TransferPasses = new List<BuiltPass>();
            AsyncComputePasses = new List<BuiltPass>();

            Pool = new DescriptorPool();
            Descriptors = new DescriptorSet();

            outputV = ShaderSource.Load(ShaderType.VertexShader, "FullScreenTriangle/vertex.glsl");
            outputF = ShaderSource.Load(ShaderType.FragmentShader, "FullScreenTriangle/fragment.glsl");


            if (GraphicsCmdPool == null)
            {
                GraphicsCmdPool = new CommandPool[GraphicsDevice.MaxFramesInFlight];
                TransferCmdPool = new CommandPool[GraphicsDevice.MaxFramesInFlight];
                AsyncComputeCmdPool = new CommandPool[GraphicsDevice.MaxFramesInFlight];

                for (int i = 0; i < GraphicsDevice.MaxFramesInFlight; i++)
                {
                    GraphicsCmdPool[i] = new CommandPool()
                    {
                        Name = "Graphics_fg",
                        Transient = false
                    };
                    GraphicsCmdPool[i].Build(DeviceIndex, CommandQueueKind.Graphics);

                    TransferCmdPool[i] = new CommandPool()
                    {
                        Name = "Transfer_fg",
                        Transient = false
                    };
                    TransferCmdPool[i].Build(DeviceIndex, CommandQueueKind.Transfer);

                    AsyncComputeCmdPool[i] = new CommandPool()
                    {
                        Name = "Compute_fg",
                        Transient = false
                    };
                    AsyncComputeCmdPool[i].Build(DeviceIndex, CommandQueueKind.Compute);
                }
            }
        }

        public void Reset()
        {
            Passes.Clear();
        }

        public void RegisterAttachment(AttachmentInfo attachment)
        {
            //Add this attachment's description to the collection
            var builtAttach = new BuiltAttachment()
            {
                Attachment = attachment,
                CurrentLayout = ImageLayout.Undefined,
                Image = new Image()
                {
                    Name = attachment.Name,
                    Width = (uint)(attachment.BaseSize == SizeClass.Constant ? attachment.SizeX : attachment.SizeX * GraphicsDevice.Width),
                    Height = (uint)(attachment.BaseSize == SizeClass.Constant ? attachment.SizeY : attachment.SizeY * GraphicsDevice.Height),
                    Depth = 1,
                    Cubemappable = false,
                    Dimensions = 2,
                    Format = attachment.Format,
                    InitialLayout = ImageLayout.Undefined,
                    Layers = attachment.Layers,
                    Levels = attachment.Levels,
                    MemoryUsage = MemoryUsage.GpuOnly,
                    Usage = attachment.Usage
                },
                View = new ImageView()
                {
                    Name = attachment.Name,
                    BaseLayer = 0,
                    BaseLevel = 0,
                    LayerCount = attachment.Layers,
                    LevelCount = attachment.Levels,
                    ViewType = ImageViewType.View2D,
                    Format = attachment.Format,
                }
            };
            builtAttach.Image.Build(DeviceIndex);
            builtAttach.View.Build(builtAttach.Image);
            Attachments.Add(builtAttach.Name, builtAttach);
        }

        public void RegisterPass(IBasePass pass)
        {
            if (!Passes.ContainsKey(pass.Name))
                Passes[pass.Name] = pass;
            else
                throw new Exception("A pass already exists with the same name.");
        }

        public void SetOutputPass(string name, string output_attachment)
        {
            OutputAttachmentName = name;

            outputPass = new RenderPass[GraphicsDevice.MaxFramesInFlight];
            pipelineLayouts = new PipelineLayout[GraphicsDevice.MaxFramesInFlight];
            pipelines = new Pipeline[GraphicsDevice.MaxFramesInFlight];
            outputCmds = new CommandBuffer[GraphicsDevice.MaxFramesInFlight];
            outputSampler = new Sampler()
            {
                Name = output_attachment,
                Border = BorderColor.TransparentFloatBlack,
                EdgeU = EdgeMode.ClampToEdge,
                EdgeV = EdgeMode.ClampToEdge,
                EdgeW = EdgeMode.ClampToEdge,
                MagLinearFilter = true,
                MinLinearFilter = true,
                MipLinearFilter = true,
                UnnormalizedCoords = false,
                AnisotropicSamples = 1
            };
            outputSampler.Build(DeviceIndex);

            //TODO split pipeline and pipeline layout
            //TODO split descriptors and descriptor layout
            outputPool = new DescriptorPool();
            outputDesc = new DescriptorSet()
            {
                Pool = outputPool
            };
            outputPool.Add(0, DescriptorType.CombinedImageSampler, 1, ShaderType.FragmentShader, new Sampler[] { outputSampler });
            outputPool.Build(DeviceIndex, 1);
            outputDesc.Build(DeviceIndex);
            outputDesc.Set(0, 0, Attachments[output_attachment].View, outputSampler);

            for (int i = 0; i < GraphicsDevice.MaxFramesInFlight; i++)
            {
                outputPass[i] = new RenderPass();
                outputPass[i].InitialLayout[AttachmentKind.ColorAttachment0] = ImageLayout.Undefined;
                outputPass[i].StartLayout[AttachmentKind.ColorAttachment0] = ImageLayout.ColorAttachmentOptimal;
                outputPass[i].FinalLayout[AttachmentKind.ColorAttachment0] = ImageLayout.PresentSrc;
                outputPass[i].LoadOp[AttachmentKind.ColorAttachment0] = AttachmentLoadOp.DoneCare;
                outputPass[i].StoreOp[AttachmentKind.ColorAttachment0] = AttachmentStoreOp.Store;
                outputPass[i].Formats[AttachmentKind.ColorAttachment0] = ImageFormat.B8G8R8A8Unorm;

                outputPass[i].Build(0);

                GraphicsDevice.DefaultFramebuffer[i].RenderPass = outputPass[i];
                GraphicsDevice.DefaultFramebuffer[i].Build(0);

                pipelineLayouts[i] = new PipelineLayout
                {
                    Descriptors = new DescriptorSet[] { outputDesc }
                };
                pipelineLayouts[i].Build(DeviceIndex);
                pipelines[i] = new Pipeline()
                {
                    PipelineLayout = pipelineLayouts[i],
                    Framebuffer = GraphicsDevice.DefaultFramebuffer[i],
                    RenderPass = outputPass[i],
                    DepthTest = DepthTest.Always,
                };
                pipelines[i].Shaders.Add(outputV);
                pipelines[i].Shaders.Add(outputF);
                pipelines[i].Build(DeviceIndex);

                outputCmds[i] = new CommandBuffer();
                outputCmds[i].Build(GraphicsCmdPool[i]);
                outputCmds[i].BeginRecording();
                outputCmds[i].SetPipeline(pipelines[i], 0);
                outputCmds[i].SetDescriptors(pipelineLayouts[i], outputDesc, DescriptorBindPoint.Graphics, 0);
                outputCmds[i].SetViewport(0, 0, GraphicsDevice.DefaultFramebuffer[0].Width, GraphicsDevice.DefaultFramebuffer[0].Height);
                outputCmds[i].Draw(3, 1, 0, 0);
                outputCmds[i].EndRenderPass();
                outputCmds[i].EndRecording();
            }
        }

        public void Compile()
        {
            uint bindingBase = InitialBaseBinding;

            var ProcessedPasses = new HashSet<string>();
            var ProcessedPassesList = new List<string>();
            var ProcessedBPsList = new List<BuiltPass>();
            var AvailablePasses = Passes.Keys.ToArray();
            var semaphoreDict = new Dictionary<string, GpuSemaphore>();

            //Process passes for which all dependent passes have been processed
            for (int i = 0; i < AvailablePasses.Length; i++)
            {
                var pass = Passes[AvailablePasses[i]];
                bool canRun = true;
                if (pass.PassDependencies != null)
                    for (int j = 0; j < pass.PassDependencies.Length; j++)
                        if (!ProcessedPasses.Contains(pass.PassDependencies[j]))
                        {
                            canRun = false;
                            break;
                        }
                if (canRun)
                {
                    ProcessedPasses.Add(pass.Name);
                    ProcessedPassesList.Add(pass.Name);
                }
            }

            for (int i = 0; i < ProcessedPassesList.Count; i++)
            {
                //Setup this pass
                var pass = Passes[ProcessedPassesList[i]];
                var bp = new BuiltPass
                {
                    BindingBase = bindingBase,
                    Pass = pass,
                    SignalSemaphores = new GpuSemaphore(),
                    WaitSemaphores = new GpuSemaphore[pass.PassDependencies == null ? 0 : pass.PassDependencies.Length],
                    Commands = new CommandBuffer[GraphicsDevice.MaxFramesInFlight]
                };


                bp.SignalSemaphores.Build(0, true, GraphicsDevice.CurrentFrameCount);
                //Add the timeline semaphores of each previous pass as dependent for the current pass
                if (pass.PassDependencies != null)
                    for (int j = 0; j < pass.PassDependencies.Length; j++)
                        bp.WaitSemaphores[j] = semaphoreDict[pass.PassDependencies[j]];

                //Add the semaphore for the current pass to the list
                semaphoreDict[bp.Name] = bp.SignalSemaphores;

                if (pass is IGpuPass gpuPass)
                {
                    finalSemaphore = bp.SignalSemaphores;
                    uint curMaxBinding = bindingBase;
                    if (gpuPass.Buffers != null)
                        for (int j = 0; j < gpuPass.Buffers.Length; j++)
                        {
                            Pool.Add(bindingBase + gpuPass.Buffers[j].BindingIndex, gpuPass.Buffers[j].DescriptorType, 1, ShaderType.All);
                            curMaxBinding = Math.Max(bindingBase + gpuPass.Buffers[j].BindingIndex, curMaxBinding);
                        }

                    if (gpuPass.SampledAttachments != null)
                        for (int j = 0; j < gpuPass.SampledAttachments.Length; j++)
                        {
                            //Use compiled samplers if requested
                            if (gpuPass.SampledAttachments[j].ImmutableSampler)
                                Pool.Add(bindingBase + gpuPass.SampledAttachments[j].BindingIndex, gpuPass.SampledAttachments[j].DescriptorType, 1, ShaderType.All, new Sampler[] { gpuPass.SampledAttachments[j].ImageSampler });
                            else
                                Pool.Add(bindingBase + gpuPass.SampledAttachments[j].BindingIndex, gpuPass.SampledAttachments[j].DescriptorType, 1, ShaderType.All);
                            curMaxBinding = Math.Max(bindingBase + gpuPass.SampledAttachments[j].BindingIndex, curMaxBinding);
                        }

                    if (gpuPass.Textures != null)
                        for (int j = 0; j < gpuPass.Textures.Length; j++)
                        {
                            //Use compiled samplers if requested
                            if (gpuPass.SampledAttachments[j].ImmutableSampler)
                                Pool.Add(bindingBase + gpuPass.Textures[j].BindingIndex, gpuPass.Textures[j].DescriptorType, 1, ShaderType.All, new Sampler[] { gpuPass.Textures[j].ImageSampler });
                            else
                                Pool.Add(bindingBase + gpuPass.Textures[j].BindingIndex, gpuPass.Textures[j].DescriptorType, 1, ShaderType.All);
                            curMaxBinding = Math.Max(bindingBase + gpuPass.Textures[j].BindingIndex, curMaxBinding);
                        }

                    if (curMaxBinding != bindingBase)
                        bindingBase = curMaxBinding + 1;
                }

                ProcessedBPsList.Add(bp);
            }

            Pool.Build(DeviceIndex, GraphicsDevice.MaxFramesInFlight);
            Descriptors.Pool = Pool;
            Descriptors.Build(DeviceIndex);

            //Write descriptors
            bindingBase = InitialBaseBinding;
            for (int i = 0; i < ProcessedPassesList.Count; i++)
            {
                var pass = Passes[ProcessedPassesList[i]];
                var bp = ProcessedBPsList[i];
                if (pass is IGpuPass gpuPass)
                {

                    uint curMaxBinding = bindingBase;
                    if (gpuPass.Buffers != null)
                        for (int j = 0; j < gpuPass.Buffers.Length; j++)
                        {
                            switch (gpuPass.Buffers[j].DescriptorType)
                            {
                                case DescriptorType.StorageBuffer:
                                    Descriptors.Set(bindingBase + gpuPass.Buffers[j].BindingIndex, 0, gpuPass.Buffers[j].DeviceBuffer, 0, gpuPass.Buffers[j].DeviceBuffer.Size);
                                    break;
                                case DescriptorType.StorageTexelBuffer:
                                    Descriptors.Set(bindingBase + gpuPass.Buffers[j].BindingIndex, 0, gpuPass.Buffers[j].View);
                                    break;
                                case DescriptorType.UniformBuffer:
                                    Descriptors.Set(bindingBase + gpuPass.Buffers[j].BindingIndex, 0, gpuPass.Buffers[j].DeviceBuffer, 0, gpuPass.Buffers[j].DeviceBuffer.Size);
                                    break;
                                case DescriptorType.UniformTexelBuffer:
                                    Descriptors.Set(bindingBase + gpuPass.Buffers[j].BindingIndex, 0, gpuPass.Buffers[j].View);
                                    break;
                            }
                            curMaxBinding = Math.Max(bindingBase + gpuPass.Buffers[j].BindingIndex, curMaxBinding);
                        }

                    if (gpuPass.SampledAttachments != null)
                        for (int j = 0; j < gpuPass.SampledAttachments.Length; j++)
                        {
                            //Use compiled samplers if requested
                            switch (gpuPass.SampledAttachments[j].DescriptorType)
                            {
                                case DescriptorType.CombinedImageSampler:
                                    Descriptors.Set(bindingBase + gpuPass.SampledAttachments[j].BindingIndex, 0, Attachments[gpuPass.SampledAttachments[j].Name].View, gpuPass.SampledAttachments[j].ImageSampler);
                                    break;
                                case DescriptorType.SampledImage:
                                    Descriptors.Set(bindingBase + gpuPass.SampledAttachments[j].BindingIndex, 0, Attachments[gpuPass.SampledAttachments[j].Name].View, false);
                                    break;
                                case DescriptorType.StorageImage:
                                    Descriptors.Set(bindingBase + gpuPass.SampledAttachments[j].BindingIndex, 0, Attachments[gpuPass.SampledAttachments[j].Name].View, true);
                                    break;
                            }
                            curMaxBinding = Math.Max(bindingBase + gpuPass.SampledAttachments[j].BindingIndex, curMaxBinding);
                        }

                    if (gpuPass.Textures != null)
                        for (int j = 0; j < gpuPass.Textures.Length; j++)
                        {
                            //Use compiled samplers if requested
                            switch (gpuPass.Textures[j].DescriptorType)
                            {
                                case DescriptorType.CombinedImageSampler:
                                    Descriptors.Set(bindingBase + gpuPass.Textures[j].BindingIndex, 0, gpuPass.Textures[j].View, gpuPass.Textures[j].ImageSampler);
                                    break;
                                case DescriptorType.SampledImage:
                                    Descriptors.Set(bindingBase + gpuPass.Textures[j].BindingIndex, 0, gpuPass.Textures[j].View, false);
                                    break;
                                case DescriptorType.StorageImage:
                                    Descriptors.Set(bindingBase + gpuPass.Textures[j].BindingIndex, 0, gpuPass.Textures[j].View, true);
                                    break;
                            }
                            curMaxBinding = Math.Max(bindingBase + gpuPass.Textures[j].BindingIndex, curMaxBinding);
                        }

                    if (bindingBase != curMaxBinding)
                        bindingBase = curMaxBinding + 1;
                }

                switch (pass.PassType)
                {
                    case PassType.Graphics:
                        {
                            for (int j = 0; j < bp.Commands.Length; j++)
                            {
                                bp.Commands[j] = new CommandBuffer();
                                bp.Commands[j].Build(GraphicsCmdPool[j]);
                            }

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
                            bp.RenderPass.Build(DeviceIndex);
                            bp.Framebuffer.RenderPass = bp.RenderPass;
                            bp.Framebuffer.Build(DeviceIndex);

                            //Setup the pipeline
                            bp.PipelineLayout = new PipelineLayout()
                            {
                                Descriptors = Pool.Layouts.Count == 0 ? null : new DescriptorSet[] { Descriptors }
                            };
                            bp.PipelineLayout.Build(DeviceIndex);

                            bp.Pipeline = new Pipeline();
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
                            bp.Pipeline.PipelineLayout = bp.PipelineLayout;
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

        private void SubmitGraphics()
        {
            //if the command buffer doesn't exist, allocate it

            GraphicsDevice.AcquireFrame();

            foreach (var e in GraphicsPasses)
            {
                var cmdbuf = e.Commands[GraphicsDevice.CurrentFrameIndex];
                cmdbuf.Reset();
                cmdbuf.BeginRecording();
                var gpass = e.Pass as GraphicsPass;
                var drCmd = gpass.DrawCmd;

                //Bind the pipeline
                cmdbuf.SetPipeline(e.Pipeline, 0);

                //Bind descriptors
                cmdbuf.SetDescriptors(e.PipelineLayout, Descriptors, DescriptorBindPoint.Graphics, 0);

                //Set viewport
                cmdbuf.SetViewport(0, 0, e.Framebuffer.Width, e.Framebuffer.Height);

                switch (drCmd.CmdType)
                {
                    case DrawCmdType.Plain:
                        {
                            var plainDrCmd = drCmd as PlainDrawCmd;
                            cmdbuf.Draw(plainDrCmd.VertexCount, plainDrCmd.InstanceCount, plainDrCmd.BaseVertex, plainDrCmd.BaseVertex);
                        }
                        break;
                }

                cmdbuf.EndRenderPass();
                cmdbuf.EndRecording();
                GraphicsDevice.GetDeviceInfo(DeviceIndex).GraphicsQueue.SubmitCommandBuffer(cmdbuf, e.WaitSemaphores, new GpuSemaphore[] { e.SignalSemaphores }, null);
                //GraphicsDevice.SubmitGraphicsCommandBuffer(GraphicsCmdBuf);
            }

            //Submit a blit to screen operation dependent on the last operation
            GraphicsDevice.GetDeviceInfo(0).GraphicsQueue.SubmitCommandBuffer(outputCmds[GraphicsDevice.CurrentFrameIndex],
                new GpuSemaphore[] { GraphicsDevice.ImageAvailableSemaphore[GraphicsDevice.CurrentFrameNumber], finalSemaphore },
                new GpuSemaphore[] { GraphicsDevice.FrameFinishedSemaphore[GraphicsDevice.CurrentFrameNumber] },
                GraphicsDevice.InflightFences[GraphicsDevice.CurrentFrameNumber]);
            GraphicsDevice.PresentFrame();
        }

        private void SubmitTransfer()
        {

            foreach (var e in TransferPasses)
            {

            }
        }

        private void SubmitAsyncCompute()
        {

            foreach (var e in AsyncComputePasses)
            {

            }
        }

        public void Execute()
        {
            //Record and Submit command buffers across multiple threads
            var graphicsThd = new Thread(SubmitGraphics);
            var transferThd = new Thread(SubmitTransfer);
            var asyncCompThd = new Thread(SubmitAsyncCompute);

            transferThd.Start();
            asyncCompThd.Start();
            graphicsThd.Start();

            transferThd.Join();
            asyncCompThd.Join();
            graphicsThd.Join();
        }

        struct BuiltAttachment
        {
            public string Name { get => Attachment.Name; }
            public AttachmentInfo Attachment;
            public ImageLayout CurrentLayout;
            public Image Image;
            public ImageView View;
        }

        struct BuiltPass
        {
            public uint BindingBase;
            public string Name { get => Pass.Name; }
            public GpuSemaphore[] WaitSemaphores;
            public GpuSemaphore SignalSemaphores;
            public IBasePass Pass;
            public Framebuffer Framebuffer;
            public RenderPass RenderPass;
            public PipelineLayout PipelineLayout;
            public Pipeline Pipeline;
            public CommandBuffer[] Commands;
        }
    }
}
