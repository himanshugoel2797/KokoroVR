using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading;
using static VulkanSharp.Raw.Vk;

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

    public class ShaderParameterSet
    {
        public string Name { get; set; }
        public TextureAttachmentInfo[] Textures { get; set; }
        public BufferInfo[] Buffers { get; set; }
        public SampledAttachmentUsageInfo[] SampledAttachments { get; set; }
    }

    public interface IGpuPass : IBasePass
    {
        public string[] ShaderParamName { get; }
    }

    public enum PassType
    {
        Unknown,
        Graphics,
        BufferUpload,
        ImageUpload,
        Compute,
        IndirectCompute,
        AsyncCompute,
        AsyncIndirectCompute,
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
        IndexedIndirect,
    }

    public enum IndexType
    {
        U32 = VkIndexType.IndexTypeUint32,
        U16 = VkIndexType.IndexTypeUint16,
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

    public class IndexedDrawCmd : IDrawCmd
    {
        public DrawCmdType CmdType { get; } = DrawCmdType.Indexed;
        public IndexType IndexType { get; set; }
        public GpuBuffer IndexBuffer { get; set; }
        public uint VertexCount { get; set; } = 0;
        public uint IndexCount { get; set; } = 0;
        public uint InstanceCount { get; set; } = 1;
        public uint BaseVertex { get; set; } = 0;
        public uint BaseInstance { get; set; } = 0;
        public uint BaseIndex { get; set; } = 0;
    }

    public class IndirectDrawCmd : IDrawCmd
    {
        public DrawCmdType CmdType { get; } = DrawCmdType.Indirect;
        public GpuBuffer DrawBuffer { get; set; }
        public ulong Offset { get; set; }
        public GpuBuffer CountBuffer { get; set; }
        public ulong CountOffset { get; set; }
        public uint MaxCount { get; set; }
        public uint Stride { get; set; }
    }

    public class IndexedIndirectDrawCmd : IDrawCmd
    {
        public DrawCmdType CmdType { get; } = DrawCmdType.IndexedIndirect;
        public IndexType IndexType { get; set; }
        public GpuBuffer IndexBuffer { get; set; }
        public ulong IndexOffset { get; set; }
        public GpuBuffer DrawBuffer { get; set; }
        public ulong Offset { get; set; }
        public GpuBuffer CountBuffer { get; set; }
        public ulong CountOffset { get; set; }
        public uint MaxCount { get; set; }
        public uint Stride { get; set; }
    }

    public class GraphicsPass : IGpuPass
    {
        public PassType PassType { get; } = PassType.Graphics;
        public string Name { get; set; }
        public string[] ShaderParamName { get; set; }
        public PrimitiveType Topology { get; set; } = PrimitiveType.Triangle;
        public bool DepthClamp { get; set; }
        public bool RasterizerDiscard { get; set; } = false;
        public float LineWidth { get; set; } = 1.0f;
        public CullMode CullMode { get; set; } = CullMode.None;
        public bool EnableBlending { get; set; }
        public DepthTest DepthTest { get; set; } = DepthTest.Greater;
        public ShaderSource[] Shaders { get; set; }
        public string[] PassDependencies { get; set; }
        public IDrawCmd DrawCmd { get; set; }
        public AttachmentUsageInfo[] AttachmentUsage { get; set; }
        public AttachmentUsageInfo DepthAttachment { get; set; }
    }

    public class ComputePass : IGpuPass
    {
        public PassType PassType { get; } = PassType.Compute;
        public string Name { get; set; }
        public string[] ShaderParamName { get; set; }
        public uint GroupX { get; set; }
        public uint GroupY { get; set; }
        public uint GroupZ { get; set; }
        public ShaderSource Shader { get; set; }
        public string[] PassDependencies { get; set; }
    }

    public class IndirectComputePass : IGpuPass
    {
        public PassType PassType { get; } = PassType.IndirectCompute;
        public string Name { get; set; }
        public string[] ShaderParamName { get; set; }
        public ShaderSource Shader { get; set; }
        public GpuBuffer Buffer { get; set; }
        public ulong Offset { get; set; }
        public string[] PassDependencies { get; set; }
    }

    public class AsyncComputePass : IGpuPass
    {
        public PassType PassType { get; } = PassType.AsyncCompute;
        public string Name { get; set; }
        public string[] ShaderParamName { get; set; }
        public uint GroupX { get; set; }
        public uint GroupY { get; set; }
        public uint GroupZ { get; set; }
        public ShaderSource Shader { get; set; }
        public string[] PassDependencies { get; set; }
    }

    public class AsyncIndirectComputePass : IGpuPass
    {
        public PassType PassType { get; } = PassType.AsyncIndirectCompute;
        public string Name { get; set; }
        public string[] ShaderParamName { get; set; }
        public ShaderSource Shader { get; set; }
        public GpuBuffer Buffer { get; set; }
        public ulong Offset { get; set; }
        public string[] PassDependencies { get; set; }
    }

    public class Framegraph
    {
        public Dictionary<string, IBasePass> Passes { get; private set; }

        private readonly Dictionary<string, BuiltAttachment> Attachments;
        private readonly Dictionary<string, BuiltShaderParameterSet> ShaderParameters;
        private readonly List<BuiltPass> GraphicsPasses;
        private readonly List<BuiltPass> TransferPasses;
        private readonly List<BuiltPass> AsyncComputePasses;
        private CommandPool[] GraphicsCmdPool;
        private CommandPool[] TransferCmdPool;
        private CommandPool[] AsyncComputeCmdPool;
        private DescriptorPool Pool;
        private uint bindingBase = 0;
        private DescriptorSet Descriptors;
        private readonly int DeviceIndex;
        private readonly int CurrentID;
        private string OutputAttachmentName;
        private static int fgraph_id = 0;

        private RenderPass[] outputPass;
        private PipelineLayout[] pipelineLayouts;
        private GraphicsPipeline[] pipelines;
        private GpuSemaphore finalSemaphore;
        private ShaderSource outputV, outputF;
        private CommandBuffer[] outputCmds;
        private DescriptorPool outputPool;
        private DescriptorSet outputDesc;
        private Sampler outputSampler;

        public Framegraph(int device_index)
        {
            CurrentID = fgraph_id++;

            DeviceIndex = device_index;
            Passes = new Dictionary<string, IBasePass>();
            Attachments = new Dictionary<string, BuiltAttachment>();
            ShaderParameters = new Dictionary<string, BuiltShaderParameterSet>();
            GraphicsPasses = new List<BuiltPass>();
            TransferPasses = new List<BuiltPass>();
            AsyncComputePasses = new List<BuiltPass>();

            Pool = new DescriptorPool();
            Pool.Name = $"Framegraph_{CurrentID}";

            Descriptors = new DescriptorSet();
            Descriptors.Name = $"Framegraph_{CurrentID}";

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
                        Name = $"Graphics_fg_{CurrentID}",
                        Transient = false
                    };
                    GraphicsCmdPool[i].Build(DeviceIndex, CommandQueueKind.Graphics);

                    TransferCmdPool[i] = new CommandPool()
                    {
                        Name = $"Transfer_fg_{CurrentID}",
                        Transient = false
                    };
                    TransferCmdPool[i].Build(DeviceIndex, CommandQueueKind.Transfer);

                    AsyncComputeCmdPool[i] = new CommandPool()
                    {
                        Name = $"Compute_fg_{CurrentID}",
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

        public void RegisterShaderParams(ShaderParameterSet set)
        {
            ShaderParameters.Add(set.Name, new BuiltShaderParameterSet()
            {
                Params = set,
                BindingBase = bindingBase
            });
            uint curbase = bindingBase;
            if (set.Buffers != null)
                for (uint i = 0; i < set.Buffers.Length; i++)
                {
                    curbase = Math.Max(curbase, curbase + set.Buffers[i].BindingIndex);
                    Pool.Add(curbase, set.Buffers[i].DescriptorType, 1, ShaderType.All);
                }
            if (set.Textures != null)
                for (uint i = 0; i < set.Textures.Length; i++)
                {
                    curbase = Math.Max(curbase, curbase + set.Textures[i].BindingIndex);
                    if (set.Textures[i].ImmutableSampler && set.Textures[i].ImageSampler != null)
                        Pool.Add(curbase, set.Textures[i].DescriptorType, 1, ShaderType.All, new Sampler[] { set.Textures[i].ImageSampler });
                    else
                        Pool.Add(curbase, set.Textures[i].DescriptorType, 1, ShaderType.All);
                }
            if (set.SampledAttachments != null)
                for (uint i = 0; i < set.SampledAttachments.Length; i++)
                {
                    curbase = Math.Max(curbase, curbase + set.SampledAttachments[i].BindingIndex);
                    if (set.SampledAttachments[i].ImmutableSampler && set.SampledAttachments[i].ImageSampler != null)
                        Pool.Add(curbase, set.SampledAttachments[i].DescriptorType, 1, ShaderType.All, new Sampler[] { set.SampledAttachments[i].ImageSampler });
                    else
                        Pool.Add(curbase, set.SampledAttachments[i].DescriptorType, 1, ShaderType.All);
                }
            if (curbase != bindingBase)
                bindingBase = curbase + 1;
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
            pipelines = new GraphicsPipeline[GraphicsDevice.MaxFramesInFlight];
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
                outputPass[i].Name = $"FinalOutput_{i}";
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
                    Name = $"FinalOutput_{i}",
                    Descriptors = new DescriptorSet[] { outputDesc }
                };
                pipelineLayouts[i].Build(DeviceIndex);
                pipelines[i] = new GraphicsPipeline()
                {
                    Name = $"FinalOutput_{i}",
                    PipelineLayout = pipelineLayouts[i],
                    Framebuffer = GraphicsDevice.DefaultFramebuffer[i],
                    RenderPass = outputPass[i],
                    DepthTest = DepthTest.Always,
                };
                pipelines[i].Shaders.Add(outputV);
                pipelines[i].Shaders.Add(outputF);
                pipelines[i].Build(DeviceIndex);

                outputCmds[i] = new CommandBuffer();
                outputCmds[i].Name = $"FinalOutput_{i}";
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
            //TODO Figure out how to determine where to insert pipeline barriers
            //track writes to framebuffer attachments, inserting barriers when those need to be read
            //can just fill a command buffer in one go then
            //buffer read/writes will need barriers too

            var ProcessedPasses = new HashSet<string>();
            var ProcessedPassesList = new List<string>();
            var ProcessedBPsList = new List<BuiltPass>();
            var AvailablePasses = Passes.Keys.ToArray();
            var semaphoreDict = new Dictionary<string, GpuSemaphore>();
            var shaderParamsList = ShaderParameters.Values.ToArray();

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

            Pool.Build(DeviceIndex, GraphicsDevice.MaxFramesInFlight);
            Descriptors.Pool = Pool;
            Descriptors.Build(DeviceIndex);

            //Setup shader parameters
            for (int i = 0; i < shaderParamsList.Length; i++)
            {
                var sp = shaderParamsList[i].Params;
                uint bindingBase = shaderParamsList[i].BindingBase;
                if (sp.Buffers != null)
                    for (int j = 0; j < sp.Buffers.Length; j++)
                        switch (sp.Buffers[j].DescriptorType)
                        {
                            case DescriptorType.StorageBuffer:
                                Descriptors.Set(bindingBase + sp.Buffers[j].BindingIndex, 0, sp.Buffers[j].DeviceBuffer, 0, sp.Buffers[j].DeviceBuffer.Size);
                                break;
                            case DescriptorType.StorageTexelBuffer:
                                Descriptors.Set(bindingBase + sp.Buffers[j].BindingIndex, 0, sp.Buffers[j].View);
                                break;
                            case DescriptorType.UniformBuffer:
                                Descriptors.Set(bindingBase + sp.Buffers[j].BindingIndex, 0, sp.Buffers[j].DeviceBuffer, 0, sp.Buffers[j].DeviceBuffer.Size);
                                break;
                            case DescriptorType.UniformTexelBuffer:
                                Descriptors.Set(bindingBase + sp.Buffers[j].BindingIndex, 0, sp.Buffers[j].View);
                                break;
                        }

                if (sp.SampledAttachments != null)
                    for (int j = 0; j < sp.SampledAttachments.Length; j++)
                        switch (sp.SampledAttachments[j].DescriptorType)
                        {
                            case DescriptorType.CombinedImageSampler:
                                Descriptors.Set(bindingBase + sp.SampledAttachments[j].BindingIndex, 0, Attachments[sp.SampledAttachments[j].Name].View, sp.SampledAttachments[j].ImageSampler);
                                break;
                            case DescriptorType.SampledImage:
                                Descriptors.Set(bindingBase + sp.SampledAttachments[j].BindingIndex, 0, Attachments[sp.SampledAttachments[j].Name].View, false);
                                break;
                            case DescriptorType.StorageImage:
                                Descriptors.Set(bindingBase + sp.SampledAttachments[j].BindingIndex, 0, Attachments[sp.SampledAttachments[j].Name].View, true);
                                break;
                        }

                if (sp.Textures != null)
                    for (int j = 0; j < sp.Textures.Length; j++)
                        switch (sp.Textures[j].DescriptorType)
                        {
                            case DescriptorType.CombinedImageSampler:
                                Descriptors.Set(bindingBase + sp.Textures[j].BindingIndex, 0, sp.Textures[j].View, sp.Textures[j].ImageSampler);
                                break;
                            case DescriptorType.SampledImage:
                                Descriptors.Set(bindingBase + sp.Textures[j].BindingIndex, 0, sp.Textures[j].View, false);
                                break;
                            case DescriptorType.StorageImage:
                                Descriptors.Set(bindingBase + sp.Textures[j].BindingIndex, 0, sp.Textures[j].View, true);
                                break;
                        }
            }

            //Determine pass order
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

                bp.SignalSemaphores.Name = $"Signal_{bp.Name}";
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
                }

                ProcessedBPsList.Add(bp);
            }

            //Write descriptors
            for (int i = 0; i < ProcessedPassesList.Count; i++)
            {
                var pass = Passes[ProcessedPassesList[i]];
                var bp = ProcessedBPsList[i];

                switch (pass.PassType)
                {
                    case PassType.Graphics:
                        {
                            for (int j = 0; j < bp.Commands.Length; j++)
                            {
                                bp.Commands[j] = new CommandBuffer();
                                bp.Commands[j].Name = $"{pass.Name}_{CurrentID}";
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
                            bp.Framebuffer.Name = $"{pass.Name}_{CurrentID}";

                            //Setup the renderpass
                            bp.RenderPass = new RenderPass();   //Track layout states
                            bp.RenderPass.Name = $"{pass.Name}_{CurrentID}";
                            if (p.DepthAttachment != null)
                            {
                                bp.RenderPass.Formats[AttachmentKind.DepthAttachment] = Attachments[p.DepthAttachment.Name].Attachment.Format;
                                bp.RenderPass.InitialLayout[AttachmentKind.DepthAttachment] = Attachments[p.DepthAttachment.Name].CurrentLayout;
                                switch (p.DepthAttachment.Usage)
                                {
                                    case AttachmentUsage.ReadOnly:
                                        //may need a barrier
                                        bp.RenderPass.StartLayout[AttachmentKind.DepthAttachment] = ImageLayout.DepthReadOnlyOptimal;
                                        bp.RenderPass.FinalLayout[AttachmentKind.DepthAttachment] = ImageLayout.DepthReadOnlyOptimal;
                                        bp.RenderPass.LoadOp[AttachmentKind.DepthAttachment] = AttachmentLoadOp.Load;
                                        bp.RenderPass.StoreOp[AttachmentKind.DepthAttachment] = AttachmentStoreOp.DoneCare;
                                        break;
                                    case AttachmentUsage.ReadWrite:
                                        //may need a barrier
                                        bp.RenderPass.StartLayout[AttachmentKind.DepthAttachment] = ImageLayout.DepthAttachmentOptimal;
                                        bp.RenderPass.FinalLayout[AttachmentKind.DepthAttachment] = ImageLayout.DepthAttachmentOptimal;
                                        bp.RenderPass.LoadOp[AttachmentKind.DepthAttachment] = AttachmentLoadOp.Load;
                                        bp.RenderPass.StoreOp[AttachmentKind.DepthAttachment] = AttachmentStoreOp.Store;

                                        Attachments[p.DepthAttachment.Name].LastWriteIndex = i;
                                        Attachments[p.DepthAttachment.Name].LastWriteStage = PipelineStage.ColorAttachOut;
                                        break;
                                    case AttachmentUsage.WriteOnly:
                                        bp.RenderPass.StartLayout[AttachmentKind.DepthAttachment] = ImageLayout.DepthAttachmentOptimal;
                                        bp.RenderPass.FinalLayout[AttachmentKind.DepthAttachment] = ImageLayout.DepthAttachmentOptimal;
                                        bp.RenderPass.LoadOp[AttachmentKind.DepthAttachment] = AttachmentLoadOp.DoneCare;
                                        bp.RenderPass.StoreOp[AttachmentKind.DepthAttachment] = AttachmentStoreOp.Store;

                                        Attachments[p.DepthAttachment.Name].LastWriteIndex = i;
                                        Attachments[p.DepthAttachment.Name].LastWriteStage = PipelineStage.ColorAttachOut;
                                        break;
                                    case AttachmentUsage.WriteOnlyClear:
                                        //may need a barrier
                                        bp.RenderPass.StartLayout[AttachmentKind.DepthAttachment] = ImageLayout.DepthAttachmentOptimal;
                                        bp.RenderPass.FinalLayout[AttachmentKind.DepthAttachment] = ImageLayout.DepthAttachmentOptimal;
                                        bp.RenderPass.LoadOp[AttachmentKind.DepthAttachment] = AttachmentLoadOp.Clear;
                                        bp.RenderPass.StoreOp[AttachmentKind.DepthAttachment] = AttachmentStoreOp.Store;

                                        Attachments[p.DepthAttachment.Name].LastWriteIndex = i;
                                        Attachments[p.DepthAttachment.Name].LastWriteStage = PipelineStage.ColorAttachOut;
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
                                        //may need a barrier

                                        bp.RenderPass.LoadOp[AttachmentKind.ColorAttachment0 + j] = AttachmentLoadOp.Load;
                                        bp.RenderPass.StoreOp[AttachmentKind.ColorAttachment0 + j] = AttachmentStoreOp.DoneCare;
                                        break;
                                    case AttachmentUsage.ReadWrite:
                                        //may need a barrier

                                        bp.RenderPass.LoadOp[AttachmentKind.ColorAttachment0 + j] = AttachmentLoadOp.Load;
                                        bp.RenderPass.StoreOp[AttachmentKind.ColorAttachment0 + j] = AttachmentStoreOp.Store;

                                        Attachments[p.AttachmentUsage[j].Name].LastWriteIndex = i;
                                        Attachments[p.AttachmentUsage[j].Name].LastWriteStage = PipelineStage.ColorAttachOut;
                                        break;
                                    case AttachmentUsage.WriteOnly:
                                        bp.RenderPass.LoadOp[AttachmentKind.ColorAttachment0 + j] = AttachmentLoadOp.DoneCare;
                                        bp.RenderPass.StoreOp[AttachmentKind.ColorAttachment0 + j] = AttachmentStoreOp.Store;

                                        Attachments[p.AttachmentUsage[j].Name].LastWriteIndex = i;
                                        Attachments[p.AttachmentUsage[j].Name].LastWriteStage = PipelineStage.ColorAttachOut;
                                        break;
                                    case AttachmentUsage.WriteOnlyClear:
                                        //may need a barrier

                                        bp.RenderPass.LoadOp[AttachmentKind.ColorAttachment0 + j] = AttachmentLoadOp.Clear;
                                        bp.RenderPass.StoreOp[AttachmentKind.ColorAttachment0 + j] = AttachmentStoreOp.Store;

                                        Attachments[p.AttachmentUsage[j].Name].LastWriteIndex = i;
                                        Attachments[p.AttachmentUsage[j].Name].LastWriteStage = PipelineStage.ColorAttachOut;
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
                            bp.PipelineLayout.Name = $"{pass.Name}_{CurrentID}";
                            bp.PipelineLayout.Build(DeviceIndex);

                            bp.GraphicsPipeline = new GraphicsPipeline();
                            bp.GraphicsPipeline.Name = $"{pass.Name}_{CurrentID}";  //TODO pipelines are cached and loaded from a hashmap to cut down on context rolling overhead
                            bp.GraphicsPipeline.Shaders.AddRange(p.Shaders);    //TODO: Specialize shader binding indices
                            bp.GraphicsPipeline.Topology = p.Topology;
                            bp.GraphicsPipeline.DepthClamp = p.DepthClamp;
                            bp.GraphicsPipeline.RasterizerDiscard = p.RasterizerDiscard;
                            bp.GraphicsPipeline.LineWidth = p.LineWidth;
                            bp.GraphicsPipeline.CullMode = p.CullMode;
                            bp.GraphicsPipeline.EnableBlending = p.EnableBlending;
                            bp.GraphicsPipeline.DepthTest = p.DepthTest;
                            bp.GraphicsPipeline.RenderPass = bp.RenderPass;
                            bp.GraphicsPipeline.Framebuffer = bp.Framebuffer;
                            bp.GraphicsPipeline.PipelineLayout = bp.PipelineLayout;
                            bp.GraphicsPipeline.Build(DeviceIndex);

                            GraphicsPasses.Add(bp);
                        }
                        break;
                    case PassType.Compute:
                        {
                            //Should be a compute pass?
                            throw new NotImplementedException();
                            GraphicsPasses.Add(bp);
                        }
                        break;
                    case PassType.AsyncCompute:
                        {
                            for (int j = 0; j < bp.Commands.Length; j++)
                            {
                                bp.Commands[j] = new CommandBuffer();
                                bp.Commands[j].Name = $"{pass.Name}_{CurrentID}";
                                bp.Commands[j].Build(AsyncComputeCmdPool[j]);
                            }

                            bp.PipelineLayout = new PipelineLayout()
                            {
                                Descriptors = Pool.Layouts.Count == 0 ? null : new DescriptorSet[] { Descriptors }
                            };
                            bp.PipelineLayout.Name = $"{pass.Name}_{CurrentID}";
                            bp.PipelineLayout.Build(DeviceIndex);

                            bp.ComputePipeline = new ComputePipeline()
                            {
                                Name = $"{pass.Name}_{CurrentID}",
                                PipelineLayout = bp.PipelineLayout,
                                Shader = (pass as AsyncComputePass).Shader,
                            };
                            bp.ComputePipeline.Build(DeviceIndex);

                            AsyncComputePasses.Add(bp);
                        }
                        break;
                    case PassType.AsyncIndirectCompute:
                        {
                            for (int j = 0; j < bp.Commands.Length; j++)
                            {
                                bp.Commands[j] = new CommandBuffer();
                                bp.Commands[j].Name = $"{pass.Name}_{CurrentID}";
                                bp.Commands[j].Build(AsyncComputeCmdPool[j]);
                            }

                            bp.PipelineLayout = new PipelineLayout()
                            {
                                Descriptors = Pool.Layouts.Count == 0 ? null : new DescriptorSet[] { Descriptors }
                            };
                            bp.PipelineLayout.Name = $"{pass.Name}_{CurrentID}";
                            bp.PipelineLayout.Build(DeviceIndex);

                            bp.ComputePipeline = new ComputePipeline()
                            {
                                Name = $"{pass.Name}_{CurrentID}",
                                PipelineLayout = bp.PipelineLayout,
                                Shader = (pass as AsyncComputePass).Shader,
                            };
                            bp.ComputePipeline.Build(DeviceIndex);

                            AsyncComputePasses.Add(bp);
                        }
                        break;
                    case PassType.ImageUpload:
                        {
                            for (int j = 0; j < bp.Commands.Length; j++)
                            {
                                bp.Commands[j] = new CommandBuffer();
                                bp.Commands[j].Name = $"{pass.Name}_{CurrentID}";
                                bp.Commands[j].Build(TransferCmdPool[j]);
                            }

                            //Nothing to do right now
                            TransferPasses.Add(bp);
                        }
                        break;
                    case PassType.BufferUpload:
                        {
                            for (int j = 0; j < bp.Commands.Length; j++)
                            {
                                bp.Commands[j] = new CommandBuffer();
                                bp.Commands[j].Name = $"{pass.Name}_{CurrentID}";
                                bp.Commands[j].Build(TransferCmdPool[j]);
                            }

                            //Nothing to do right now
                            TransferPasses.Add(bp);
                        }
                        break;
                }
            }
        }

        private void SubmitGraphics()
        {
            GraphicsDevice.AcquireFrame();

            foreach (var e in GraphicsPasses)
            {
                var cmdbuf = e.Commands[GraphicsDevice.CurrentFrameIndex];
                cmdbuf.Reset();
                cmdbuf.BeginRecording();
                var gpass = e.Pass as GraphicsPass;
                var drCmd = gpass.DrawCmd;

                //Bind the pipeline
                cmdbuf.SetPipeline(e.GraphicsPipeline, 0);

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
                    case DrawCmdType.Indexed:
                        {
                            var indexedDrCmd = drCmd as IndexedDrawCmd;
                            cmdbuf.Draw(indexedDrCmd.VertexCount, indexedDrCmd.InstanceCount, indexedDrCmd.BaseVertex, indexedDrCmd.BaseVertex);
                        }
                        break;
                    case DrawCmdType.Indirect:
                        {
                            var indirDrCmd = drCmd as IndirectDrawCmd;
                            cmdbuf.DrawIndirect(indirDrCmd.DrawBuffer, indirDrCmd.Offset, indirDrCmd.CountBuffer, indirDrCmd.CountOffset, indirDrCmd.MaxCount, indirDrCmd.Stride);
                        }
                        break;
                    case DrawCmdType.IndexedIndirect:
                        {
                            var indirDrCmd = drCmd as IndexedIndirectDrawCmd;
                            cmdbuf.DrawIndexedIndirect(indirDrCmd.IndexBuffer, indirDrCmd.IndexOffset, indirDrCmd.IndexType, indirDrCmd.DrawBuffer, indirDrCmd.Offset, indirDrCmd.CountBuffer, indirDrCmd.CountOffset, indirDrCmd.MaxCount, indirDrCmd.Stride);
                        }
                        break;
                }
                cmdbuf.EndRenderPass();
                cmdbuf.EndRecording();
            }
        }

        private void SubmitTransfer()
        {
            foreach (var e in TransferPasses)
            {
                var cmdbuf = e.Commands[GraphicsDevice.CurrentFrameIndex];
                cmdbuf.Reset();
                cmdbuf.BeginRecording();

                if (e.Pass.PassType == PassType.ImageUpload)
                {
                    var pass = e.Pass as ImageUploadPass;
                    cmdbuf.Stage(pass.SourceBuffer, 0, pass.DestBuffer);
                }
                else if (e.Pass.PassType == PassType.BufferUpload)
                {
                    var pass = e.Pass as BufferUploadPass;
                    cmdbuf.Stage(pass.SourceBuffer, pass.LocalOffset, pass.DestBuffer, pass.DeviceOffset, pass.Size);
                }
                cmdbuf.EndRenderPass();
                cmdbuf.EndRecording();
            }
        }

        private void SubmitAsyncCompute()
        {
            foreach (var e in AsyncComputePasses)
            {
                var cmdbuf = e.Commands[GraphicsDevice.CurrentFrameIndex];
                cmdbuf.Reset();
                cmdbuf.BeginRecording();

                if (e.Pass.PassType == PassType.AsyncCompute)
                {
                    var cpass = e.Pass as AsyncComputePass;

                    cmdbuf.SetPipeline(e.ComputePipeline);
                    cmdbuf.SetDescriptors(e.PipelineLayout, Descriptors, DescriptorBindPoint.Compute, 0);
                    cmdbuf.Dispatch(cpass.GroupX, cpass.GroupY, cpass.GroupZ);
                }
                else if (e.Pass.PassType == PassType.AsyncIndirectCompute)
                {
                    var cpass = e.Pass as AsyncIndirectComputePass;

                    cmdbuf.SetPipeline(e.ComputePipeline);
                    cmdbuf.SetDescriptors(e.PipelineLayout, Descriptors, DescriptorBindPoint.Compute, 0);
                    cmdbuf.DispatchIndirect(cpass.Buffer, cpass.Offset);
                }
                cmdbuf.EndRenderPass();
                cmdbuf.EndRecording();
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

            int maxLen = Math.Max(GraphicsPasses.Count, Math.Max(TransferPasses.Count, AsyncComputePasses.Count));

            for (int i = 0; i < maxLen; i++)
            {
                if (i < AsyncComputePasses.Count)
                    GraphicsDevice.GetDeviceInfo(DeviceIndex).ComputeQueue.SubmitCommandBuffer(AsyncComputePasses[i].Commands[GraphicsDevice.CurrentFrameIndex], AsyncComputePasses[i].WaitSemaphores, new GpuSemaphore[] { AsyncComputePasses[i].SignalSemaphores }, null);

                if (i < TransferPasses.Count)
                    GraphicsDevice.GetDeviceInfo(DeviceIndex).TransferQueue.SubmitCommandBuffer(TransferPasses[i].Commands[GraphicsDevice.CurrentFrameIndex], TransferPasses[i].WaitSemaphores, new GpuSemaphore[] { TransferPasses[i].SignalSemaphores }, null);

                if (i < GraphicsPasses.Count)
                    GraphicsDevice.GetDeviceInfo(DeviceIndex).GraphicsQueue.SubmitCommandBuffer(GraphicsPasses[i].Commands[GraphicsDevice.CurrentFrameIndex], GraphicsPasses[i].WaitSemaphores, new GpuSemaphore[] { GraphicsPasses[i].SignalSemaphores }, null);
            }

            //Submit a blit to screen operation dependent on the last operation
            GraphicsDevice.GetDeviceInfo(0).GraphicsQueue.SubmitCommandBuffer(outputCmds[GraphicsDevice.CurrentFrameIndex],
                new GpuSemaphore[] { GraphicsDevice.ImageAvailableSemaphore[GraphicsDevice.CurrentFrameNumber], finalSemaphore },
                new GpuSemaphore[] { GraphicsDevice.FrameFinishedSemaphore[GraphicsDevice.CurrentFrameNumber] },
                GraphicsDevice.InflightFences[GraphicsDevice.CurrentFrameNumber]);
            GraphicsDevice.PresentFrame();
        }

        class BuiltShaderParameterSet
        {
            public string Name { get => Params.Name; }
            public ShaderParameterSet Params;
            public uint BindingBase;
        }

        class BuiltAttachment
        {
            public string Name { get => Attachment.Name; }
            public AttachmentInfo Attachment;
            public ImageLayout CurrentLayout;
            public Image Image;
            public ImageView View;

            public PipelineStage LastWriteStage;
            public int LastWriteIndex = -1;  //TODO use this to adjust barrier location to minimize possible stalls
        }

        class BuiltPass
        {
            public uint BindingBase;
            public string Name { get => Pass.Name; }
            public GpuSemaphore[] WaitSemaphores;
            public GpuSemaphore SignalSemaphores;
            public IBasePass Pass;
            public Framebuffer Framebuffer;
            public RenderPass RenderPass;
            public PipelineLayout PipelineLayout;
            public GraphicsPipeline GraphicsPipeline;
            public ComputePipeline ComputePipeline;
            public CommandBuffer[] Commands;
        }
    }
}
