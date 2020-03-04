using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading;
using static VulkanSharp.Raw.Vk;
using System.Diagnostics;

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
        public bool UseMipMaps { get; set; } = false;
        public uint Layers { get; set; } = 1;
    }

    public class AttachmentUsageInfo : INamedResource
    {
        public string Name { get; set; }
        public AttachmentUsage Usage { get; set; }
        public uint BaseLevel { get; set; } = 0;
        public uint Levels { get; set; } = 1;
    }

    public class SampledAttachmentUsageInfo : IIndexedResource
    {
        public string Name { get; set; }
        public uint BindingIndex { get; set; }
        public uint BaseLevel { get; set; }
        public uint Levels { get; set; }
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
        public GpuBuffer[] DeviceBuffer { get; set; }
        public GpuBufferView[] View { get; set; }
    }

    public interface IBasePass
    {
        string Name { get; }
        bool Active { get; set; }
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
        public bool Active { get; set; } = true;
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
        public bool Active { get; set; } = true;
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
        public ulong DrawBufferOffset { get; set; }
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
        public ulong DrawBufferOffset { get; set; }
        public GpuBuffer CountBuffer { get; set; }
        public ulong CountOffset { get; set; }
        public uint MaxCount { get; set; }
        public uint Stride { get; set; }
    }

    public class GraphicsPass : IGpuPass
    {
        public PassType PassType { get; } = PassType.Graphics;
        public string Name { get; set; }
        public bool Active { get; set; } = true;
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
        public bool Active { get; set; } = true;
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
        public bool Active { get; set; } = true;
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
        public bool Active { get; set; } = true;
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
        public bool Active { get; set; } = true;
        public string[] ShaderParamName { get; set; }
        public ShaderSource Shader { get; set; }
        public GpuBuffer Buffer { get; set; }
        public ulong Offset { get; set; }
        public string[] PassDependencies { get; set; }
    }

    public class Framegraph
    {
        public Dictionary<string, IBasePass> Passes { get; private set; }
        public GpuBuffer GlobalParameters { get; private set; }
        public string GlobalParametersName => "GlobalParameters";
        public ulong GlobalParametersLength => 16384;

        private Dictionary<string, BuiltAttachment> Attachments;
        private Dictionary<string, BuiltShaderParameterSet> ShaderParameters;
        private List<BuiltPass> GraphicsPasses;
        private List<BuiltPass> TransferPasses;
        private List<BuiltPass> AsyncComputePasses;
        private CommandPool[] GraphicsCmdPool;
        private CommandPool[] TransferCmdPool;
        private CommandPool[] AsyncComputeCmdPool;
        private DescriptorPool Pool;
        private readonly int DeviceIndex;
        private readonly int CurrentID;
        private string OutputAttachmentName;
        private static int fgraph_id = 0;
        private static Fence[] TransferFences;
        private static Fence[] ComputeFences;

        private RenderPass[] outputPass;
        private PipelineLayout[] pipelineLayouts;
        private GraphicsPipeline[] pipelines;
        private GpuSemaphore finalGraphicsSemaphore;
        private ShaderSource outputV, outputF;
        private CommandBuffer[] outputCmds;
        private DescriptorPool outputPool;
        private DescriptorLayout outputLayout;
        private DescriptorSet outputDesc;
        private Sampler outputSampler;

        public Framegraph(int device_index)
        {
            CurrentID = fgraph_id++;
            DeviceIndex = device_index;
            Reset();
        }

        public void Reset()
        {
            Passes = new Dictionary<string, IBasePass>();
            Attachments = new Dictionary<string, BuiltAttachment>();
            ShaderParameters = new Dictionary<string, BuiltShaderParameterSet>();
            GraphicsPasses = new List<BuiltPass>();
            TransferPasses = new List<BuiltPass>();
            AsyncComputePasses = new List<BuiltPass>();

            GlobalParameters = new GpuBuffer()
            {
                Name = GlobalParametersName,
                MemoryUsage = MemoryUsage.GpuOnly,
                Usage = BufferUsage.TransferDst | BufferUsage.Uniform,
                Size = GlobalParametersLength,
                Mapped = false
            };
            GlobalParameters.Build(DeviceIndex);

            Pool = new DescriptorPool();
            Pool.Name = $"Framegraph_{CurrentID}";

            outputV = ShaderSource.Load(ShaderType.VertexShader, "FullScreenTriangle/vertex.glsl");
            outputF = ShaderSource.Load(ShaderType.FragmentShader, "FullScreenTriangle/fragment.glsl");

            if (GraphicsCmdPool == null)
            {
                GraphicsCmdPool = new CommandPool[GraphicsDevice.MaxFramesInFlight];
                TransferCmdPool = new CommandPool[GraphicsDevice.MaxFramesInFlight];
                AsyncComputeCmdPool = new CommandPool[GraphicsDevice.MaxFramesInFlight];

                TransferFences = new Fence[GraphicsDevice.MaxFramesInFlight];
                ComputeFences = new Fence[GraphicsDevice.MaxFramesInFlight];

                for (int i = 0; i < GraphicsDevice.MaxFramesInFlight; i++)
                {
                    TransferFences[i] = new Fence()
                    {
                        CreateSignaled = true
                    };
                    TransferFences[i].Build(DeviceIndex);

                    ComputeFences[i] = new Fence()
                    {
                        CreateSignaled = true
                    };
                    ComputeFences[i].Build(DeviceIndex);

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

            RegisterShaderParams(new ShaderParameterSet()
            {
                Name = GlobalParametersName,
                Buffers = new BufferInfo[]
                {
                    new BufferInfo()
                    {
                        Name = GlobalParametersName,
                        BindingIndex = 0,
                        DescriptorType = DescriptorType.UniformBufferDynamic,
                        DeviceBuffer = new GpuBuffer[]{ GlobalParameters },
                    }
                },
            });
        }

        public void RegisterAttachment(AttachmentInfo attachment)
        {
            //Add this attachment's description to the collection
            uint width = (uint)(attachment.BaseSize == SizeClass.Constant ? attachment.SizeX : attachment.SizeX * GraphicsDevice.Width);
            uint height = (uint)(attachment.BaseSize == SizeClass.Constant ? attachment.SizeY : attachment.SizeY * GraphicsDevice.Height);
            uint lvCnt = 1;
            var maxSz = System.Math.Max(width, height);
            if (attachment.UseMipMaps)
                while (maxSz != 1)
                {
                    lvCnt++;
                    maxSz /= 2;
                }

            var builtAttach = new BuiltAttachment()
            {
                Attachment = attachment,
                CurrentLayout = ImageLayout.Undefined,
                Image = new Image()
                {
                    Name = attachment.Name,
                    Width = width,
                    Height = height,
                    Depth = 1,
                    Cubemappable = false,
                    Dimensions = 2,
                    Format = attachment.Format,
                    InitialLayout = ImageLayout.Undefined,
                    Layers = attachment.Layers,
                    Levels = lvCnt,
                    MemoryUsage = MemoryUsage.GpuOnly,
                    Usage = attachment.Usage
                },
                View = new ImageView()
                {
                    Name = attachment.Name,
                    BaseLayer = 0,
                    BaseLevel = 0,
                    LayerCount = attachment.Layers,
                    LevelCount = lvCnt,
                    ViewType = ImageViewType.View2D,
                    Format = attachment.Format,
                }
            };
            builtAttach.Image.Build(DeviceIndex);
            builtAttach.Mipmaps = new ImageView[lvCnt];
            for (uint i = 0; i < lvCnt; i++)
            {
                builtAttach.Mipmaps[i] = new ImageView()
                {
                    Name = attachment.Name,
                    BaseLayer = 0,
                    BaseLevel = i,
                    LayerCount = attachment.Layers,
                    LevelCount = 1,
                    ViewType = ImageViewType.View2D,
                    Format = attachment.Format,
                };
                builtAttach.Mipmaps[i].Build(builtAttach.Image);
            }
            builtAttach.View.Build(builtAttach.Image);
            Attachments.Add(builtAttach.Name, builtAttach);
        }

        public void RegisterShaderParams(ShaderParameterSet set)
        {
            ShaderParameters.Add(set.Name, new BuiltShaderParameterSet()
            {
                Params = set,
            });
            //uint curbase = 0;
            if (set.Buffers != null)
                for (uint i = 0; i < set.Buffers.Length; i++)
                {
                    //curbase = System.Math.Max(curbase, curbase + set.Buffers[i].BindingIndex);
                    switch (set.Buffers[i].DescriptorType)
                    {
                        case DescriptorType.StorageBuffer:
                        case DescriptorType.UniformBuffer:
                        case DescriptorType.UniformBufferDynamic:
                            Pool.Add(set.Buffers[i].DescriptorType, (uint)set.Buffers[i].DeviceBuffer.Length);
                            break;
                        case DescriptorType.UniformTexelBuffer:
                        case DescriptorType.StorageTexelBuffer:
                            Pool.Add(set.Buffers[i].DescriptorType, (uint)set.Buffers[i].View.Length);
                            break;
                    }
                }
            if (set.Textures != null)
                for (uint i = 0; i < set.Textures.Length; i++)
                {
                    //curbase = System.Math.Max(curbase, curbase + set.Textures[i].BindingIndex);
                    //if (set.Textures[i].ImmutableSampler && set.Textures[i].ImageSampler != null)
                    //    Pool.Add(curbase, set.Textures[i].DescriptorType, 1, ShaderType.All, new Sampler[] { set.Textures[i].ImageSampler });
                    //else
                    Pool.Add(set.Textures[i].DescriptorType, 1);
                }
            if (set.SampledAttachments != null)
                for (uint i = 0; i < set.SampledAttachments.Length; i++)
                {
                    //curbase = System.Math.Max(curbase, curbase + set.SampledAttachments[i].BindingIndex);
                    //if (set.SampledAttachments[i].ImmutableSampler && set.SampledAttachments[i].ImageSampler != null)
                    //    Pool.Add(curbase, set.SampledAttachments[i].DescriptorType, 1, ShaderType.All, new Sampler[] { set.SampledAttachments[i].ImageSampler });
                    //else
                    Pool.Add(set.SampledAttachments[i].DescriptorType, 1);
                }
        }

        public void RegisterPass(IBasePass pass)
        {
            if (!Passes.ContainsKey(pass.Name))
                Passes[pass.Name] = pass;
            else
                throw new Exception("A pass already exists with the same name.");
        }

        public void SetActiveState(string name, bool active)
        {
            Passes[name].Active = active;
        }

        public void SetOutputPass(string output_attachment)
        {
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
            outputPool.Add(DescriptorType.CombinedImageSampler, 1);
            outputPool.Build(DeviceIndex, 1);

            outputLayout = new DescriptorLayout();
            outputLayout.Add(0, DescriptorType.CombinedImageSampler, 1, ShaderType.FragmentShader, new Sampler[] { outputSampler });
            outputLayout.Build(DeviceIndex, 1);

            outputDesc = new DescriptorSet()
            {
                Layout = outputLayout,
                Pool = outputPool
            };
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
            if (ProcessedPassesList.Count != AvailablePasses.Length)
                throw new Exception("Failed to resolve all dependencies.");

            Pool.Build(DeviceIndex, GraphicsDevice.MaxFramesInFlight);

            //Determine pass order
            for (int i = 0; i < ProcessedPassesList.Count; i++)
            {
                //TODO Find all the dependencies and handle them automatically

                //Setup this pass
                var pass = Passes[ProcessedPassesList[i]];
                var bp = new BuiltPass
                {
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

                switch (pass.PassType)
                {
                    case PassType.Compute:
                    case PassType.IndirectCompute:
                    case PassType.Graphics:
                        finalGraphicsSemaphore = bp.SignalSemaphores;
                        break;
                        //case PassType.ImageUpload:
                        //case PassType.BufferUpload:
                        //    finalTransferSemaphore = bp.SignalSemaphores;
                        //    break;
                        //case PassType.AsyncCompute:
                        //case PassType.AsyncIndirectCompute:
                        //    finalComputeSemaphore = bp.SignalSemaphores;
                        //    break;
                }
                if (pass is IGpuPass gpuPass)
                {
                    if (gpuPass.ShaderParamName != null)
                    {
                        uint bindingBase = 0;
                        bp.DescriptorLayout = new DescriptorLayout();
                        for (int j = 0; j < shaderParamsList.Length; j++)
                        {
                            uint curBindingTop = bindingBase;
                            var sp = shaderParamsList[j].Params;
                            if (sp.Buffers != null)
                                for (int q = 0; q < sp.Buffers.Length; q++)
                                {
                                    switch (sp.Buffers[q].DescriptorType)
                                    {
                                        case DescriptorType.StorageBuffer:
                                        case DescriptorType.UniformBuffer:
                                        case DescriptorType.UniformBufferDynamic:
                                            bp.DescriptorLayout.Add(bindingBase + sp.Buffers[q].BindingIndex, sp.Buffers[q].DescriptorType, (uint)sp.Buffers[q].DeviceBuffer.Length, ShaderType.All);
                                            break;
                                        case DescriptorType.StorageTexelBuffer:
                                        case DescriptorType.UniformTexelBuffer:
                                            bp.DescriptorLayout.Add(bindingBase + sp.Buffers[q].BindingIndex, sp.Buffers[q].DescriptorType, (uint)sp.Buffers[q].View.Length, ShaderType.All);
                                            break;
                                    }
                                    curBindingTop = System.Math.Max(bindingBase + sp.Buffers[q].BindingIndex, curBindingTop) + 1;
                                }

                            if (sp.SampledAttachments != null)
                                for (int q = 0; q < sp.SampledAttachments.Length; q++)
                                {
                                    var view = Attachments[sp.SampledAttachments[q].Name].View;
                                    if (sp.SampledAttachments[q].BaseLevel != 0 | sp.SampledAttachments[q].Levels != Attachments[sp.SampledAttachments[q].Name].Mipmaps.Length)
                                    {
                                        if (sp.SampledAttachments[q].Levels == 1)
                                            view = Attachments[sp.SampledAttachments[q].Name].Mipmaps[sp.SampledAttachments[q].BaseLevel];
                                        else if (sp.SampledAttachments[q].Levels != Attachments[sp.SampledAttachments[q].Name].Mipmaps.Length)
                                            throw new NotImplementedException("Can't handle arbitrary mip requirements.");
                                    }
                                    switch (sp.SampledAttachments[q].DescriptorType)
                                    {
                                        case DescriptorType.CombinedImageSampler:
                                        case DescriptorType.SampledImage:
                                        case DescriptorType.StorageImage:
                                            bp.DescriptorLayout.Add(bindingBase + sp.SampledAttachments[q].BindingIndex, sp.SampledAttachments[q].DescriptorType, 1, ShaderType.All);
                                            break;
                                    }
                                    curBindingTop = System.Math.Max(bindingBase + sp.SampledAttachments[q].BindingIndex, curBindingTop) + 1;
                                }

                            if (sp.Textures != null)
                                for (int q = 0; q < sp.Textures.Length; q++)
                                {
                                    switch (sp.Textures[q].DescriptorType)
                                    {
                                        case DescriptorType.CombinedImageSampler:
                                        case DescriptorType.SampledImage:
                                        case DescriptorType.StorageImage:
                                            bp.DescriptorLayout.Add(bindingBase + sp.Textures[q].BindingIndex, sp.Textures[q].DescriptorType, 1, ShaderType.All);
                                            break;
                                    }
                                    curBindingTop = System.Math.Max(bindingBase + sp.Textures[q].BindingIndex, curBindingTop) + 1;
                                }
                            bindingBase = curBindingTop;
                        }
                        bp.DescriptorLayout.Build(DeviceIndex, 1);
                        bp.DescriptorSet = new DescriptorSet();
                        bp.DescriptorSet.Layout = bp.DescriptorLayout;
                        bp.DescriptorSet.Pool = Pool;
                        bp.DescriptorSet.Build(DeviceIndex);

                        bindingBase = 0;
                        for (int j = 0; j < shaderParamsList.Length; j++)
                        {
                            uint curBindingTop = bindingBase;
                            var sp = shaderParamsList[j].Params;
                            if (sp.Buffers != null)
                                for (int q = 0; q < sp.Buffers.Length; q++)
                                {
                                    switch (sp.Buffers[q].DescriptorType)
                                    {
                                        case DescriptorType.StorageBuffer:
                                            for (uint k = 0; k < sp.Buffers[q].DeviceBuffer.Length; k++)
                                                bp.DescriptorSet.Set(bindingBase + sp.Buffers[q].BindingIndex, k, sp.Buffers[q].DeviceBuffer[k], 0, sp.Buffers[q].DeviceBuffer[k].Size);
                                            break;
                                        case DescriptorType.StorageTexelBuffer:
                                            for (uint k = 0; k < sp.Buffers[q].View.Length; k++)
                                                bp.DescriptorSet.Set(bindingBase + sp.Buffers[q].BindingIndex, k, sp.Buffers[q].View[k]);
                                            break;
                                        case DescriptorType.UniformBuffer:
                                            for (uint k = 0; k < sp.Buffers[q].DeviceBuffer.Length; k++)
                                                bp.DescriptorSet.Set(bindingBase + sp.Buffers[q].BindingIndex, k, sp.Buffers[q].DeviceBuffer[k], 0, sp.Buffers[q].DeviceBuffer[k].Size);
                                            break;
                                        case DescriptorType.UniformBufferDynamic:
                                            for (uint k = 0; k < sp.Buffers[q].DeviceBuffer.Length; k++)
                                                bp.DescriptorSet.Set(bindingBase + sp.Buffers[q].BindingIndex, k, sp.Buffers[q].DeviceBuffer[k], 0, sp.Buffers[q].DeviceBuffer[k].Size);
                                            break;
                                        case DescriptorType.UniformTexelBuffer:
                                            for (uint k = 0; k < sp.Buffers[q].View.Length; k++)
                                                bp.DescriptorSet.Set(bindingBase + sp.Buffers[q].BindingIndex, k, sp.Buffers[q].View[k]);
                                            break;
                                    }
                                    curBindingTop = System.Math.Max(bindingBase + sp.Buffers[q].BindingIndex, curBindingTop) + 1;
                                }

                            if (sp.SampledAttachments != null)
                                for (int q = 0; q < sp.SampledAttachments.Length; q++)
                                {
                                    var view = Attachments[sp.SampledAttachments[q].Name].View;
                                    if (sp.SampledAttachments[q].BaseLevel != 0 | sp.SampledAttachments[q].Levels != Attachments[sp.SampledAttachments[q].Name].Mipmaps.Length)
                                    {
                                        if (sp.SampledAttachments[q].Levels == 1)
                                            view = Attachments[sp.SampledAttachments[q].Name].Mipmaps[sp.SampledAttachments[q].BaseLevel];
                                        else if (sp.SampledAttachments[q].Levels != Attachments[sp.SampledAttachments[q].Name].Mipmaps.Length)
                                            throw new NotImplementedException("Can't handle arbitrary mip requirements.");
                                    }
                                    switch (sp.SampledAttachments[q].DescriptorType)
                                    {
                                        case DescriptorType.CombinedImageSampler:
                                            bp.DescriptorSet.Set(bindingBase + sp.SampledAttachments[q].BindingIndex, 0, view, sp.SampledAttachments[q].ImageSampler);
                                            break;
                                        case DescriptorType.SampledImage:
                                            bp.DescriptorSet.Set(bindingBase + sp.SampledAttachments[q].BindingIndex, 0, view, false);
                                            break;
                                        case DescriptorType.StorageImage:
                                            bp.DescriptorSet.Set(bindingBase + sp.SampledAttachments[q].BindingIndex, 0, view, true);
                                            break;
                                    }
                                    curBindingTop = System.Math.Max(bindingBase + sp.SampledAttachments[q].BindingIndex, curBindingTop) + 1;
                                }

                            if (sp.Textures != null)
                                for (int q = 0; q < sp.Textures.Length; q++)
                                {
                                    switch (sp.Textures[q].DescriptorType)
                                    {
                                        case DescriptorType.CombinedImageSampler:
                                            bp.DescriptorSet.Set(bindingBase + sp.Textures[q].BindingIndex, 0, sp.Textures[q].View, sp.Textures[q].ImageSampler);
                                            break;
                                        case DescriptorType.SampledImage:
                                            bp.DescriptorSet.Set(bindingBase + sp.Textures[q].BindingIndex, 0, sp.Textures[q].View, false);
                                            break;
                                        case DescriptorType.StorageImage:
                                            bp.DescriptorSet.Set(bindingBase + sp.Textures[q].BindingIndex, 0, sp.Textures[q].View, true);
                                            break;
                                    }
                                    curBindingTop = System.Math.Max(bindingBase + sp.Textures[q].BindingIndex, curBindingTop) + 1;
                                }

                            bindingBase = curBindingTop;
                        }
                    }
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
                                bp.Framebuffer[AttachmentKind.DepthAttachment] = Attachments[p.DepthAttachment.Name].Mipmaps[p.DepthAttachment.BaseLevel];
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
                                bp.Framebuffer[AttachmentKind.ColorAttachment0 + j] = Attachments[p.AttachmentUsage[j].Name].Mipmaps[p.DepthAttachment.BaseLevel];
                            }
                            bp.RenderPass.Build(DeviceIndex);
                            bp.Framebuffer.RenderPass = bp.RenderPass;
                            bp.Framebuffer.Build(DeviceIndex);

                            //Setup the pipeline
                            bp.PipelineLayout = new PipelineLayout()
                            {
                                Descriptors = bp.DescriptorLayout.Layouts.Count == 0 ? null : new DescriptorSet[] { bp.DescriptorSet }
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
                                Descriptors = bp.DescriptorLayout.Layouts.Count == 0 ? null : new DescriptorSet[] { bp.DescriptorSet }
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
                                Descriptors = bp.DescriptorLayout.Layouts.Count == 0 ? null : new DescriptorSet[] { bp.DescriptorSet }
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
            foreach (var e in GraphicsPasses)
            {
                for (int i = 0; i < GraphicsDevice.MaxFramesInFlight; i++)
                {
                    var cmdbuf = e.Commands[i];
                    cmdbuf.Reset();
                    cmdbuf.BeginRecording();
                    var gpass = e.Pass as GraphicsPass;
                    var drCmd = gpass.DrawCmd;

                    //Bind the pipeline
                    cmdbuf.SetPipeline(e.GraphicsPipeline, 0);

                    //Bind descriptors
                    if (e.DescriptorSet != null) cmdbuf.SetDescriptors(e.PipelineLayout, e.DescriptorSet, DescriptorBindPoint.Graphics, 0);

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
                                cmdbuf.DrawIndirect(indirDrCmd.DrawBuffer, indirDrCmd.DrawBufferOffset, indirDrCmd.CountBuffer, indirDrCmd.CountOffset, indirDrCmd.MaxCount, indirDrCmd.Stride);
                            }
                            break;
                        case DrawCmdType.IndexedIndirect:
                            {
                                var indirDrCmd = drCmd as IndexedIndirectDrawCmd;
                                cmdbuf.DrawIndexedIndirect(indirDrCmd.IndexBuffer, indirDrCmd.IndexOffset, indirDrCmd.IndexType, indirDrCmd.DrawBuffer, indirDrCmd.DrawBufferOffset, indirDrCmd.CountBuffer, indirDrCmd.CountOffset, indirDrCmd.MaxCount, indirDrCmd.Stride);
                            }
                            break;
                    }
                    cmdbuf.EndRenderPass();
                    cmdbuf.EndRecording();
                }
            }
        }

        private void SubmitTransfer()
        {
            foreach (var e in TransferPasses)
            {
                for (int i = 0; i < GraphicsDevice.MaxFramesInFlight; i++)
                {
                    var cmdbuf = e.Commands[i];
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
                    cmdbuf.EndRecording();
                }
            }
        }

        private void SubmitAsyncCompute()
        {
            foreach (var e in AsyncComputePasses)
            {
                for (int i = 0; i < GraphicsDevice.MaxFramesInFlight; i++)
                {
                    var cmdbuf = e.Commands[i];
                    cmdbuf.Reset();
                    cmdbuf.BeginRecording();

                    if (e.Pass.PassType == PassType.AsyncCompute)
                    {
                        var cpass = e.Pass as AsyncComputePass;

                        cmdbuf.SetPipeline(e.ComputePipeline);
                        if (e.DescriptorSet != null) cmdbuf.SetDescriptors(e.PipelineLayout, e.DescriptorSet, DescriptorBindPoint.Compute, 0);
                        cmdbuf.Dispatch(cpass.GroupX, cpass.GroupY, cpass.GroupZ);
                    }
                    else if (e.Pass.PassType == PassType.AsyncIndirectCompute)
                    {
                        var cpass = e.Pass as AsyncIndirectComputePass;

                        cmdbuf.SetPipeline(e.ComputePipeline);
                        if (e.DescriptorSet != null) cmdbuf.SetDescriptors(e.PipelineLayout, e.DescriptorSet, DescriptorBindPoint.Compute, 0);
                        cmdbuf.DispatchIndirect(cpass.Buffer, cpass.Offset);
                    }
                    cmdbuf.EndRecording();
                }
            }
        }

        public void Execute(bool rebuildCmds)
        {
            ComputeFences[GraphicsDevice.CurrentFrameNumber].Wait();
            TransferFences[GraphicsDevice.CurrentFrameNumber].Wait();

            ComputeFences[GraphicsDevice.CurrentFrameNumber].Reset();
            TransferFences[GraphicsDevice.CurrentFrameNumber].Reset();
            GraphicsDevice.AcquireFrame();

            if (rebuildCmds)
            {
                //Record and Submit command buffers across multiple threads
                //var graphicsThd = new Thread(SubmitGraphics);
                //var transferThd = new Thread(SubmitTransfer);
                //var asyncCompThd = new Thread(SubmitAsyncCompute);

                //transferThd.Start();
                //asyncCompThd.Start();
                //graphicsThd.Start();

                //transferThd.Join();
                //asyncCompThd.Join();
                //graphicsThd.Join();

                SubmitGraphics();
                SubmitTransfer();
                SubmitAsyncCompute();
            }

            var asyncPasses = AsyncComputePasses.ToArray();//.Where(a => a.Pass.Active).ToArray();
            var transferPasses = TransferPasses.ToArray();//.Where(a => a.Pass.Active).ToArray();
            var graphicsPasses = GraphicsPasses.ToArray();//.Where(a => a.Pass.Active).ToArray();
            int maxLen = System.Math.Max(graphicsPasses.Length, System.Math.Max(transferPasses.Length, asyncPasses.Length));

            var finalGfxSem = finalGraphicsSemaphore;
            for (int i = 0; i < maxLen; i++)
            {
                if (i < asyncPasses.Length)
                    if (!asyncPasses[i].Pass.Active)
                        asyncPasses[i].SignalSemaphores.Signal(GraphicsDevice.CurrentFrameCount + 1);
                    else
                        GraphicsDevice.GetDeviceInfo(DeviceIndex).ComputeQueue.SubmitCommandBuffer(asyncPasses[i].Commands[GraphicsDevice.CurrentFrameID], asyncPasses[i].WaitSemaphores, new GpuSemaphore[] { asyncPasses[i].SignalSemaphores }, i == asyncPasses.Length - 1 ? ComputeFences[GraphicsDevice.CurrentFrameNumber] : null);

                if (i < transferPasses.Length)
                    if (!transferPasses[i].Pass.Active)
                        transferPasses[i].SignalSemaphores.Signal(GraphicsDevice.CurrentFrameCount + 1);
                    else
                        GraphicsDevice.GetDeviceInfo(DeviceIndex).TransferQueue.SubmitCommandBuffer(transferPasses[i].Commands[GraphicsDevice.CurrentFrameID], transferPasses[i].WaitSemaphores, new GpuSemaphore[] { transferPasses[i].SignalSemaphores }, i == transferPasses.Length - 1 ? TransferFences[GraphicsDevice.CurrentFrameNumber] : null);

                if (i < graphicsPasses.Length)
                    if (!graphicsPasses[i].Pass.Active)
                        graphicsPasses[i].SignalSemaphores.Signal(GraphicsDevice.CurrentFrameCount + 1);
                    else
                    {
                        GraphicsDevice.GetDeviceInfo(DeviceIndex).GraphicsQueue.SubmitCommandBuffer(graphicsPasses[i].Commands[GraphicsDevice.CurrentFrameID], graphicsPasses[i].WaitSemaphores, new GpuSemaphore[] { graphicsPasses[i].SignalSemaphores }, null);
                        finalGfxSem = graphicsPasses[i].SignalSemaphores;
                    }
            }

            //Submit a blit to screen operation dependent on the last operation
            GraphicsDevice.GetDeviceInfo(0).GraphicsQueue.SubmitCommandBuffer(outputCmds[GraphicsDevice.CurrentFrameID],
                new GpuSemaphore[] { GraphicsDevice.ImageAvailableSemaphore[GraphicsDevice.CurrentFrameNumber], finalGfxSem },
                new GpuSemaphore[] { GraphicsDevice.FrameFinishedSemaphore[GraphicsDevice.CurrentFrameNumber] },
                GraphicsDevice.InflightFences[GraphicsDevice.CurrentFrameNumber]);
            GraphicsDevice.PresentFrame();
        }

        class BuiltShaderParameterSet
        {
            public string Name { get => Params.Name; }
            public ShaderParameterSet Params;
        }

        class BuiltAttachment
        {
            public string Name { get => Attachment.Name; }
            public AttachmentInfo Attachment;
            public ImageLayout CurrentLayout;
            public Image Image;
            public ImageView View;
            public ImageView[] Mipmaps;

            public PipelineStage LastWriteStage;
            public int LastWriteIndex = -1;  //TODO use this to adjust barrier location to minimize possible stalls
        }

        class BuiltPass
        {
            public string Name { get => Pass.Name; }
            public DescriptorSet DescriptorSet;
            public DescriptorLayout DescriptorLayout;
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
