using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RadeonRaysSharp.Raw;
using static RadeonRaysSharp.Raw.RadeonRays;

namespace Kokoro.Graphics.Framegraph
{
    public enum GpuCmd
    {
        None,
        Draw,
        DrawIndexed,

        Build,
        Intersect,

        Stage,
        Download,

        Compute,
    }

    public class GpuOp
    {
        //Pass Name
        public string PassName { get; set; }
        public string[] Resources { get; set; }

        //Only used to build the framebuffer
        public string[] ColorAttachments { get; set; }
        public string DepthAttachment { get; set; }

        public GpuCmd Cmd { get; set; } = GpuCmd.None;
        public uint BaseVertex { get; set; } = 0;
        public uint BaseInstance { get; set; } = 0;
        public uint VertexCount { get; set; }
        public uint InstanceCount { get; set; } = 1;
        public string IndexBuffer { get; set; }
        public string IndirectBuffer { get; set; }
        public ulong IndexBufferOffset { get; set; }
        public IndexType IndexType { get; set; }
        public uint IndexCount { get; set; }
        public uint FirstIndex { get; set; }
        public IntPtr PushConstants { get; set; }
        public uint PushConstantsLen { get; set; }

        //Radeon Rays
        public RayGeometry RRGeometry;
        public RayIntersections RRIntersections;

        internal CommandQueueKind QueueKind;

        public GpuOp() { }
        public GpuOp(GpuOp src)
        {
            Cmd = src.Cmd;
            PassName = src.PassName;
            Resources = src.Resources;
            ColorAttachments = src.ColorAttachments;
            DepthAttachment = src.DepthAttachment;
            QueueKind = src.QueueKind;

            RRGeometry = src.RRGeometry;
            RRIntersections = src.RRIntersections;
        }
    }

    public class FrameGraph
    {
        int DeviceIndex;
        int SemaphoreCounter;
        SemaphoreSlim buildLock;
        DescriptorPool globalDescriptorPool;
        DescriptorLayout globalDescriptorLayout;
        DescriptorSet globalDescriptorSet;

        ConcurrentDictionary<string, SpecializedShader> Shaders;
        ConcurrentDictionary<string, GraphicsPass> GraphicsPasses;
        ConcurrentDictionary<string, ComputePass> ComputePasses;
        ConcurrentDictionary<string, BufferTransferPass> BufferTransferPasses;
        ConcurrentDictionary<string, ImageTransferPass> ImageTransferPasses;
        ConcurrentDictionary<string, ImageView> ImageViews;
        ConcurrentDictionary<string, GpuBuffer> GpuBuffers;
        ConcurrentDictionary<string, GpuBufferView> GpuBufferViews;
        ConcurrentDictionary<string, Sampler> Samplers;

        ConcurrentQueue<GpuOp[]> Ops;

        Dictionary<RenderLayout, RenderPass> RenderPasses;
        Dictionary<string, Framebuffer> Framebuffers;
        Dictionary<string, PipelineLayout> PipelineLayouts;
        Dictionary<string, GraphicsPipeline> GraphicsPipelines;
        Dictionary<string, ComputePipeline> ComputePipelines;

        List<GpuSemaphore>[] SemaphoreCache;

        CommandPool[] GraphicsCmdPool;
        CommandPool[] TransferCmdPool;
        CommandPool[] AsyncComputeCmdPool;

        CompiledCommandBuffer[][] graphCmds;
        CompiledCommandBuffer[][] compCmds;
        CompiledCommandBuffer[][] transCmds;
        Fence[] compFences;
        Fence[] transFences;

        CommandBuffer[] transitionBuffer;

        class CompiledCommandBuffer
        {
            public CommandBuffer CmdBuffer;
            public GpuSemaphore[] signalling;
            public GpuSemaphore[] waiting;
            public PipelineStage SrcStage;
            public PipelineStage DstStage;
        }

        public FrameGraph(int deviceIndex)
        {
            DeviceIndex = deviceIndex;
            buildLock = new SemaphoreSlim(1);

            Shaders = new ConcurrentDictionary<string, SpecializedShader>(Environment.ProcessorCount, 1024);
            GraphicsPasses = new ConcurrentDictionary<string, GraphicsPass>(Environment.ProcessorCount, 1024);
            ComputePasses = new ConcurrentDictionary<string, ComputePass>(Environment.ProcessorCount, 1024);
            BufferTransferPasses = new ConcurrentDictionary<string, BufferTransferPass>(Environment.ProcessorCount, 1024);
            ImageTransferPasses = new ConcurrentDictionary<string, ImageTransferPass>(Environment.ProcessorCount, 1024);
            ImageViews = new ConcurrentDictionary<string, ImageView>(Environment.ProcessorCount, 1024);
            GpuBuffers = new ConcurrentDictionary<string, GpuBuffer>(Environment.ProcessorCount, 1024);
            GpuBufferViews = new ConcurrentDictionary<string, GpuBufferView>(Environment.ProcessorCount, 1024);
            Samplers = new ConcurrentDictionary<string, Sampler>(Environment.ProcessorCount, 1024);

            RenderPasses = new Dictionary<RenderLayout, RenderPass>();
            Framebuffers = new Dictionary<string, Framebuffer>();
            PipelineLayouts = new Dictionary<string, PipelineLayout>();
            GraphicsPipelines = new Dictionary<string, GraphicsPipeline>();
            ComputePipelines = new Dictionary<string, ComputePipeline>();

            Ops = new ConcurrentQueue<GpuOp[]>();

            SemaphoreCache = new List<GpuSemaphore>[GraphicsDevice.MaxFramesInFlight];
            GraphicsCmdPool = new CommandPool[GraphicsDevice.MaxFramesInFlight];
            TransferCmdPool = new CommandPool[GraphicsDevice.MaxFramesInFlight];
            AsyncComputeCmdPool = new CommandPool[GraphicsDevice.MaxFramesInFlight];
            graphCmds = new CompiledCommandBuffer[GraphicsDevice.MaxFramesInFlight][];
            compCmds = new CompiledCommandBuffer[GraphicsDevice.MaxFramesInFlight][];
            transCmds = new CompiledCommandBuffer[GraphicsDevice.MaxFramesInFlight][];
            compFences = new Fence[GraphicsDevice.MaxFramesInFlight];
            transFences = new Fence[GraphicsDevice.MaxFramesInFlight];
            transitionBuffer = new CommandBuffer[GraphicsDevice.MaxFramesInFlight];
            for (int i = 0; i < GraphicsDevice.MaxFramesInFlight; i++)
            {
                graphCmds[i] = new CompiledCommandBuffer[0];
                compCmds[i] = new CompiledCommandBuffer[0];
                transCmds[i] = new CompiledCommandBuffer[0];

                compFences[i] = new Fence()
                {
                    Name = $"Compute_fence_{i}",
                    CreateSignaled = true
                };
                compFences[i].Build(deviceIndex);

                transFences[i] = new Fence()
                {
                    Name = $"Transfer_fence_{i}",
                    CreateSignaled = true
                };
                transFences[i].Build(deviceIndex);

                SemaphoreCache[i] = new List<GpuSemaphore>(1024);
                for (int j = 0; j < 1024; j++)
                {
                    SemaphoreCache[i].Add(new GpuSemaphore()
                    {
                        Name = $"Semaphore_{i}_{j}",
                    });
                    SemaphoreCache[i][j].Build(deviceIndex, false, 0);
                }

                GraphicsCmdPool[i] = new CommandPool()
                {
                    Name = $"Graphics_fg_{i}",
                    Transient = true
                };
                GraphicsCmdPool[i].Build(DeviceIndex, CommandQueueKind.Graphics);

                TransferCmdPool[i] = new CommandPool()
                {
                    Name = $"Transfer_fg_{i}",
                    Transient = true
                };
                TransferCmdPool[i].Build(DeviceIndex, CommandQueueKind.Transfer);

                AsyncComputeCmdPool[i] = new CommandPool()
                {
                    Name = $"Compute_fg_{i}",
                    Transient = true
                };
                AsyncComputeCmdPool[i].Build(DeviceIndex, CommandQueueKind.Compute);
            }
        }

        #region Registration
        public void RegisterShader(SpecializedShader shader)
        {
            Shaders.AddOrUpdate(shader.Name, shader, (a, b) => shader);
        }

        public void RegisterGraphicsPass(GraphicsPass graphicsPass)
        {
            GraphicsPasses.AddOrUpdate(graphicsPass.Name, graphicsPass, (a, b) => graphicsPass);
        }

        public void RegisterComputePass(ComputePass computePass)
        {
            ComputePasses.AddOrUpdate(computePass.Name, computePass, (a, b) => computePass);
        }

        public void RegisterBufferTransferPass(BufferTransferPass bufferTransferPass)
        {
            BufferTransferPasses.AddOrUpdate(bufferTransferPass.Name, bufferTransferPass, (a, b) => bufferTransferPass);
        }

        public void RegisterImageTransferPass(ImageTransferPass imageTransferPass)
        {
            ImageTransferPasses.AddOrUpdate(imageTransferPass.Name, imageTransferPass, (a, b) => imageTransferPass);
        }

        public void RegisterResource(ImageView imageView)
        {
            ImageViews.AddOrUpdate(imageView.Name, imageView, (a, b) => imageView);
        }

        public void RegisterResource(Sampler sampler)
        {
            Samplers.AddOrUpdate(sampler.Name, sampler, (a, b) => sampler);
        }

        public void RegisterResource(GpuBuffer gpuBuffer)
        {
            GpuBuffers.AddOrUpdate(gpuBuffer.Name, gpuBuffer, (a, b) => gpuBuffer);
        }

        public void RegisterResource(GpuBufferView gpuBufferView)
        {
            GpuBufferViews.AddOrUpdate(gpuBufferView.Name, gpuBufferView, (a, b) => gpuBufferView);
        }
        #endregion

        public void QueueOp(params GpuOp[] ops)
        {
            Ops.Enqueue(ops);
        }

        private RenderPass CreateRenderPass(ref RenderLayout layout, string passname)
        {
            if (RenderPasses.ContainsKey(layout))
                return RenderPasses[layout];

            RenderPass pass = new RenderPass();
            pass.Name = passname;
            //Setup currently available state, don't have built-in transitions, we will handle them ourselves
            //because it is important to absolutely minimize the number of these
            if (layout.Color != null)
            {
                pass.ColorAttachments = new RenderPassEntry[layout.Color.Length];
                for (int i = 0; i < pass.ColorAttachments.Length; i++)
                {
                    pass.ColorAttachments[i] = new RenderPassEntry();
                    pass.ColorAttachments[i].InitialLayout = layout.Color[i].DesiredLayout;
                    pass.ColorAttachments[i].StartLayout = layout.Color[i].DesiredLayout;
                    pass.ColorAttachments[i].FinalLayout = layout.Color[i].DesiredLayout;
                    pass.ColorAttachments[i].Format = layout.Color[i].Format;
                    pass.ColorAttachments[i].LoadOp = layout.Color[i].LoadOp;
                    pass.ColorAttachments[i].StoreOp = layout.Color[i].StoreOp;
                }
            }
            if (layout.Depth != null)
            {
                pass.DepthAttachment = new RenderPassEntry();
                pass.DepthAttachment.InitialLayout = layout.Depth.DesiredLayout;
                pass.DepthAttachment.StartLayout = layout.Depth.DesiredLayout;
                pass.DepthAttachment.FinalLayout = layout.Depth.DesiredLayout;
                pass.DepthAttachment.Format = layout.Depth.Format;
                pass.DepthAttachment.LoadOp = layout.Depth.LoadOp;
                pass.DepthAttachment.StoreOp = layout.Depth.StoreOp;
            }
            pass.Build(DeviceIndex);

            RenderPasses[layout] = pass;
            return pass;
        }
        private string GetFramebufferName(string[] colorAttachments, string depthAttachment)
        {
            string framebufferName = "";
            if (colorAttachments != null)
                for (int i = 0; i < colorAttachments.Length; i++)
                    framebufferName += "color_" + colorAttachments[i] + ",";
            else
                framebufferName += "_sys_color_none,";
            if (depthAttachment != null)
                framebufferName += "depth_" + depthAttachment;
            return framebufferName;
        }
        private Framebuffer CreateFramebuffer(string[] colorAttachments, string depthAttachment, ref RenderLayout renderLayout)
        {
            var name = GetFramebufferName(colorAttachments, depthAttachment);
            if (Framebuffers.ContainsKey(name))
                return Framebuffers[name];

            if (!RenderPasses.ContainsKey(renderLayout))
                throw new Exception($"Expected RenderPass {renderLayout} to have been built.");

            Framebuffer fbuf = new Framebuffer();
            fbuf.Name = name;
            fbuf.RenderPass = RenderPasses[renderLayout];
            if (colorAttachments != null && colorAttachments.Length > 0)
            {
                fbuf.ColorAttachments = new ImageView[colorAttachments.Length];
                for (int i = 0; i < colorAttachments.Length; i++)
                {
                    fbuf.ColorAttachments[i] = ImageViews[colorAttachments[i]];
                    fbuf.Width = fbuf.ColorAttachments[i].Width;
                    fbuf.Height = fbuf.ColorAttachments[i].Height;
                }
            }
            if (depthAttachment != null)
            {
                fbuf.DepthAttachment = ImageViews[depthAttachment];
                fbuf.Width = fbuf.DepthAttachment.Width;
                fbuf.Height = fbuf.DepthAttachment.Height;
            }
            fbuf.Build(DeviceIndex);

            Framebuffers[name] = fbuf;
            return fbuf;
        }
        private PipelineLayout CreatePipelineLayout(DescriptorSetup descriptorSetup, string passname)
        {
            if (PipelineLayouts.ContainsKey(passname))
                return PipelineLayouts[passname];

            PipelineLayout layout = new PipelineLayout();
            layout.Name = passname;
            layout.Descriptors = new DescriptorSet[] { globalDescriptorSet };

            if (descriptorSetup.PushConstants != null)
            {
                layout.PushConstants = new PushConstantRange[descriptorSetup.PushConstants.Length];
                for (int i = 0; i < layout.PushConstants.Length; i++)
                {
                    layout.PushConstants[i] = new PushConstantRange()
                    {
                        Offset = descriptorSetup.PushConstants[i].Offset,
                        Size = descriptorSetup.PushConstants[i].Size,
                        Stages = descriptorSetup.PushConstants[i].Stages
                    };
                }
            }
            layout.Build(DeviceIndex);

            PipelineLayouts[passname] = layout;
            return layout;
        }
        private void CompileDescriptorConfig(DescriptorSetup config)
        {
            if (config.Descriptors != null)
                for (int i = 0; i < config.Descriptors.Length; i++)
                {
                    globalDescriptorPool.Add(config.Descriptors[i].DescriptorType, config.Descriptors[i].Count);
                    globalDescriptorLayout.Add(config.Descriptors[i].Index, config.Descriptors[i].DescriptorType, config.Descriptors[i].Count, ShaderType.All);
                }
        }
        private GpuSemaphore AllocateSemaphore(string name)
        {
            if (SemaphoreCache[GraphicsDevice.CurrentFrameID].Count <= SemaphoreCounter)
            {
                var sem = new GpuSemaphore()
                {
                    Name = name
                };
                sem.Build(DeviceIndex, false, 0);
                SemaphoreCache[GraphicsDevice.CurrentFrameID].Add(sem);
            }
            return SemaphoreCache[GraphicsDevice.CurrentFrameID][SemaphoreCounter++];
        }
        public void GatherDescriptors()
        {
            buildLock.Wait();

            globalDescriptorPool = new DescriptorPool();
            globalDescriptorPool.Name = "GlobalDescriptorPool";

            globalDescriptorLayout = new DescriptorLayout();
            globalDescriptorLayout.Name = "GlobalDescriptorLayout";

            //globalDescriptorPool.Add(DescriptorType.UniformBuffer, 1);
            //globalDescriptorLayout.Add(0, DescriptorType.UniformBuffer, 1, ShaderType.All);

            var gPipes = GraphicsPasses.Values.ToArray();
            for (int i = 0; i < gPipes.Length; i++)
                CompileDescriptorConfig(gPipes[i].DescriptorSetup);

            var cPIpes = ComputePasses.Values.ToArray();
            for (int i = 0; i < cPIpes.Length; i++)
                CompileDescriptorConfig(cPIpes[i].DescriptorSetup);

            globalDescriptorPool.Build(DeviceIndex, 1);
            globalDescriptorLayout.Build(DeviceIndex);

            globalDescriptorSet = new DescriptorSet();
            globalDescriptorSet.Name = "GlobalDescriptorSet";
            globalDescriptorSet.Layout = globalDescriptorLayout;
            globalDescriptorSet.Pool = globalDescriptorPool;
            globalDescriptorSet.Build(DeviceIndex);

            buildLock.Release();
        }
        public void Build()
        {
            buildLock.Wait();

            SemaphoreCounter = 0;
            GpuSemaphore finalGfxSem = null;
            var Q0 = new LinkedList<GpuOp>();
            var l_graphCmds = new List<CompiledCommandBuffer>();
            var l_acompCmds = new List<CompiledCommandBuffer>();
            var l_transCmds = new List<CompiledCommandBuffer>();

            var activ_graphCmds = new List<CompiledCommandBuffer>();
            var activ_acompCmds = new List<CompiledCommandBuffer>();
            var activ_transCmds = new List<CompiledCommandBuffer>();

            var activ_submissionOrder = new LinkedList<CompiledCommandBuffer>();

            //Assign queue ownership and validate passes
            while (Ops.TryDequeue(out var opSet))
            {
                for (int i = 0; i < opSet.Length; i++)
                {
                    //Label queue kind based on pass info
                    if (GraphicsPasses.ContainsKey(opSet[i].PassName))
                        opSet[i].QueueKind = CommandQueueKind.Graphics;
                    else if (ComputePasses.ContainsKey(opSet[i].PassName))
                        if (ComputePasses[opSet[i].PassName].IsAsync)
                            opSet[i].QueueKind = CommandQueueKind.Compute;
                        else
                            opSet[i].QueueKind = CommandQueueKind.Graphics;
                    else if (BufferTransferPasses.ContainsKey(opSet[i].PassName))
                        opSet[i].QueueKind = CommandQueueKind.Transfer;
                    else if (ImageTransferPasses.ContainsKey(opSet[i].PassName))
                        opSet[i].QueueKind = CommandQueueKind.Transfer;

                    //Validate the pass 
                    if (opSet[i].QueueKind == CommandQueueKind.Graphics && GraphicsPasses.ContainsKey(opSet[i].PassName))
                    {
                        //Make sure that the framebuffer attachment layouts match the graphicspass specification
                        //If layout is 'undefined', override the layout to match graphicspass
                        var gpass = GraphicsPasses[opSet[i].PassName];
                        if (gpass.RenderLayout.Color == null && opSet[i].ColorAttachments != null)
                            throw new Exception();
                        if (gpass.RenderLayout.Color != null)
                        {
                            if (opSet[i].ColorAttachments == null)
                                throw new Exception();
                            if (gpass.RenderLayout.Color.Length != opSet[i].ColorAttachments.Length)
                                throw new Exception();
                        }
                        if (gpass.RenderLayout.Depth == null && opSet[i].DepthAttachment != null)
                            throw new Exception();
                        if (gpass.RenderLayout.Depth != null && opSet[i].DepthAttachment == null)
                            throw new Exception();
                    }

                    Q0.AddLast(opSet[i]);
                }
            }

            //Allocate per pass resources
            foreach (var op in Q0)
            {
                if (ComputePasses.ContainsKey(op.PassName))
                {
                    var computePass = ComputePasses[op.PassName];

                    if (!(computePass is RayPass))
                    {
                        var descriptorSetup = computePass.DescriptorSetup;
                        if (!ComputePipelines.ContainsKey(computePass.Name))
                        {
                            //build the pipeline layout using the descriptors
                            var pipelineLayout = CreatePipelineLayout(descriptorSetup, computePass.Name);

                            //allocate the pipeline object
                            var compPipeline = new ComputePipeline()
                            {
                                Name = computePass.Name,
                                Shader = Shaders[computePass.Shader].Shader,
                                SpecializationData = Shaders[computePass.Shader].SpecializationData,
                                PipelineLayout = pipelineLayout,
                            };
                            ComputePipelines[compPipeline.Name] = compPipeline;
                        }
                    }
                }
                if (GraphicsPasses.ContainsKey(op.PassName))
                {
                    var graphicsPass = GraphicsPasses[op.PassName];
                    var renderLayout = graphicsPass.RenderLayout;
                    var descriptorSetup = graphicsPass.DescriptorSetup;

                    if (op.Resources != null)
                        for (int i = 0; i < op.Resources.Length; i++)
                        {
                            if (GpuBuffers.ContainsKey(op.Resources[i]))
                            {
                                if ((graphicsPass.DescriptorSetup.Descriptors[i].DescriptorType == DescriptorType.UniformBuffer) ||
                                    (graphicsPass.DescriptorSetup.Descriptors[i].DescriptorType == DescriptorType.UniformBufferDynamic) ||
                                    (graphicsPass.DescriptorSetup.Descriptors[i].DescriptorType == DescriptorType.StorageBuffer))
                                    globalDescriptorSet.Set(graphicsPass.DescriptorSetup.Descriptors[i].Index, 0, GpuBuffers[op.Resources[i]], 0, GpuBuffers[op.Resources[i]].Size);
                                else
                                    throw new NotImplementedException("May be an unimplemented descriptor type.");
                            }
                            else if (GpuBufferViews.ContainsKey(op.Resources[i]))
                            {
                                if ((graphicsPass.DescriptorSetup.Descriptors[i].DescriptorType == DescriptorType.UniformTexelBuffer) ||
                                    (graphicsPass.DescriptorSetup.Descriptors[i].DescriptorType == DescriptorType.StorageTexelBuffer))
                                    globalDescriptorSet.Set(graphicsPass.DescriptorSetup.Descriptors[i].Index, 0, GpuBufferViews[op.Resources[i]]);
                                else
                                    throw new NotImplementedException("May be an unimplemented descriptor type.");
                            }
                            else if (ImageViews.ContainsKey(op.Resources[i]))
                            {
                                if (graphicsPass.DescriptorSetup.Descriptors[i].DescriptorType == DescriptorType.SampledImage)
                                    globalDescriptorSet.Set(graphicsPass.DescriptorSetup.Descriptors[i].Index, 0, ImageViews[op.Resources[i]], false);
                                else if (graphicsPass.DescriptorSetup.Descriptors[i].DescriptorType == DescriptorType.StorageImage)
                                    globalDescriptorSet.Set(graphicsPass.DescriptorSetup.Descriptors[i].Index, 0, ImageViews[op.Resources[i]], true);
                                else if (graphicsPass.DescriptorSetup.Descriptors[i].DescriptorType == DescriptorType.CombinedImageSampler)
                                    globalDescriptorSet.Set(graphicsPass.DescriptorSetup.Descriptors[i].Index, 0, ImageViews[op.Resources[i]], graphicsPass.DescriptorSetup.Descriptors[i].ImmutableSampler);
                                else
                                    throw new NotImplementedException("May be an unimplemented descriptor type.");
                            }
                        }

                    //Allocate the renderpass object
                    var rpass = CreateRenderPass(ref renderLayout, graphicsPass.Name);

                    //allocate the framebuffer object
                    CreateFramebuffer(op.ColorAttachments, op.DepthAttachment, ref renderLayout);

                    if (!GraphicsPipelines.ContainsKey(graphicsPass.Name))
                    {
                        //build the pipeline layout
                        var pipelineLayout = CreatePipelineLayout(descriptorSetup, graphicsPass.Name);

                        //allocate the pipeline object
                        var gpipe = new GraphicsPipeline
                        {
                            Name = graphicsPass.Name,
                            Topology = graphicsPass.Topology,
                            DepthClamp = graphicsPass.DepthClamp,
                            RasterizerDiscard = graphicsPass.RasterizerDiscard,
                            LineWidth = graphicsPass.LineWidth,
                            CullMode = graphicsPass.CullMode,
                            EnableBlending = graphicsPass.EnableBlending,
                            DepthTest = graphicsPass.DepthTest,
                            Fill = graphicsPass.Fill,
                            DepthWrite = graphicsPass.DepthWriteEnable,
                            RenderPass = rpass,
                            PipelineLayout = pipelineLayout,
                            ViewportX = graphicsPass.ViewportX,
                            ViewportY = graphicsPass.ViewportY,
                            ViewportWidth = graphicsPass.ViewportWidth,
                            ViewportHeight = graphicsPass.ViewportHeight,
                            ViewportMinDepth = graphicsPass.ViewportMinDepth,
                            ViewportMaxDepth = graphicsPass.ViewportMaxDepth,
                            ViewportDynamic = graphicsPass.ViewportDynamic,

                            Shaders = new ShaderSource[graphicsPass.Shaders.Length],
                            SpecializationData = new Memory<int>[graphicsPass.Shaders.Length]
                        };
                        for (int i = 0; i < gpipe.Shaders.Length; i++)
                        {
                            gpipe.Shaders[i] = Shaders[graphicsPass.Shaders[i]].Shader;
                            gpipe.SpecializationData[i] = Shaders[graphicsPass.Shaders[i]].SpecializationData;
                        }
                        gpipe.Build(DeviceIndex);
                        GraphicsPipelines[graphicsPass.Name] = gpipe;
                    }
                }
            }

            //Fill the command buffers
            bool renderPassBound = false;
            var g_cmd = new CommandBuffer()
            {
                OneTimeSubmit = true,
                Name = $"Graphics_{GraphicsDevice.CurrentFrameID}"
            };
            g_cmd.Build(GraphicsCmdPool[GraphicsDevice.CurrentFrameID]);
            g_cmd.BeginRecording();

            var c_cmd = new CommandBuffer()
            {
                OneTimeSubmit = true,
                Name = $"Compute_{GraphicsDevice.CurrentFrameID}"
            };
            c_cmd.Build(AsyncComputeCmdPool[GraphicsDevice.CurrentFrameID]);
            c_cmd.BeginRecording();

            var t_cmd = new CommandBuffer()
            {
                OneTimeSubmit = true,
                Name = $"Transfer_{GraphicsDevice.CurrentFrameID}"
            };
            t_cmd.Build(TransferCmdPool[GraphicsDevice.CurrentFrameID]);
            t_cmd.BeginRecording();

            var g_waitingSems = new List<GpuSemaphore>();
            var c_waitingSems = new List<GpuSemaphore>();
            var t_waitingSems = new List<GpuSemaphore>();

            var node = Q0.First;
            do
            {
                var op = node.Value;
                CommandBuffer tgt_cmdbuf = null;
                List<GpuSemaphore> waitingSems = null;
                switch (op.QueueKind)
                {
                    case CommandQueueKind.Compute:
                        tgt_cmdbuf = c_cmd;
                        waitingSems = c_waitingSems;
                        break;
                    case CommandQueueKind.Graphics:
                        tgt_cmdbuf = g_cmd;
                        waitingSems = g_waitingSems;
                        break;
                    case CommandQueueKind.Transfer:
                        tgt_cmdbuf = t_cmd;
                        waitingSems = t_waitingSems;
                        break;
                }

                //Check if any resources need barriers, ownership changes
                if (GraphicsPasses.ContainsKey(op.PassName))
                {
                    var pass = GraphicsPasses[op.PassName];
                    if (pass.Resources != null)
                        for (int i = 0; i < pass.Resources.Length; i++)
                        {
                            if (GpuBuffers.ContainsKey(op.Resources[i]))
                            {
                                var resc = GpuBuffers[op.Resources[i]];
                                var bar = new BufferMemoryBarrier()
                                {
                                    Accesses = resc.CurrentAccesses,
                                    Buffer = resc,
                                    Stores = pass.Resources[i].StartAccesses,
                                    Offset = 0,
                                    Size = resc.Size,
                                    DstFamily = op.QueueKind,
                                    SrcFamily = resc.OwningQueue == CommandQueueKind.Ignored ? op.QueueKind : resc.OwningQueue,
                                };
                                if (op.QueueKind != resc.OwningQueue && resc.OwningQueue != CommandQueueKind.Ignored)
                                {
                                    //Split command buffer and setup semaphore
                                    switch (resc.OwningQueue)
                                    {
                                        case CommandQueueKind.Compute:
                                            {
                                                c_cmd.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, new BufferMemoryBarrier[] { bar }, null);
                                                c_cmd.EndRecording();
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = c_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = pass.Resources[i].StartStage,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = c_waitingSems.ToArray()
                                                };
                                                l_acompCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                c_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                c_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Compute_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                c_cmd.Build(AsyncComputeCmdPool[GraphicsDevice.CurrentFrameID]);
                                                c_cmd.BeginRecording();
                                            }
                                            break;
                                        case CommandQueueKind.Graphics:
                                            {
                                                g_cmd.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, new BufferMemoryBarrier[] { bar }, null);
                                                if (renderPassBound)
                                                {
                                                    g_cmd.EndRenderPass();
                                                    renderPassBound = false;
                                                }
                                                g_cmd.EndRecording();
                                                if (l_graphCmds.Count == 0) g_waitingSems.Add(GraphicsDevice.ImageAvailableSemaphore[GraphicsDevice.PrevFrameID]);
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = g_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = pass.Resources[i].StartStage,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = g_waitingSems.ToArray()
                                                };
                                                l_graphCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                g_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                g_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Graphics_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                g_cmd.Build(GraphicsCmdPool[GraphicsDevice.CurrentFrameID]);
                                                g_cmd.BeginRecording();
                                            }
                                            break;
                                        case CommandQueueKind.Transfer:
                                            {
                                                t_cmd.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, new BufferMemoryBarrier[] { bar }, null);
                                                t_cmd.EndRecording();
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = t_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = pass.Resources[i].StartStage,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = t_waitingSems.ToArray()
                                                };
                                                l_transCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                t_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                t_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Transfer_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                t_cmd.Build(TransferCmdPool[GraphicsDevice.CurrentFrameID]);
                                                t_cmd.BeginRecording();
                                            }
                                            break;
                                    }
                                }
                                tgt_cmdbuf.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, new BufferMemoryBarrier[] { bar }, null);
                                resc.CurrentUsageStage = pass.Resources[i].FinalStage;
                                resc.CurrentAccesses = pass.Resources[i].FinalAccesses;
                                resc.OwningQueue = op.QueueKind;
                            }
                            else if (GpuBufferViews.ContainsKey(op.Resources[i]))
                            {
                                var resc = GpuBufferViews[op.Resources[i]];
                                var bar = new BufferMemoryBarrier()
                                {
                                    Accesses = resc.CurrentAccesses,
                                    Buffer = resc.parent,
                                    Stores = pass.Resources[i].StartAccesses,
                                    Offset = 0,
                                    Size = resc.Size,
                                    DstFamily = op.QueueKind,
                                    SrcFamily = resc.OwningQueue == CommandQueueKind.Ignored ? op.QueueKind : resc.OwningQueue,
                                };
                                if (op.QueueKind != resc.OwningQueue && resc.OwningQueue != CommandQueueKind.Ignored)
                                {
                                    //Split command buffer and setup semaphore
                                    switch (resc.OwningQueue)
                                    {
                                        case CommandQueueKind.Compute:
                                            {
                                                c_cmd.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, new BufferMemoryBarrier[] { bar }, null);
                                                c_cmd.EndRecording();
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = c_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = pass.Resources[i].StartStage,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = c_waitingSems.ToArray()
                                                };
                                                l_acompCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                c_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                c_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Compute_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                c_cmd.Build(AsyncComputeCmdPool[GraphicsDevice.CurrentFrameID]);
                                                c_cmd.BeginRecording();
                                            }
                                            break;
                                        case CommandQueueKind.Graphics:
                                            {
                                                g_cmd.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, new BufferMemoryBarrier[] { bar }, null);
                                                if (renderPassBound)
                                                {
                                                    g_cmd.EndRenderPass();
                                                    renderPassBound = false;
                                                }
                                                g_cmd.EndRecording();
                                                if (l_graphCmds.Count == 0) g_waitingSems.Add(GraphicsDevice.ImageAvailableSemaphore[GraphicsDevice.PrevFrameID]);
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = g_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = pass.Resources[i].StartStage,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = g_waitingSems.ToArray()
                                                };
                                                l_graphCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                g_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                g_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Graphics_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                g_cmd.Build(GraphicsCmdPool[GraphicsDevice.CurrentFrameID]);
                                                g_cmd.BeginRecording();
                                            }
                                            break;
                                        case CommandQueueKind.Transfer:
                                            {
                                                t_cmd.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, new BufferMemoryBarrier[] { bar }, null);
                                                t_cmd.EndRecording();
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = t_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = pass.Resources[i].StartStage,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = t_waitingSems.ToArray()
                                                };
                                                l_transCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                t_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                t_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Transfer_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                t_cmd.Build(TransferCmdPool[GraphicsDevice.CurrentFrameID]);
                                                t_cmd.BeginRecording();
                                            }
                                            break;
                                    }
                                }
                                tgt_cmdbuf.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, new BufferMemoryBarrier[] { bar }, null);
                                resc.CurrentUsageStage = pass.Resources[i].FinalStage;
                                resc.CurrentAccesses = pass.Resources[i].FinalAccesses;
                                resc.OwningQueue = op.QueueKind;
                            }
                            else if (ImageViews.ContainsKey(op.Resources[i]))
                            {
                                var resc_data = (ImageViewUsageEntry)pass.Resources[i];
                                var resc = ImageViews[op.Resources[i]];
                                var bar = new ImageMemoryBarrier()
                                {
                                    Accesses = resc.CurrentAccesses,
                                    Image = resc.parent,
                                    Stores = pass.Resources[i].StartAccesses,
                                    BaseArrayLayer = resc_data.BaseArrayLayer,
                                    BaseMipLevel = resc_data.BaseMipLevel,
                                    LayerCount = resc_data.LayerCount,
                                    LevelCount = resc_data.LevelCount,
                                    DstFamily = op.QueueKind,
                                    SrcFamily = resc.OwningQueue == CommandQueueKind.Ignored ? op.QueueKind : resc.OwningQueue,
                                    NewLayout = resc_data.StartLayout,
                                    OldLayout = resc.CurrentLayout,
                                };
                                if (op.QueueKind != resc.OwningQueue && resc.OwningQueue != CommandQueueKind.Ignored)
                                {
                                    //Split command buffer and setup semaphore
                                    switch (resc.OwningQueue)
                                    {
                                        case CommandQueueKind.Compute:
                                            {
                                                c_cmd.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, null, new ImageMemoryBarrier[] { bar });
                                                c_cmd.EndRecording();
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = c_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = pass.Resources[i].StartStage,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = c_waitingSems.ToArray()
                                                };
                                                l_acompCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                c_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                c_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Compute_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                c_cmd.Build(AsyncComputeCmdPool[GraphicsDevice.CurrentFrameID]);
                                                c_cmd.BeginRecording();
                                            }
                                            break;
                                        case CommandQueueKind.Graphics:
                                            {
                                                g_cmd.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, null, new ImageMemoryBarrier[] { bar });
                                                if (renderPassBound)
                                                {
                                                    g_cmd.EndRenderPass();
                                                    renderPassBound = false;
                                                }
                                                g_cmd.EndRecording();
                                                if (l_graphCmds.Count == 0) g_waitingSems.Add(GraphicsDevice.ImageAvailableSemaphore[GraphicsDevice.PrevFrameID]);
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = g_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = pass.Resources[i].StartStage,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = g_waitingSems.ToArray()
                                                };
                                                l_graphCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                g_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                g_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Graphics_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                g_cmd.Build(GraphicsCmdPool[GraphicsDevice.CurrentFrameID]);
                                                g_cmd.BeginRecording();
                                            }
                                            break;
                                        case CommandQueueKind.Transfer:
                                            {
                                                t_cmd.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, null, new ImageMemoryBarrier[] { bar });
                                                t_cmd.EndRecording();
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = t_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = pass.Resources[i].StartStage,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = t_waitingSems.ToArray()
                                                };
                                                l_transCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                t_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                t_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Transfer_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                t_cmd.Build(TransferCmdPool[GraphicsDevice.CurrentFrameID]);
                                                t_cmd.BeginRecording();
                                            }
                                            break;
                                    }
                                }
                                tgt_cmdbuf.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, null, new ImageMemoryBarrier[] { bar });
                                resc.CurrentUsageStage = pass.Resources[i].FinalStage;
                                resc.CurrentAccesses = pass.Resources[i].FinalAccesses;
                                resc.CurrentLayout = resc_data.FinalLayout;
                                resc.OwningQueue = op.QueueKind;
                            }
                        }

                    if (op.ColorAttachments != null)
                        for (int i = 0; i < op.ColorAttachments.Length; i++)
                        {
                            if (ImageViews.ContainsKey(op.ColorAttachments[i]))
                            {
                                var resc_data = pass.RenderLayout.Color[i];
                                var resc = ImageViews[op.ColorAttachments[i]];
                                var bar = new ImageMemoryBarrier()
                                {
                                    Accesses = resc.CurrentAccesses,
                                    Image = resc.parent,
                                    Stores = AccessFlags.ColorAttachmentWrite,
                                    BaseArrayLayer = resc_data.BaseArrayLayer,
                                    BaseMipLevel = resc_data.BaseMipLevel,
                                    LayerCount = resc_data.LayerCount,
                                    LevelCount = resc_data.LevelCount,
                                    DstFamily = op.QueueKind,
                                    SrcFamily = resc.OwningQueue == CommandQueueKind.Ignored ? op.QueueKind : resc.OwningQueue,
                                    NewLayout = resc_data.DesiredLayout,
                                    OldLayout = resc.CurrentLayout,
                                };
                                if (op.QueueKind != resc.OwningQueue && resc.OwningQueue != CommandQueueKind.Ignored)
                                {
                                    //Split command buffer and setup semaphore
                                    switch (resc.OwningQueue)
                                    {
                                        case CommandQueueKind.Compute:
                                            {
                                                c_cmd.Barrier(resc.CurrentUsageStage, PipelineStage.ColorAttachOut, null, new ImageMemoryBarrier[] { bar });
                                                c_cmd.EndRecording();
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = c_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = PipelineStage.ColorAttachOut,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = c_waitingSems.ToArray()
                                                };
                                                l_acompCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                c_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                c_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Compute_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                c_cmd.Build(AsyncComputeCmdPool[GraphicsDevice.CurrentFrameID]);
                                                c_cmd.BeginRecording();
                                            }
                                            break;
                                        case CommandQueueKind.Graphics:
                                            {
                                                g_cmd.Barrier(resc.CurrentUsageStage, PipelineStage.ColorAttachOut, null, new ImageMemoryBarrier[] { bar });
                                                if (renderPassBound)
                                                {
                                                    g_cmd.EndRenderPass();
                                                    renderPassBound = false;
                                                }
                                                g_cmd.EndRecording();
                                                if (l_graphCmds.Count == 0) g_waitingSems.Add(GraphicsDevice.ImageAvailableSemaphore[GraphicsDevice.PrevFrameID]);
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = g_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = PipelineStage.ColorAttachOut,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = g_waitingSems.ToArray()
                                                };
                                                l_graphCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                g_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                g_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Graphics_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                g_cmd.Build(GraphicsCmdPool[GraphicsDevice.CurrentFrameID]);
                                                g_cmd.BeginRecording();
                                            }
                                            break;
                                        case CommandQueueKind.Transfer:
                                            {
                                                t_cmd.Barrier(resc.CurrentUsageStage, PipelineStage.ColorAttachOut, null, new ImageMemoryBarrier[] { bar });
                                                t_cmd.EndRecording();
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = t_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = PipelineStage.ColorAttachOut,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = t_waitingSems.ToArray()
                                                };
                                                l_transCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                t_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                t_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Transfer_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                t_cmd.Build(TransferCmdPool[GraphicsDevice.CurrentFrameID]);
                                                t_cmd.BeginRecording();
                                            }
                                            break;
                                    }
                                }
                                tgt_cmdbuf.Barrier(resc.CurrentUsageStage, PipelineStage.ColorAttachOut, null, new ImageMemoryBarrier[] { bar });
                                resc.CurrentUsageStage = PipelineStage.ColorAttachOut;
                                resc.CurrentAccesses = AccessFlags.ColorAttachmentWrite;
                                resc.CurrentLayout = resc_data.DesiredLayout;
                                resc.OwningQueue = op.QueueKind;
                            }
                        }

                    if (!string.IsNullOrEmpty(op.DepthAttachment))
                        if (ImageViews.ContainsKey(op.DepthAttachment))
                        {
                            var resc_data = pass.RenderLayout.Depth;
                            var resc = ImageViews[op.DepthAttachment];
                            var bar = new ImageMemoryBarrier()
                            {
                                Accesses = resc.CurrentAccesses,
                                Image = resc.parent,
                                Stores = AccessFlags.ColorAttachmentWrite,
                                BaseArrayLayer = resc_data.BaseArrayLayer,
                                BaseMipLevel = resc_data.BaseMipLevel,
                                LayerCount = resc_data.LayerCount,
                                LevelCount = resc_data.LevelCount,
                                DstFamily = op.QueueKind,
                                SrcFamily = resc.OwningQueue == CommandQueueKind.Ignored ? op.QueueKind : resc.OwningQueue,
                                NewLayout = resc_data.DesiredLayout,
                                OldLayout = resc.CurrentLayout,
                            };
                            if (op.QueueKind != resc.OwningQueue && resc.OwningQueue != CommandQueueKind.Ignored)
                            {
                                //Split command buffer and setup semaphore
                                switch (resc.OwningQueue)
                                {
                                    case CommandQueueKind.Compute:
                                        {
                                            c_cmd.Barrier(resc.CurrentUsageStage, PipelineStage.ColorAttachOut, null, new ImageMemoryBarrier[] { bar });
                                            c_cmd.EndRecording();
                                            var c_comp_cmd = new CompiledCommandBuffer
                                            {
                                                CmdBuffer = c_cmd,
                                                SrcStage = resc.CurrentUsageStage,
                                                DstStage = PipelineStage.ColorAttachOut,
                                                signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                waiting = c_waitingSems.ToArray()
                                            };
                                            l_acompCmds.Add(c_comp_cmd);
                                            activ_submissionOrder.AddLast(c_comp_cmd);
                                            c_waitingSems.Clear();
                                            waitingSems.Add(c_comp_cmd.signalling[0]);
                                            c_cmd = new CommandBuffer()
                                            {
                                                OneTimeSubmit = true,
                                                Name = $"Compute_{GraphicsDevice.CurrentFrameID}"
                                            };
                                            c_cmd.Build(AsyncComputeCmdPool[GraphicsDevice.CurrentFrameID]);
                                            c_cmd.BeginRecording();
                                        }
                                        break;
                                    case CommandQueueKind.Graphics:
                                        {
                                            g_cmd.Barrier(resc.CurrentUsageStage, PipelineStage.ColorAttachOut, null, new ImageMemoryBarrier[] { bar });
                                            if (renderPassBound)
                                            {
                                                g_cmd.EndRenderPass();
                                                renderPassBound = false;
                                            }
                                            g_cmd.EndRecording();
                                            if (l_graphCmds.Count == 0) g_waitingSems.Add(GraphicsDevice.ImageAvailableSemaphore[GraphicsDevice.PrevFrameID]);
                                            var c_comp_cmd = new CompiledCommandBuffer
                                            {
                                                CmdBuffer = g_cmd,
                                                SrcStage = resc.CurrentUsageStage,
                                                DstStage = PipelineStage.ColorAttachOut,
                                                signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                waiting = g_waitingSems.ToArray()
                                            };
                                            l_graphCmds.Add(c_comp_cmd);
                                            activ_submissionOrder.AddLast(c_comp_cmd);
                                            g_waitingSems.Clear();
                                            waitingSems.Add(c_comp_cmd.signalling[0]);
                                            g_cmd = new CommandBuffer()
                                            {
                                                OneTimeSubmit = true,
                                                Name = $"Graphics_{GraphicsDevice.CurrentFrameID}"
                                            };
                                            g_cmd.Build(GraphicsCmdPool[GraphicsDevice.CurrentFrameID]);
                                            g_cmd.BeginRecording();
                                        }
                                        break;
                                    case CommandQueueKind.Transfer:
                                        {
                                            t_cmd.Barrier(resc.CurrentUsageStage, PipelineStage.ColorAttachOut, null, new ImageMemoryBarrier[] { bar });
                                            t_cmd.EndRecording();
                                            var c_comp_cmd = new CompiledCommandBuffer
                                            {
                                                CmdBuffer = t_cmd,
                                                SrcStage = resc.CurrentUsageStage,
                                                DstStage = PipelineStage.ColorAttachOut,
                                                signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                waiting = t_waitingSems.ToArray()
                                            };
                                            l_transCmds.Add(c_comp_cmd);
                                            activ_submissionOrder.AddLast(c_comp_cmd);
                                            t_waitingSems.Clear();
                                            waitingSems.Add(c_comp_cmd.signalling[0]);
                                            t_cmd = new CommandBuffer()
                                            {
                                                OneTimeSubmit = true,
                                                Name = $"Transfer_{GraphicsDevice.CurrentFrameID}"
                                            };
                                            t_cmd.Build(TransferCmdPool[GraphicsDevice.CurrentFrameID]);
                                            t_cmd.BeginRecording();
                                        }
                                        break;
                                }
                            }
                            tgt_cmdbuf.Barrier(resc.CurrentUsageStage, PipelineStage.ColorAttachOut, null, new ImageMemoryBarrier[] { bar });
                            resc.CurrentUsageStage = PipelineStage.ColorAttachOut;
                            resc.CurrentAccesses = AccessFlags.ColorAttachmentWrite;
                            resc.CurrentLayout = resc_data.DesiredLayout;
                            resc.OwningQueue = op.QueueKind;
                        }

                    //Process the command
                    g_cmd.SetDescriptors(PipelineLayouts[op.PassName], globalDescriptorSet, DescriptorBindPoint.Graphics, 0);
                    if (op.PushConstants != IntPtr.Zero && op.PushConstantsLen != 0)
                        g_cmd.PushConstants(PipelineLayouts[op.PassName], ShaderType.All, op.PushConstants, op.PushConstantsLen);
                    g_cmd.SetPipeline(GraphicsPipelines[op.PassName], Framebuffers[GetFramebufferName(op.ColorAttachments, op.DepthAttachment)], 0);
                    renderPassBound = true;
                    switch (op.Cmd)
                    {
                        case GpuCmd.Draw:
                            {
                                g_cmd.Draw(op.VertexCount, op.InstanceCount, op.FirstIndex, op.BaseInstance);
                            }
                            break;
                        case GpuCmd.DrawIndexed:
                            {
                                g_cmd.DrawIndexed(GpuBuffers[op.IndexBuffer],
                                                  op.IndexBufferOffset,
                                                  op.IndexType,
                                                  op.IndexCount,
                                                  op.InstanceCount,
                                                  op.FirstIndex,
                                                  (int)op.BaseVertex,
                                                  op.BaseInstance);
                            }
                            break;
                    }
                    g_cmd.EndRenderPass();
                    renderPassBound = false;
                }
                else if (ComputePasses.ContainsKey(op.PassName))
                {
                    var pass = ComputePasses[op.PassName];
                    if (pass.Resources != null)
                        for (int i = 0; i < pass.Resources.Length; i++)
                        {
                            if (GpuBuffers.ContainsKey(op.Resources[i]))
                            {
                                var resc = GpuBuffers[op.Resources[i]];
                                var bar = new BufferMemoryBarrier()
                                {
                                    Accesses = resc.CurrentAccesses,
                                    Buffer = resc,
                                    Stores = pass.Resources[i].StartAccesses,
                                    Offset = 0,
                                    Size = resc.Size,
                                    DstFamily = op.QueueKind,
                                    SrcFamily = resc.OwningQueue == CommandQueueKind.Ignored ? op.QueueKind : resc.OwningQueue,
                                };
                                if (op.QueueKind != resc.OwningQueue && resc.OwningQueue != CommandQueueKind.Ignored)
                                {
                                    //Split command buffer and setup semaphore
                                    switch (resc.OwningQueue)
                                    {
                                        case CommandQueueKind.Compute:
                                            {
                                                c_cmd.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, new BufferMemoryBarrier[] { bar }, null);
                                                c_cmd.EndRecording();
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = c_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = pass.Resources[i].StartStage,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = c_waitingSems.ToArray()
                                                };
                                                l_acompCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                c_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                c_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Compute_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                c_cmd.Build(AsyncComputeCmdPool[GraphicsDevice.CurrentFrameID]);
                                                c_cmd.BeginRecording();
                                            }
                                            break;
                                        case CommandQueueKind.Graphics:
                                            {
                                                g_cmd.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, new BufferMemoryBarrier[] { bar }, null);
                                                if (renderPassBound)
                                                {
                                                    g_cmd.EndRenderPass();
                                                    renderPassBound = false;
                                                }
                                                g_cmd.EndRecording();
                                                if (l_graphCmds.Count == 0) g_waitingSems.Add(GraphicsDevice.ImageAvailableSemaphore[GraphicsDevice.PrevFrameID]);
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = g_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = pass.Resources[i].StartStage,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = g_waitingSems.ToArray()
                                                };
                                                l_graphCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                g_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                g_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Graphics_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                g_cmd.Build(GraphicsCmdPool[GraphicsDevice.CurrentFrameID]);
                                                g_cmd.BeginRecording();
                                            }
                                            break;
                                        case CommandQueueKind.Transfer:
                                            {
                                                t_cmd.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, new BufferMemoryBarrier[] { bar }, null);
                                                t_cmd.EndRecording();
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = t_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = pass.Resources[i].StartStage,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = t_waitingSems.ToArray()
                                                };
                                                l_transCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                t_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                t_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Transfer_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                t_cmd.Build(TransferCmdPool[GraphicsDevice.CurrentFrameID]);
                                                t_cmd.BeginRecording();
                                            }
                                            break;
                                    }
                                }
                                tgt_cmdbuf.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, new BufferMemoryBarrier[] { bar }, null);
                                resc.CurrentUsageStage = pass.Resources[i].FinalStage;
                                resc.CurrentAccesses = pass.Resources[i].FinalAccesses;
                                resc.OwningQueue = op.QueueKind;
                            }
                            else if (GpuBufferViews.ContainsKey(op.Resources[i]))
                            {
                                var resc = GpuBufferViews[op.Resources[i]];
                                var bar = new BufferMemoryBarrier()
                                {
                                    Accesses = resc.CurrentAccesses,
                                    Buffer = resc.parent,
                                    Stores = pass.Resources[i].StartAccesses,
                                    Offset = 0,
                                    Size = resc.Size,
                                    DstFamily = op.QueueKind,
                                    SrcFamily = resc.OwningQueue == CommandQueueKind.Ignored ? op.QueueKind : resc.OwningQueue,
                                };
                                if (op.QueueKind != resc.OwningQueue && resc.OwningQueue != CommandQueueKind.Ignored)
                                {
                                    //Split command buffer and setup semaphore
                                    switch (resc.OwningQueue)
                                    {
                                        case CommandQueueKind.Compute:
                                            {
                                                c_cmd.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, new BufferMemoryBarrier[] { bar }, null);
                                                c_cmd.EndRecording();
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = c_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = pass.Resources[i].StartStage,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = c_waitingSems.ToArray()
                                                };
                                                l_acompCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                c_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                c_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Compute_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                c_cmd.Build(AsyncComputeCmdPool[GraphicsDevice.CurrentFrameID]);
                                                c_cmd.BeginRecording();
                                            }
                                            break;
                                        case CommandQueueKind.Graphics:
                                            {
                                                g_cmd.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, new BufferMemoryBarrier[] { bar }, null);
                                                if (renderPassBound)
                                                {
                                                    g_cmd.EndRenderPass();
                                                    renderPassBound = false;
                                                }
                                                g_cmd.EndRecording();
                                                if (l_graphCmds.Count == 0) g_waitingSems.Add(GraphicsDevice.ImageAvailableSemaphore[GraphicsDevice.PrevFrameID]);
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = g_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = pass.Resources[i].StartStage,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = g_waitingSems.ToArray()
                                                };
                                                l_graphCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                g_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                g_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Graphics_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                g_cmd.Build(GraphicsCmdPool[GraphicsDevice.CurrentFrameID]);
                                                g_cmd.BeginRecording();
                                            }
                                            break;
                                        case CommandQueueKind.Transfer:
                                            {
                                                t_cmd.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, new BufferMemoryBarrier[] { bar }, null);
                                                t_cmd.EndRecording();
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = t_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = pass.Resources[i].StartStage,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = t_waitingSems.ToArray()
                                                };
                                                l_transCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                t_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                t_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Transfer_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                t_cmd.Build(TransferCmdPool[GraphicsDevice.CurrentFrameID]);
                                                t_cmd.BeginRecording();
                                            }
                                            break;
                                    }
                                }
                                tgt_cmdbuf.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, new BufferMemoryBarrier[] { bar }, null);
                                resc.CurrentUsageStage = pass.Resources[i].FinalStage;
                                resc.CurrentAccesses = pass.Resources[i].FinalAccesses;
                                resc.OwningQueue = op.QueueKind;
                            }
                            else if (ImageViews.ContainsKey(op.Resources[i]))
                            {
                                var resc_data = (ImageViewUsageEntry)pass.Resources[i];
                                var resc = ImageViews[op.Resources[i]];
                                var bar = new ImageMemoryBarrier()
                                {
                                    Accesses = resc.CurrentAccesses,
                                    Image = resc.parent,
                                    Stores = pass.Resources[i].StartAccesses,
                                    BaseArrayLayer = resc_data.BaseArrayLayer,
                                    BaseMipLevel = resc_data.BaseMipLevel,
                                    LayerCount = resc_data.LayerCount,
                                    LevelCount = resc_data.LevelCount,
                                    DstFamily = op.QueueKind,
                                    SrcFamily = resc.OwningQueue == CommandQueueKind.Ignored ? op.QueueKind : resc.OwningQueue,
                                    NewLayout = resc_data.StartLayout,
                                    OldLayout = resc.CurrentLayout,
                                };
                                if (op.QueueKind != resc.OwningQueue && resc.OwningQueue != CommandQueueKind.Ignored)
                                {
                                    //Split command buffer and setup semaphore
                                    switch (resc.OwningQueue)
                                    {
                                        case CommandQueueKind.Compute:
                                            {
                                                c_cmd.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, null, new ImageMemoryBarrier[] { bar });
                                                c_cmd.EndRecording();
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = c_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = pass.Resources[i].StartStage,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = c_waitingSems.ToArray()
                                                };
                                                l_acompCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                c_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                c_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Compute_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                c_cmd.Build(AsyncComputeCmdPool[GraphicsDevice.CurrentFrameID]);
                                                c_cmd.BeginRecording();
                                            }
                                            break;
                                        case CommandQueueKind.Graphics:
                                            {
                                                g_cmd.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, null, new ImageMemoryBarrier[] { bar });
                                                if (renderPassBound)
                                                {
                                                    g_cmd.EndRenderPass();
                                                    renderPassBound = false;
                                                }
                                                g_cmd.EndRecording();
                                                if (l_graphCmds.Count == 0) g_waitingSems.Add(GraphicsDevice.ImageAvailableSemaphore[GraphicsDevice.PrevFrameID]);
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = g_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = pass.Resources[i].StartStage,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = g_waitingSems.ToArray()
                                                };
                                                l_graphCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                g_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                g_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Graphics_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                g_cmd.Build(GraphicsCmdPool[GraphicsDevice.CurrentFrameID]);
                                                g_cmd.BeginRecording();
                                            }
                                            break;
                                        case CommandQueueKind.Transfer:
                                            {
                                                t_cmd.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, null, new ImageMemoryBarrier[] { bar });
                                                t_cmd.EndRecording();
                                                var c_comp_cmd = new CompiledCommandBuffer
                                                {
                                                    CmdBuffer = t_cmd,
                                                    SrcStage = resc.CurrentUsageStage,
                                                    DstStage = pass.Resources[i].StartStage,
                                                    signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                                    waiting = t_waitingSems.ToArray()
                                                };
                                                l_transCmds.Add(c_comp_cmd);
                                                activ_submissionOrder.AddLast(c_comp_cmd);
                                                t_waitingSems.Clear();
                                                waitingSems.Add(c_comp_cmd.signalling[0]);
                                                t_cmd = new CommandBuffer()
                                                {
                                                    OneTimeSubmit = true,
                                                    Name = $"Transfer_{GraphicsDevice.CurrentFrameID}"
                                                };
                                                t_cmd.Build(TransferCmdPool[GraphicsDevice.CurrentFrameID]);
                                                t_cmd.BeginRecording();
                                            }
                                            break;
                                    }
                                }
                                tgt_cmdbuf.Barrier(resc.CurrentUsageStage, pass.Resources[i].StartStage, null, new ImageMemoryBarrier[] { bar });
                                resc.CurrentUsageStage = pass.Resources[i].FinalStage;
                                resc.CurrentAccesses = pass.Resources[i].FinalAccesses;
                                resc.CurrentLayout = resc_data.FinalLayout;
                                resc.OwningQueue = op.QueueKind;
                            }
                        }

                    //Process the command
                    switch (op.Cmd)
                    {
                        case GpuCmd.Build:
                            {
                                tgt_cmdbuf.BuildGeometry(op.RRGeometry);
                            }
                            break;
                        case GpuCmd.Intersect:
                            {
                                tgt_cmdbuf.IntersectRays(op.RRIntersections, op.RRGeometry);
                            }
                            break;
                    }
                }
                else if (BufferTransferPasses.ContainsKey(op.PassName))
                {
                    var pass = BufferTransferPasses[op.PassName];
                    if (GpuBuffers.ContainsKey(pass.Source))
                    {
                        var resc = GpuBuffers[pass.Source];
                        var bar = new BufferMemoryBarrier()
                        {
                            Accesses = resc.CurrentAccesses,
                            Buffer = resc,
                            Stores = AccessFlags.TransferWrite,
                            Offset = 0,
                            Size = resc.Size,
                            DstFamily = op.QueueKind,
                            SrcFamily = resc.OwningQueue == CommandQueueKind.Ignored ? op.QueueKind : resc.OwningQueue,
                        };
                        if (op.QueueKind != resc.OwningQueue && resc.OwningQueue != CommandQueueKind.Ignored)
                        {
                            //Split command buffer and setup semaphore
                            switch (resc.OwningQueue)
                            {
                                case CommandQueueKind.Compute:
                                    {
                                        c_cmd.Barrier(resc.CurrentUsageStage, PipelineStage.Transfer, new BufferMemoryBarrier[] { bar }, null);
                                        c_cmd.EndRecording();
                                        var c_comp_cmd = new CompiledCommandBuffer
                                        {
                                            CmdBuffer = c_cmd,
                                            SrcStage = resc.CurrentUsageStage,
                                            DstStage = PipelineStage.Transfer,
                                            signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                            waiting = c_waitingSems.ToArray()
                                        };
                                        l_acompCmds.Add(c_comp_cmd);
                                        activ_submissionOrder.AddLast(c_comp_cmd);
                                        c_waitingSems.Clear();
                                        waitingSems.Add(c_comp_cmd.signalling[0]);
                                        c_cmd = new CommandBuffer()
                                        {
                                            OneTimeSubmit = true,
                                            Name = $"Compute_{GraphicsDevice.CurrentFrameID}"
                                        };
                                        c_cmd.Build(AsyncComputeCmdPool[GraphicsDevice.CurrentFrameID]);
                                        c_cmd.BeginRecording();
                                    }
                                    break;
                                case CommandQueueKind.Graphics:
                                    {
                                        g_cmd.Barrier(resc.CurrentUsageStage, PipelineStage.Transfer, new BufferMemoryBarrier[] { bar }, null);
                                        if (renderPassBound)
                                        {
                                            g_cmd.EndRenderPass();
                                            renderPassBound = false;
                                        }
                                        g_cmd.EndRecording();
                                        if (l_graphCmds.Count == 0) g_waitingSems.Add(GraphicsDevice.ImageAvailableSemaphore[GraphicsDevice.PrevFrameID]);
                                        var c_comp_cmd = new CompiledCommandBuffer
                                        {
                                            CmdBuffer = g_cmd,
                                            SrcStage = resc.CurrentUsageStage,
                                            DstStage = PipelineStage.Transfer,
                                            signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                            waiting = g_waitingSems.ToArray()
                                        };
                                        l_graphCmds.Add(c_comp_cmd);
                                        activ_submissionOrder.AddLast(c_comp_cmd);
                                        g_waitingSems.Clear();
                                        waitingSems.Add(c_comp_cmd.signalling[0]);
                                        g_cmd = new CommandBuffer()
                                        {
                                            OneTimeSubmit = true,
                                            Name = $"Graphics_{GraphicsDevice.CurrentFrameID}"
                                        };
                                        g_cmd.Build(GraphicsCmdPool[GraphicsDevice.CurrentFrameID]);
                                        g_cmd.BeginRecording();
                                    }
                                    break;
                                case CommandQueueKind.Transfer:
                                    {
                                        t_cmd.Barrier(resc.CurrentUsageStage, PipelineStage.Transfer, new BufferMemoryBarrier[] { bar }, null);
                                        t_cmd.EndRecording();
                                        var c_comp_cmd = new CompiledCommandBuffer
                                        {
                                            CmdBuffer = t_cmd,
                                            SrcStage = resc.CurrentUsageStage,
                                            DstStage = PipelineStage.Transfer,
                                            signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                            waiting = t_waitingSems.ToArray()
                                        };
                                        l_transCmds.Add(c_comp_cmd);
                                        activ_submissionOrder.AddLast(c_comp_cmd);
                                        t_waitingSems.Clear();
                                        waitingSems.Add(c_comp_cmd.signalling[0]);
                                        t_cmd = new CommandBuffer()
                                        {
                                            OneTimeSubmit = true,
                                            Name = $"Transfer_{GraphicsDevice.CurrentFrameID}"
                                        };
                                        t_cmd.Build(TransferCmdPool[GraphicsDevice.CurrentFrameID]);
                                        t_cmd.BeginRecording();
                                    }
                                    break;
                            }
                        }
                        tgt_cmdbuf.Barrier(resc.CurrentUsageStage, PipelineStage.Transfer, new BufferMemoryBarrier[] { bar }, null);
                        resc.CurrentUsageStage = PipelineStage.Transfer;
                        resc.CurrentAccesses = AccessFlags.TransferWrite;
                        resc.OwningQueue = op.QueueKind;
                    }

                    if (GpuBuffers.ContainsKey(pass.Destination))
                    {
                        var resc = GpuBuffers[pass.Destination];
                        var bar = new BufferMemoryBarrier()
                        {
                            Accesses = resc.CurrentAccesses,
                            Buffer = resc,
                            Stores = AccessFlags.TransferWrite,
                            Offset = 0,
                            Size = resc.Size,
                            DstFamily = op.QueueKind,
                            SrcFamily = resc.OwningQueue == CommandQueueKind.Ignored ? op.QueueKind : resc.OwningQueue,
                        };
                        if (op.QueueKind != resc.OwningQueue && resc.OwningQueue != CommandQueueKind.Ignored)
                        {
                            //Split command buffer and setup semaphore
                            switch (resc.OwningQueue)
                            {
                                case CommandQueueKind.Compute:
                                    {
                                        c_cmd.Barrier(resc.CurrentUsageStage, PipelineStage.Transfer, new BufferMemoryBarrier[] { bar }, null);
                                        c_cmd.EndRecording();
                                        var c_comp_cmd = new CompiledCommandBuffer
                                        {
                                            CmdBuffer = c_cmd,
                                            SrcStage = resc.CurrentUsageStage,
                                            DstStage = PipelineStage.Transfer,
                                            signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                            waiting = c_waitingSems.ToArray()
                                        };
                                        l_acompCmds.Add(c_comp_cmd);
                                        activ_submissionOrder.AddLast(c_comp_cmd);
                                        c_waitingSems.Clear();
                                        waitingSems.Add(c_comp_cmd.signalling[0]);
                                        c_cmd = new CommandBuffer()
                                        {
                                            OneTimeSubmit = true,
                                            Name = $"Compute_{GraphicsDevice.CurrentFrameID}"
                                        };
                                        c_cmd.Build(AsyncComputeCmdPool[GraphicsDevice.CurrentFrameID]);
                                        c_cmd.BeginRecording();
                                    }
                                    break;
                                case CommandQueueKind.Graphics:
                                    {
                                        g_cmd.Barrier(resc.CurrentUsageStage, PipelineStage.Transfer, new BufferMemoryBarrier[] { bar }, null);
                                        if (renderPassBound)
                                        {
                                            g_cmd.EndRenderPass();
                                            renderPassBound = false;
                                        }
                                        g_cmd.EndRecording();
                                        if (l_graphCmds.Count == 0) g_waitingSems.Add(GraphicsDevice.ImageAvailableSemaphore[GraphicsDevice.PrevFrameID]);
                                        var c_comp_cmd = new CompiledCommandBuffer
                                        {
                                            CmdBuffer = g_cmd,
                                            SrcStage = resc.CurrentUsageStage,
                                            DstStage = PipelineStage.Transfer,
                                            signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                            waiting = g_waitingSems.ToArray()
                                        };
                                        l_graphCmds.Add(c_comp_cmd);
                                        activ_submissionOrder.AddLast(c_comp_cmd);
                                        g_waitingSems.Clear();
                                        waitingSems.Add(c_comp_cmd.signalling[0]);
                                        g_cmd = new CommandBuffer()
                                        {
                                            OneTimeSubmit = true,
                                            Name = $"Graphics_{GraphicsDevice.CurrentFrameID}"
                                        };
                                        g_cmd.Build(GraphicsCmdPool[GraphicsDevice.CurrentFrameID]);
                                        g_cmd.BeginRecording();
                                    }
                                    break;
                                case CommandQueueKind.Transfer:
                                    {
                                        t_cmd.Barrier(resc.CurrentUsageStage, PipelineStage.Transfer, new BufferMemoryBarrier[] { bar }, null);
                                        t_cmd.EndRecording();
                                        var c_comp_cmd = new CompiledCommandBuffer
                                        {
                                            CmdBuffer = t_cmd,
                                            SrcStage = resc.CurrentUsageStage,
                                            DstStage = PipelineStage.Transfer,
                                            signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                            waiting = t_waitingSems.ToArray()
                                        };
                                        l_transCmds.Add(c_comp_cmd);
                                        activ_submissionOrder.AddLast(c_comp_cmd);
                                        t_waitingSems.Clear();
                                        waitingSems.Add(c_comp_cmd.signalling[0]);
                                        t_cmd = new CommandBuffer()
                                        {
                                            OneTimeSubmit = true,
                                            Name = $"Transfer_{GraphicsDevice.CurrentFrameID}"
                                        };
                                        t_cmd.Build(TransferCmdPool[GraphicsDevice.CurrentFrameID]);
                                        t_cmd.BeginRecording();
                                    }
                                    break;
                            }
                        }
                        tgt_cmdbuf.Barrier(resc.CurrentUsageStage, PipelineStage.Transfer, new BufferMemoryBarrier[] { bar }, null);
                        resc.CurrentUsageStage = PipelineStage.Transfer;
                        resc.CurrentAccesses = AccessFlags.TransferWrite;
                        resc.OwningQueue = op.QueueKind;
                    }

                    switch (op.Cmd)
                    {
                        case GpuCmd.Stage:
                            tgt_cmdbuf.Stage(GpuBuffers[pass.Source], pass.SourceOffset, GpuBuffers[pass.Destination], pass.DestinationOffset, pass.Size);
                            break;
                        case GpuCmd.Download:
                            tgt_cmdbuf.Stage(GpuBuffers[pass.Destination], pass.DestinationOffset, GpuBuffers[pass.Source], pass.SourceOffset, pass.Size);
                            break;
                        default:
                            throw new ArgumentException("Transfer operation not specified!");
                    }
                }
                else if (ImageTransferPasses.ContainsKey(op.PassName))
                {
                    var pass = ImageTransferPasses[op.PassName];
                    if (GpuBuffers.ContainsKey(pass.Source))
                    {
                        var resc = GpuBuffers[pass.Source];
                        var bar = new BufferMemoryBarrier()
                        {
                            Accesses = resc.CurrentAccesses,
                            Buffer = resc,
                            Stores = AccessFlags.TransferWrite,
                            Offset = pass.SourceOffset,
                            Size = resc.Size - pass.SourceOffset,
                            DstFamily = op.QueueKind,
                            SrcFamily = resc.OwningQueue == CommandQueueKind.Ignored ? op.QueueKind : resc.OwningQueue,
                        };
                        if (op.QueueKind != resc.OwningQueue && resc.OwningQueue != CommandQueueKind.Ignored)
                        {
                            //Split command buffer and setup semaphore
                            switch (resc.OwningQueue)
                            {
                                case CommandQueueKind.Compute:
                                    {
                                        c_cmd.Barrier(resc.CurrentUsageStage, PipelineStage.Transfer, new BufferMemoryBarrier[] { bar }, null);
                                        c_cmd.EndRecording();
                                        var c_comp_cmd = new CompiledCommandBuffer
                                        {
                                            CmdBuffer = c_cmd,
                                            SrcStage = resc.CurrentUsageStage,
                                            DstStage = PipelineStage.Transfer,
                                            signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                            waiting = c_waitingSems.ToArray()
                                        };
                                        l_acompCmds.Add(c_comp_cmd);
                                        activ_submissionOrder.AddLast(c_comp_cmd);
                                        c_waitingSems.Clear();
                                        waitingSems.Add(c_comp_cmd.signalling[0]);
                                        c_cmd = new CommandBuffer()
                                        {
                                            OneTimeSubmit = true,
                                            Name = $"Compute_{GraphicsDevice.CurrentFrameID}"
                                        };
                                        c_cmd.Build(AsyncComputeCmdPool[GraphicsDevice.CurrentFrameID]);
                                        c_cmd.BeginRecording();
                                    }
                                    break;
                                case CommandQueueKind.Graphics:
                                    {
                                        g_cmd.Barrier(resc.CurrentUsageStage, PipelineStage.Transfer, new BufferMemoryBarrier[] { bar }, null);
                                        if (renderPassBound)
                                        {
                                            g_cmd.EndRenderPass();
                                            renderPassBound = false;
                                        }
                                        g_cmd.EndRecording();
                                        if (l_graphCmds.Count == 0) g_waitingSems.Add(GraphicsDevice.ImageAvailableSemaphore[GraphicsDevice.PrevFrameID]);
                                        var c_comp_cmd = new CompiledCommandBuffer
                                        {
                                            CmdBuffer = g_cmd,
                                            SrcStage = resc.CurrentUsageStage,
                                            DstStage = PipelineStage.Transfer,
                                            signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                            waiting = g_waitingSems.ToArray()
                                        };
                                        l_graphCmds.Add(c_comp_cmd);
                                        activ_submissionOrder.AddLast(c_comp_cmd);
                                        g_waitingSems.Clear();
                                        waitingSems.Add(c_comp_cmd.signalling[0]);
                                        g_cmd = new CommandBuffer()
                                        {
                                            OneTimeSubmit = true,
                                            Name = $"Graphics_{GraphicsDevice.CurrentFrameID}"
                                        };
                                        g_cmd.Build(GraphicsCmdPool[GraphicsDevice.CurrentFrameID]);
                                        g_cmd.BeginRecording();
                                    }
                                    break;
                                case CommandQueueKind.Transfer:
                                    {
                                        t_cmd.Barrier(resc.CurrentUsageStage, PipelineStage.Transfer, new BufferMemoryBarrier[] { bar }, null);
                                        t_cmd.EndRecording();
                                        var c_comp_cmd = new CompiledCommandBuffer
                                        {
                                            CmdBuffer = t_cmd,
                                            SrcStage = resc.CurrentUsageStage,
                                            DstStage = PipelineStage.Transfer,
                                            signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                            waiting = t_waitingSems.ToArray()
                                        };
                                        l_transCmds.Add(c_comp_cmd);
                                        activ_submissionOrder.AddLast(c_comp_cmd);
                                        t_waitingSems.Clear();
                                        waitingSems.Add(c_comp_cmd.signalling[0]);
                                        t_cmd = new CommandBuffer()
                                        {
                                            OneTimeSubmit = true,
                                            Name = $"Transfer_{GraphicsDevice.CurrentFrameID}"
                                        };
                                        t_cmd.Build(TransferCmdPool[GraphicsDevice.CurrentFrameID]);
                                        t_cmd.BeginRecording();
                                    }
                                    break;
                            }
                        }

                        tgt_cmdbuf.Barrier(resc.CurrentUsageStage, PipelineStage.Transfer, new BufferMemoryBarrier[] { bar }, null);
                        resc.CurrentUsageStage = PipelineStage.Transfer;
                        resc.CurrentAccesses = AccessFlags.TransferWrite;
                        resc.OwningQueue = op.QueueKind;
                    }

                    if (ImageViews.ContainsKey(pass.Destination))
                    {
                        var resc = ImageViews[pass.Destination];
                        var bar = new ImageMemoryBarrier()
                        {
                            Accesses = resc.CurrentAccesses,
                            Image = resc.parent,
                            Stores = AccessFlags.TransferWrite,
                            BaseArrayLayer = pass.BaseArrayLayer,
                            BaseMipLevel = pass.BaseMipLevel,
                            LayerCount = pass.LayerCount,
                            LevelCount = 1,
                            DstFamily = op.QueueKind,
                            SrcFamily = resc.OwningQueue == CommandQueueKind.Ignored ? op.QueueKind : resc.OwningQueue,
                            NewLayout = ImageLayout.TransferDstOptimal,
                            OldLayout = resc.CurrentLayout,
                        };
                        if (op.QueueKind != resc.OwningQueue && resc.OwningQueue != CommandQueueKind.Ignored)
                        {
                            //Split command buffer and setup semaphore
                            switch (resc.OwningQueue)
                            {
                                case CommandQueueKind.Compute:
                                    {
                                        c_cmd.Barrier(resc.CurrentUsageStage, PipelineStage.Transfer, null, new ImageMemoryBarrier[] { bar });
                                        c_cmd.EndRecording();
                                        var c_comp_cmd = new CompiledCommandBuffer
                                        {
                                            CmdBuffer = c_cmd,
                                            SrcStage = resc.CurrentUsageStage,
                                            DstStage = PipelineStage.Transfer,
                                            signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                            waiting = c_waitingSems.ToArray()
                                        };
                                        l_acompCmds.Add(c_comp_cmd);
                                        activ_submissionOrder.AddLast(c_comp_cmd);
                                        c_waitingSems.Clear();
                                        waitingSems.Add(c_comp_cmd.signalling[0]);
                                        c_cmd = new CommandBuffer()
                                        {
                                            OneTimeSubmit = true,
                                            Name = $"Compute_{GraphicsDevice.CurrentFrameID}"
                                        };
                                        c_cmd.Build(AsyncComputeCmdPool[GraphicsDevice.CurrentFrameID]);
                                        c_cmd.BeginRecording();
                                    }
                                    break;
                                case CommandQueueKind.Graphics:
                                    {
                                        g_cmd.Barrier(resc.CurrentUsageStage, PipelineStage.Transfer, null, new ImageMemoryBarrier[] { bar });
                                        if (renderPassBound)
                                        {
                                            g_cmd.EndRenderPass();
                                            renderPassBound = false;
                                        }
                                        g_cmd.EndRecording();
                                        if (l_graphCmds.Count == 0) g_waitingSems.Add(GraphicsDevice.ImageAvailableSemaphore[GraphicsDevice.PrevFrameID]);
                                        var c_comp_cmd = new CompiledCommandBuffer
                                        {
                                            CmdBuffer = g_cmd,
                                            SrcStage = resc.CurrentUsageStage,
                                            DstStage = PipelineStage.Transfer,
                                            signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                            waiting = g_waitingSems.ToArray()
                                        };
                                        l_graphCmds.Add(c_comp_cmd);
                                        activ_submissionOrder.AddLast(c_comp_cmd);
                                        g_waitingSems.Clear();
                                        waitingSems.Add(c_comp_cmd.signalling[0]);
                                        g_cmd = new CommandBuffer()
                                        {
                                            OneTimeSubmit = true,
                                            Name = $"Graphics_{GraphicsDevice.CurrentFrameID}"
                                        };
                                        g_cmd.Build(GraphicsCmdPool[GraphicsDevice.CurrentFrameID]);
                                        g_cmd.BeginRecording();
                                    }
                                    break;
                                case CommandQueueKind.Transfer:
                                    {
                                        t_cmd.Barrier(resc.CurrentUsageStage, PipelineStage.Transfer, null, new ImageMemoryBarrier[] { bar });
                                        t_cmd.EndRecording();
                                        var c_comp_cmd = new CompiledCommandBuffer
                                        {
                                            CmdBuffer = t_cmd,
                                            SrcStage = resc.CurrentUsageStage,
                                            DstStage = PipelineStage.Transfer,
                                            signalling = new GpuSemaphore[] { AllocateSemaphore(SemaphoreCounter.ToString()) },
                                            waiting = t_waitingSems.ToArray()
                                        };
                                        l_transCmds.Add(c_comp_cmd);
                                        activ_submissionOrder.AddLast(c_comp_cmd);
                                        t_waitingSems.Clear();
                                        waitingSems.Add(c_comp_cmd.signalling[0]);
                                        t_cmd = new CommandBuffer()
                                        {
                                            OneTimeSubmit = true,
                                            Name = $"Transfer_{GraphicsDevice.CurrentFrameID}"
                                        };
                                        t_cmd.Build(TransferCmdPool[GraphicsDevice.CurrentFrameID]);
                                        t_cmd.BeginRecording();
                                    }
                                    break;
                            }
                        }
                        tgt_cmdbuf.Barrier(resc.CurrentUsageStage, PipelineStage.Transfer, null, new ImageMemoryBarrier[] { bar });
                        resc.CurrentUsageStage = PipelineStage.Transfer;
                        resc.CurrentAccesses = AccessFlags.TransferWrite;
                        resc.CurrentLayout = ImageLayout.TransferDstOptimal;
                        resc.OwningQueue = op.QueueKind;
                    }

                    switch (op.Cmd)
                    {
                        case GpuCmd.Stage:
                            tgt_cmdbuf.Stage(GpuBuffers[pass.Source],
                                             pass.SourceOffset,
                                             ImageViews[pass.Destination].parent,
                                             pass.BaseMipLevel,
                                             pass.BaseArrayLayer,
                                             pass.LayerCount,
                                             pass.X,
                                             pass.Y,
                                             pass.Z,
                                             pass.Width,
                                             pass.Height,
                                             pass.Depth);
                            break;
                        case GpuCmd.Download:
                            tgt_cmdbuf.Download(ImageViews[pass.Destination].parent,
                                                pass.BaseMipLevel,
                                                pass.BaseArrayLayer,
                                                pass.LayerCount,
                                                pass.X,
                                                pass.Y,
                                                pass.Z,
                                                pass.Width,
                                                pass.Height,
                                                pass.Depth,
                                                GpuBuffers[pass.Source],
                                                pass.SourceOffset);
                            break;
                        default:
                            throw new ArgumentException("Transfer operation not specified!");
                    }
                }

                node = node.Next;
            }
            while (node != null);
            if (t_cmd.IsRecording)
            {
                t_cmd.EndRecording();
                var c_comp_cmd = new CompiledCommandBuffer()
                {
                    CmdBuffer = t_cmd,
                    waiting = t_waitingSems.ToArray(),
                    SrcStage = PipelineStage.Bottom,
                    DstStage = PipelineStage.Top,
                };
                l_transCmds.Add(c_comp_cmd);
                activ_submissionOrder.AddLast(c_comp_cmd);
            }
            if (c_cmd.IsRecording)
            {
                c_cmd.EndRecording();
                var c_comp_cmd = new CompiledCommandBuffer()
                {
                    CmdBuffer = c_cmd,
                    waiting = c_waitingSems.ToArray(),
                    SrcStage = PipelineStage.Bottom,
                    DstStage = PipelineStage.Top,
                };
                l_acompCmds.Add(c_comp_cmd);
                activ_submissionOrder.AddLast(c_comp_cmd);
            }
            if (g_cmd.IsRecording)
            {
                if (renderPassBound) g_cmd.EndRenderPass();
                g_cmd.EndRecording();
                if (l_graphCmds.Count == 0) g_waitingSems.Add(GraphicsDevice.ImageAvailableSemaphore[GraphicsDevice.PrevFrameID]);

                var c_comp_cmd = new CompiledCommandBuffer()
                {
                    CmdBuffer = g_cmd,
                    waiting = g_waitingSems.ToArray(),
                    SrcStage = PipelineStage.Bottom,
                    DstStage = PipelineStage.Top,
                };
                l_graphCmds.Add(c_comp_cmd);
                activ_submissionOrder.AddLast(c_comp_cmd);
            }

            finalGfxSem = AllocateSemaphore("FinalGFX");

            if (graphCmds[GraphicsDevice.CurrentFrameID].Length != 0)
            {
                for (int i = 0; i < graphCmds[GraphicsDevice.CurrentFrameID].Length; i++)
                    graphCmds[GraphicsDevice.CurrentFrameID][i].CmdBuffer.Dispose();
                if (transitionBuffer[GraphicsDevice.CurrentFrameID] != null) transitionBuffer[GraphicsDevice.CurrentFrameID].Dispose();
            }

            compFences[GraphicsDevice.CurrentFrameID].Wait();
            compFences[GraphicsDevice.CurrentFrameID].Reset();

            transFences[GraphicsDevice.CurrentFrameID].Wait();
            transFences[GraphicsDevice.CurrentFrameID].Reset();

            if (compCmds[GraphicsDevice.CurrentFrameID].Length != 0)
            {
                for (int i = 0; i < compCmds[GraphicsDevice.CurrentFrameID].Length; i++)
                    compCmds[GraphicsDevice.CurrentFrameID][i].CmdBuffer.Dispose();
            }
            if (transCmds[GraphicsDevice.CurrentFrameID].Length != 0)
            {
                for (int i = 0; i < transCmds[GraphicsDevice.CurrentFrameID].Length; i++)
                    transCmds[GraphicsDevice.CurrentFrameID][i].CmdBuffer.Dispose();
            }

            //Process state into semaphores and command buffers
            graphCmds[GraphicsDevice.CurrentFrameID] = l_graphCmds.ToArray();
            compCmds[GraphicsDevice.CurrentFrameID] = l_acompCmds.ToArray();
            transCmds[GraphicsDevice.CurrentFrameID] = l_transCmds.ToArray();

            for (int i = 0; i < l_graphCmds.Count; i++)
                if (!l_graphCmds[i].CmdBuffer.IsEmpty)
                {
                    activ_graphCmds.Add(l_graphCmds[i]);
                }

            for (int i = 0; i < l_acompCmds.Count; i++)
                if (!l_acompCmds[i].CmdBuffer.IsEmpty)
                {
                    activ_acompCmds.Add(l_acompCmds[i]);
                }

            for (int i = 0; i < l_transCmds.Count; i++)
                if (!l_transCmds[i].CmdBuffer.IsEmpty)
                {
                    activ_transCmds.Add(l_transCmds[i]);
                }

            int maxlen = System.Math.Max(activ_graphCmds.Count - 1, System.Math.Max(activ_acompCmds.Count - 1, activ_transCmds.Count - 1));
            var cur_submission_cmd = activ_submissionOrder.First;
            while (cur_submission_cmd.Value.CmdBuffer.IsEmpty)
                cur_submission_cmd = cur_submission_cmd.Next;

            int g_i = 0, c_i = 0, t_i = 0;
            do
            {
                bool processed = false;
                if (!processed && g_i < activ_graphCmds.Count && cur_submission_cmd.Value == activ_graphCmds[g_i])
                {
                    if (g_i < activ_graphCmds.Count - 1)
                        GraphicsDevice.SubmitCommandBuffer(activ_graphCmds[g_i].CmdBuffer, activ_graphCmds[g_i].waiting, activ_graphCmds[g_i].signalling, null);
                    g_i++;
                    processed = true;
                }

                if (!processed && c_i < activ_acompCmds.Count && cur_submission_cmd.Value == activ_acompCmds[c_i])
                {
                    if (c_i < activ_acompCmds.Count - 1)
                        GraphicsDevice.SubmitCommandBuffer(activ_acompCmds[c_i].CmdBuffer, activ_acompCmds[c_i].waiting, activ_acompCmds[c_i].signalling, null);
                    c_i++;
                    processed = true;
                }

                if (!processed && t_i < activ_transCmds.Count && cur_submission_cmd.Value == activ_transCmds[t_i])
                {
                    if (t_i < activ_transCmds.Count - 1)
                        GraphicsDevice.SubmitCommandBuffer(activ_transCmds[t_i].CmdBuffer, activ_transCmds[t_i].waiting, activ_transCmds[t_i].signalling, null);
                    t_i++;
                    processed = true;
                }

                if (processed)
                    do
                    {
                        cur_submission_cmd = cur_submission_cmd.Next;
                    }
                    while (cur_submission_cmd != null && cur_submission_cmd.Value.CmdBuffer.IsEmpty);
            } while (cur_submission_cmd != null);

            transitionBuffer[GraphicsDevice.CurrentFrameID] = new CommandBuffer
            {
                Name = "transitionBuffer",
                OneTimeSubmit = true
            };
            transitionBuffer[GraphicsDevice.CurrentFrameID].Build(GraphicsCmdPool[GraphicsDevice.CurrentFrameID]);
            transitionBuffer[GraphicsDevice.CurrentFrameID].BeginRecording();
            transitionBuffer[GraphicsDevice.CurrentFrameID].Barrier(PipelineStage.ColorAttachOut, PipelineStage.ColorAttachOut, null, new ImageMemoryBarrier[]
            {
                new ImageMemoryBarrier()
                {
                    Accesses = AccessFlags.ColorAttachmentWrite,
                    Stores = AccessFlags.ColorAttachmentWrite,
                    BaseArrayLayer = 0,
                    BaseMipLevel = 0,
                    DstFamily = CommandQueueKind.Ignored,
                    SrcFamily = CommandQueueKind.Ignored,
                    Image = GraphicsDevice.DefaultFramebuffer[GraphicsDevice.CurrentFrameID].ColorAttachments[0].parent,
                    LayerCount = 1,
                    LevelCount = 1,
                    NewLayout = ImageLayout.PresentSrc,
                    OldLayout = ImageLayout.ColorAttachmentOptimal,
                }
            });
            transitionBuffer[GraphicsDevice.CurrentFrameID].EndRecording();
            GraphicsDevice.DefaultFramebuffer[GraphicsDevice.CurrentFrameID].ColorAttachments[0].CurrentLayout = ImageLayout.PresentSrc;
            GraphicsDevice.DefaultFramebuffer[GraphicsDevice.CurrentFrameID].ColorAttachments[0].CurrentAccesses = AccessFlags.ColorAttachmentWrite;
            GraphicsDevice.DefaultFramebuffer[GraphicsDevice.CurrentFrameID].ColorAttachments[0].CurrentUsageStage = PipelineStage.ColorAttachOut;

            {
                int i = activ_acompCmds.Count - 1;
                if (i >= 0)
                    GraphicsDevice.SubmitCommandBuffer(activ_acompCmds[i].CmdBuffer, activ_acompCmds[i].waiting, activ_acompCmds[i].signalling, compFences[GraphicsDevice.CurrentFrameID]);
                else
                {
                    compFences[GraphicsDevice.CurrentFrameID].Dispose();
                    compFences[GraphicsDevice.CurrentFrameID] = new Fence()
                    {
                        Name = compFences[GraphicsDevice.CurrentFrameID].Name,
                        CreateSignaled = true,
                    };
                    compFences[GraphicsDevice.CurrentFrameID].Build(DeviceIndex);
                }

                i = activ_transCmds.Count - 1;
                if (i >= 0)
                    GraphicsDevice.SubmitCommandBuffer(activ_transCmds[i].CmdBuffer, activ_transCmds[i].waiting, activ_transCmds[i].signalling, transFences[GraphicsDevice.CurrentFrameID]);
                else
                {
                    transFences[GraphicsDevice.CurrentFrameID].Dispose();
                    transFences[GraphicsDevice.CurrentFrameID] = new Fence()
                    {
                        Name = transFences[GraphicsDevice.CurrentFrameID].Name,
                        CreateSignaled = true,
                    };
                    transFences[GraphicsDevice.CurrentFrameID].Build(DeviceIndex);
                }

                i = activ_graphCmds.Count - 1;
                GraphicsDevice.SubmitCommandBuffer(activ_graphCmds[i].CmdBuffer, activ_graphCmds[i].waiting, new GpuSemaphore[] { finalGfxSem }, null);
            }

            //Submit the last graphics command with an additional sync + fence for the frame
            GraphicsDevice.SubmitCommandBuffer(transitionBuffer[GraphicsDevice.CurrentFrameID], new GpuSemaphore[] { finalGfxSem }, new GpuSemaphore[] { GraphicsDevice.FrameFinishedSemaphore[GraphicsDevice.CurrentFrameID] }, GraphicsDevice.InflightFences[GraphicsDevice.CurrentFrameID]);

            Console.WriteLine("NEXT FRAME");
            buildLock.Release();
        }
    }
}
