using Kokoro.Graphics;
using Kokoro.Math.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

namespace Kokoro.Graphics.Framegraph
{

    public struct GpuResourceRequest
    {
        //Resource Requests
        public string Name { get; set; }

        internal PipelineStage transitionSourceStage;
        internal PipelineStage transitionDestStage;
        internal ImageLayout transitionSourceLayout;
        internal ImageLayout transitionDestLayout;
        internal AccessFlags barrierSourceAccess;
        internal AccessFlags barrierDestAccess;
    }

    public enum GpuCmd
    {
        None,
        Draw,
        DrawIndexed,
    }

    public class GpuOp
    {
        //Pass Name
        public string PassName { get; set; }
        public GpuResourceRequest[] Resources { get; set; }

        //Only used to build the framebuffer, resource info must still be specified in Resources
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

        internal CommandQueueKind QueueKind;
        internal bool barrierOp;
        internal bool transitionOp;
        internal bool layoutOp;
        internal CommandQueueKind transitionDestQueueKind;
        internal int ownerChangeSemaphoreIdx;

        public GpuOp() { }
        public GpuOp(GpuOp src)
        {
            Cmd = src.Cmd;
            PassName = src.PassName;
            Resources = src.Resources;
            ColorAttachments = src.ColorAttachments;
            DepthAttachment = src.DepthAttachment;
            QueueKind = src.QueueKind;
            barrierOp = src.barrierOp;
            transitionOp = src.transitionOp;
            layoutOp = src.layoutOp;
            transitionDestQueueKind = src.transitionDestQueueKind;
            ownerChangeSemaphoreIdx = src.ownerChangeSemaphoreIdx;
        }
    }

    public class FrameGraph
    {
        int DeviceIndex;
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

        CommandBuffer[] transitionBuffer;

        class ImageViewState
        {
            public ImageView view;
            public ImageLayout layout;
            public CommandQueueKind ownerQueue;
            public PipelineStage lastStoreStage;
            public AccessFlags lastStoreAccess;
            public GpuOp lastStoreOp;
        }

        class BufferState
        {
            public GpuBuffer buffer;
            public CommandQueueKind ownerQueue;
            public PipelineStage lastStoreStage;
            public AccessFlags lastStoreAccess;
            public GpuOp lastStoreOp;
        }

        class BufferViewState
        {
            public GpuBufferView bufferView;
            public CommandQueueKind ownerQueue;
            public PipelineStage lastStoreStage;
            public AccessFlags lastStoreAccess;
            public GpuOp lastStoreOp;
        }

        class SemaphoreState
        {
            public GpuOp signaler;
            public GpuOp waiter;
            public GpuSemaphore semaphore;
        }

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
            transitionBuffer = new CommandBuffer[GraphicsDevice.MaxFramesInFlight];
            for (int i = 0; i < GraphicsDevice.MaxFramesInFlight; i++)
            {
                graphCmds[i] = new CompiledCommandBuffer[0];
                compCmds[i] = new CompiledCommandBuffer[0];
                transCmds[i] = new CompiledCommandBuffer[0];

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

        private CompiledCommandBuffer[] GenerateCommands(LinkedList<GpuOp> ops, CommandPool cmdPool, List<SemaphoreState> semaphores)
        {
            if (ops.Count == 0)
                return new CompiledCommandBuffer[0];

            //TODO: Reorder barriers here to execute as early as possible

            //Figure out where command buffers need to be split and synchronized with semaphores
            var cmdBufs = new List<CompiledCommandBuffer>();
            var signallingSemaphores = new List<GpuSemaphore>();
            var waitingSemaphores = new List<GpuSemaphore>();
            var cmdBuf = new CommandBuffer()
            {
                Name = cmdPool.Name + "_" + cmdBufs.Count.ToString(),
                OneTimeSubmit = true,
            };
            cmdBuf.Build(cmdPool);
            cmdBuf.BeginRecording();
            bool renderPassBound = false;
            while (ops.Count > 0)
            {
                var op = ops.First.Value;
                ops.RemoveFirst();

                if (op.barrierOp | op.layoutOp | op.transitionOp)
                {
                    var imgBarrier = new ImageMemoryBarrier()
                    {
                        Accesses = op.Resources[0].barrierSourceAccess,
                        Stores = op.Resources[0].barrierDestAccess,
                        DstFamily = op.transitionOp ? op.transitionDestQueueKind : CommandQueueKind.Ignored,
                        SrcFamily = op.transitionOp ? op.QueueKind : CommandQueueKind.Ignored,
                        OldLayout = op.Resources[0].transitionSourceLayout,
                        NewLayout = op.Resources[0].transitionDestLayout,
                    };

                    var bufBarrier = new BufferMemoryBarrier()
                    {
                        Accesses = op.Resources[0].barrierSourceAccess,
                        Stores = op.Resources[0].barrierDestAccess,
                        DstFamily = op.transitionOp ? op.transitionDestQueueKind : CommandQueueKind.Ignored,
                        SrcFamily = op.transitionOp ? op.QueueKind : CommandQueueKind.Ignored,
                    };

                    bool addImgBarrier = false;
                    if (ImageViews.ContainsKey(op.Resources[0].Name))
                    {
                        var imgView = ImageViews[op.Resources[0].Name];
                        imgBarrier.Image = imgView.parent;
                        imgBarrier.BaseArrayLayer = imgView.BaseLayer;
                        imgBarrier.BaseMipLevel = imgView.BaseLevel;
                        imgBarrier.LayerCount = imgView.LayerCount;
                        imgBarrier.LevelCount = imgView.LevelCount;

                        addImgBarrier = true;
                    }

                    bool addBufBarrier = false;
                    if (GpuBufferViews.ContainsKey(op.Resources[0].Name))
                    {
                        var bufView = GpuBufferViews[op.Resources[0].Name];
                        bufBarrier.Buffer = bufView.parent;
                        bufBarrier.Offset = 0;
                        bufBarrier.Size = bufView.Size;
                        addBufBarrier = true;
                    }
                    else if (GpuBuffers.ContainsKey(op.Resources[0].Name))
                    {
                        var buf = GpuBuffers[op.Resources[0].Name];
                        bufBarrier.Buffer = buf;
                        bufBarrier.Offset = 0;
                        bufBarrier.Size = buf.Size;
                        addBufBarrier = true;
                    }

                    //Add signalled semaphore
                    bool transitionOutOp = false;
                    if (op.transitionOp)
                    {
                        var semaphoreData = semaphores[op.ownerChangeSemaphoreIdx];
                        //if this is the source queue, signal the semaphore
                        if (semaphoreData.signaler == op)
                        {
                            signallingSemaphores.Add(semaphoreData.semaphore);
                            transitionOutOp = true;
                        }

                        //if this is the dest queue, wait for the semaphore
                        if (semaphoreData.waiter == op)
                            waitingSemaphores.Add(semaphoreData.semaphore);
                    }

                    //Emit barrier
                    if (addImgBarrier)
                        cmdBuf.Barrier(op.Resources[0].transitionSourceStage, op.Resources[0].transitionDestStage, null, new ImageMemoryBarrier[] { imgBarrier });
                    else if (addBufBarrier)
                        cmdBuf.Barrier(op.Resources[0].transitionSourceStage, op.Resources[0].transitionDestStage, new BufferMemoryBarrier[] { bufBarrier }, null);

                    if (op.transitionOp && transitionOutOp)
                    {
                        //Emit command buffer
                        if (renderPassBound) cmdBuf.EndRenderPass();
                        cmdBuf.EndRecording();
                        cmdBufs.Add(new CompiledCommandBuffer()
                        {
                            CmdBuffer = cmdBuf,
                            signalling = signallingSemaphores.ToArray(),
                            waiting = waitingSemaphores.ToArray(),
                            SrcStage = op.Resources[0].transitionSourceStage,
                            DstStage = op.Resources[0].transitionDestStage,
                        });
                        signallingSemaphores.Clear();
                        waitingSemaphores.Clear();
                        renderPassBound = false;

                        cmdBuf = new CommandBuffer()
                        {
                            Name = cmdPool.Name + "_" + cmdBufs.Count.ToString(),
                            OneTimeSubmit = true,
                        };
                        cmdBuf.Build(cmdPool);
                        cmdBuf.BeginRecording();
                    }
                    continue;
                }

                if (BufferTransferPasses.ContainsKey(op.PassName))
                {
                    var pass = BufferTransferPasses[op.PassName];
                    cmdBuf.Stage(GpuBuffers[pass.Source], pass.SourceOffset, GpuBuffers[pass.Destination], pass.DestinationOffset, pass.Size);
                }
                else if (ImageTransferPasses.ContainsKey(op.PassName))
                {

                }
                else if (GraphicsPasses.ContainsKey(op.PassName))
                {
                    switch (op.Cmd)
                    {
                        case GpuCmd.Draw:
                        case GpuCmd.DrawIndexed:
                            {
                                if (renderPassBound) cmdBuf.EndRenderPass();
                                renderPassBound = false;
                                //Bind the graphics pipeline, framebuffer, descriptors
                                cmdBuf.SetDescriptors(PipelineLayouts[op.PassName], globalDescriptorSet, DescriptorBindPoint.Graphics, 0);
                                cmdBuf.SetPipeline(GraphicsPipelines[op.PassName], Framebuffers[GetFramebufferName(op.ColorAttachments, op.DepthAttachment)], 0);
                                renderPassBound = true;
                            }
                            break;
                    }

                    switch (op.Cmd)
                    {
                        case GpuCmd.Draw:
                            {
                                cmdBuf.Draw(op.VertexCount, op.InstanceCount, op.BaseVertex, op.BaseInstance);
                            }
                            break;
                        case GpuCmd.DrawIndexed:
                            {
                                cmdBuf.DrawIndexed(GpuBuffers[op.IndexBuffer], op.IndexBufferOffset, op.IndexType, op.IndexCount, op.InstanceCount, op.FirstIndex, (int)op.BaseVertex, op.BaseInstance);
                            }
                            break;
                    }
                }
            }
            if (cmdBuf.IsRecording)
            {
                if (renderPassBound) cmdBuf.EndRenderPass();
                cmdBuf.EndRecording();
                cmdBufs.Add(new CompiledCommandBuffer()
                {
                    CmdBuffer = cmdBuf,
                    signalling = signallingSemaphores.ToArray(),
                    waiting = waitingSemaphores.ToArray(),
                });
                signallingSemaphores.Clear();
                waitingSemaphores.Clear();
                renderPassBound = false;
            }

            return cmdBufs.ToArray();
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

        public void GatherDescriptors()
        {
            buildLock.Wait();

            globalDescriptorPool = new DescriptorPool();
            globalDescriptorPool.Name = "GlobalDescriptorPool";

            globalDescriptorLayout = new DescriptorLayout();
            globalDescriptorLayout.Name = "GlobalDescriptorLayout";

            globalDescriptorPool.Add(DescriptorType.UniformBuffer, 1);
            globalDescriptorLayout.Add(0, DescriptorType.UniformBuffer, 1, ShaderType.All);

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

            int semaphoreCntr = 0;
            GpuSemaphore finalGfxSem = null;
            var graphQ0 = new Queue<GpuOp>(Ops.Count);
            var compQ0 = new Queue<GpuOp>(Ops.Count);
            var transQ0 = new Queue<GpuOp>(Ops.Count);
            var graphQ1 = new LinkedList<GpuOp>();
            var compQ1 = new LinkedList<GpuOp>();
            var transQ1 = new LinkedList<GpuOp>();
            var semaphoreSet = new List<SemaphoreState>(256);
            var imgViews = new Dictionary<string, ImageViewState>(ImageViews.Count);
            var buffers = new Dictionary<string, BufferState>(GpuBuffers.Count);
            var bufferViews = new Dictionary<string, BufferViewState>(GpuBufferViews.Count);

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
                        int k;
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

                    for (int j = 0; j < opSet[i].Resources.Length; j++)
                    {
                        var resourceName = opSet[i].Resources[j].Name;
                        if (ImageViews.ContainsKey(resourceName) && !imgViews.ContainsKey(resourceName))
                        {
                            //Collect all imageview resource names
                            imgViews[resourceName] = new ImageViewState()
                            {
                                view = ImageViews[resourceName],
                                layout = ImageViews[resourceName].Layout,
                                ownerQueue = opSet[i].QueueKind,
                            };
                            if (opSet[i].QueueKind == CommandQueueKind.Transfer)
                            {
                                imgViews[resourceName].lastStoreStage = PipelineStage.Transfer;
                                imgViews[resourceName].lastStoreOp = opSet[i];
                            }
                            else if (opSet[i].QueueKind == CommandQueueKind.Compute)
                            {
                                imgViews[resourceName].lastStoreStage = PipelineStage.CompShader;
                                if (opSet[i].Resources[j].LastStoreStage != PipelineStage.Top)
                                    imgViews[resourceName].lastStoreOp = opSet[i];
                            }
                            else
                            {
                                imgViews[resourceName].lastStoreStage = PipelineStage.Bottom;
                                if (opSet[i].Resources[j].LastStoreStage != PipelineStage.Top)
                                    imgViews[resourceName].lastStoreOp = opSet[i];
                            }
                            //Check the desired layout, emit a transition if needed
                            if (ImageViews[resourceName].Layout != opSet[i].Resources[j].DesiredLayout)
                            {
                                var tr_op = new GpuOp();
                                tr_op.Resources = new GpuResourceRequest[]
                                {
                                    new GpuResourceRequest()
                                    {
                                        Name = resourceName
                                    }
                                };

                                tr_op.barrierOp = true;
                                tr_op.Resources[0].transitionSourceStage = imgViews[resourceName].lastStoreStage;
                                tr_op.Resources[0].transitionDestStage = opSet[i].Resources[j].FirstLoadStage;

                                //Prepare a memory barrier for the last store ops to be ready for the latest load ops 
                                tr_op.Resources[0].barrierSourceAccess = imgViews[resourceName].lastStoreAccess;
                                tr_op.Resources[0].barrierDestAccess = opSet[i].Resources[j].Accesses;

                                //Generate a layout transition
                                tr_op.layoutOp = true;
                                tr_op.Resources[0].transitionSourceLayout = imgViews[resourceName].layout;
                                tr_op.Resources[0].transitionDestLayout = opSet[i].Resources[j].DesiredLayout;
                                switch (imgViews[resourceName].ownerQueue)
                                {
                                    case CommandQueueKind.Compute:
                                        compQ0.Enqueue(tr_op);
                                        break;
                                    case CommandQueueKind.Graphics:
                                        graphQ0.Enqueue(tr_op);
                                        break;
                                    case CommandQueueKind.Transfer:
                                        transQ0.Enqueue(tr_op);
                                        break;
                                    default:
                                        throw new Exception();
                                };
                            }
                        }
                        else if (GpuBuffers.ContainsKey(resourceName) && !buffers.ContainsKey(resourceName))
                        {
                            //Collect all buffer resource names
                            buffers[resourceName] = new BufferState()
                            {
                                ownerQueue = opSet[i].QueueKind,
                                buffer = GpuBuffers[resourceName],
                            };
                            if (opSet[i].QueueKind == CommandQueueKind.Transfer)
                            {
                                buffers[resourceName].lastStoreStage = PipelineStage.Transfer;
                                buffers[resourceName].lastStoreOp = opSet[i];
                            }
                            else if (opSet[i].QueueKind == CommandQueueKind.Compute)
                            {
                                buffers[resourceName].lastStoreStage = PipelineStage.CompShader;
                                if (opSet[i].Resources[j].LastStoreStage != PipelineStage.Top)
                                    buffers[resourceName].lastStoreOp = opSet[i];
                            }
                            else
                            {
                                buffers[resourceName].lastStoreStage = PipelineStage.Bottom;
                                if (opSet[i].Resources[j].LastStoreStage != PipelineStage.Top)
                                    buffers[resourceName].lastStoreOp = opSet[i];
                            }
                        }
                        else if (GpuBufferViews.ContainsKey(resourceName) && !bufferViews.ContainsKey(resourceName))
                        {
                            //Collect all buffer view resource names
                            bufferViews[resourceName] = new BufferViewState()
                            {
                                ownerQueue = opSet[i].QueueKind,
                                bufferView = GpuBufferViews[resourceName]
                            };
                            if (opSet[i].QueueKind == CommandQueueKind.Transfer)
                            {
                                bufferViews[resourceName].lastStoreStage = PipelineStage.Transfer;
                                bufferViews[resourceName].lastStoreOp = opSet[i];
                            }
                            else if (opSet[i].QueueKind == CommandQueueKind.Compute)
                            {
                                bufferViews[resourceName].lastStoreStage = PipelineStage.CompShader;
                                if (opSet[i].Resources[j].LastStoreStage != PipelineStage.Top)
                                    bufferViews[resourceName].lastStoreOp = opSet[i];
                            }
                            else
                            {
                                bufferViews[resourceName].lastStoreStage = PipelineStage.Bottom;
                                if (opSet[i].Resources[j].LastStoreStage != PipelineStage.Top)
                                    bufferViews[resourceName].lastStoreOp = opSet[i];
                            }
                        }
                        //Detect and insert queue transition info + propogate state transitions
                        else if (ImageViews.ContainsKey(resourceName))
                        {
                            var node = imgViews[resourceName];
                            if (node.lastStoreOp != null)
                            {
                                var tr_op = new GpuOp();
                                tr_op.Resources = new GpuResourceRequest[]
                                {
                                        new GpuResourceRequest()
                                        {
                                            Name = resourceName,
                                        }
                                };

                                //propogate any necessary layout transitions and barriers
                                tr_op.barrierOp = true;
                                tr_op.Resources[0].transitionSourceStage = node.lastStoreStage;
                                tr_op.Resources[0].transitionDestStage = opSet[i].Resources[j].FirstLoadStage;

                                //Prepare a memory barrier for the last store ops to be ready for the latest load ops 
                                tr_op.Resources[0].barrierSourceAccess = node.lastStoreAccess;
                                tr_op.Resources[0].barrierDestAccess = opSet[i].Resources[j].Accesses;

                                if (node.layout != opSet[i].Resources[j].DesiredLayout)
                                {
                                    //Generate a layout transition
                                    tr_op.layoutOp = true;
                                    tr_op.Resources[0].transitionSourceLayout = node.layout;
                                    tr_op.Resources[0].transitionDestLayout = opSet[i].Resources[j].DesiredLayout;
                                }

                                if (node.ownerQueue != opSet[i].QueueKind)
                                {
                                    //Insert a queue transition op
                                    tr_op.transitionOp = true;
                                    tr_op.transitionDestQueueKind = opSet[i].QueueKind;
                                    tr_op.QueueKind = node.ownerQueue;
                                    tr_op.ownerChangeSemaphoreIdx = semaphoreSet.Count;
                                    //Emit this into both queues
                                    switch (node.ownerQueue)
                                    {
                                        case CommandQueueKind.Compute:
                                            compQ0.Enqueue(tr_op);
                                            break;
                                        case CommandQueueKind.Graphics:
                                            graphQ0.Enqueue(tr_op);
                                            break;
                                        case CommandQueueKind.Transfer:
                                            transQ0.Enqueue(tr_op);
                                            break;
                                        default:
                                            throw new Exception();
                                    };
                                    var waiter_op = new GpuOp(tr_op);
                                    waiter_op.ownerChangeSemaphoreIdx = semaphoreSet.Count;
                                    semaphoreSet.Add(new SemaphoreState()
                                    {
                                        signaler = tr_op,
                                        waiter = waiter_op
                                    });
                                    switch (opSet[i].QueueKind)
                                    {
                                        case CommandQueueKind.Compute:
                                            compQ0.Enqueue(waiter_op);
                                            break;
                                        case CommandQueueKind.Graphics:
                                            graphQ0.Enqueue(waiter_op);
                                            break;
                                        case CommandQueueKind.Transfer:
                                            transQ0.Enqueue(waiter_op);
                                            break;
                                        default:
                                            throw new Exception();
                                    };
                                }
                                else
                                {
                                    switch (opSet[i].QueueKind)
                                    {
                                        case CommandQueueKind.Compute:
                                            compQ0.Enqueue(tr_op);
                                            break;
                                        case CommandQueueKind.Graphics:
                                            graphQ0.Enqueue(tr_op);
                                            break;
                                        case CommandQueueKind.Transfer:
                                            transQ0.Enqueue(tr_op);
                                            break;
                                        default:
                                            throw new Exception();
                                    };
                                }

                                //update the imgViews state
                                imgViews[resourceName].lastStoreOp = opSet[i];
                                imgViews[resourceName].lastStoreStage = opSet[i].Resources[j].LastStoreStage;
                                imgViews[resourceName].lastStoreAccess = opSet[i].Resources[j].Stores;
                                imgViews[resourceName].layout = opSet[i].Resources[j].DesiredLayout;
                                imgViews[resourceName].ownerQueue = opSet[i].QueueKind;
                            }
                            //If no stores have been performed, there is nothing to transition or barrier
                        }
                        else if (GpuBuffers.ContainsKey(resourceName))
                        {
                            var node = buffers[resourceName];
                            if (node.lastStoreOp != null)
                            {
                                var tr_op = new GpuOp();
                                tr_op.Resources = new GpuResourceRequest[]
                                {
                                        new GpuResourceRequest()
                                        {
                                            Name = resourceName,
                                        }
                                };

                                //propogate any necessary layout transitions and barriers
                                tr_op.barrierOp = true;
                                tr_op.Resources[0].transitionSourceStage = node.lastStoreStage;
                                tr_op.Resources[0].transitionDestStage = opSet[i].Resources[j].FirstLoadStage;

                                //Prepare a memory barrier for the last store ops to be ready for the latest load ops 
                                tr_op.Resources[0].barrierSourceAccess = node.lastStoreAccess;
                                tr_op.Resources[0].barrierDestAccess = opSet[i].Resources[j].Accesses;

                                if (node.ownerQueue != opSet[i].QueueKind)
                                {
                                    //Insert a queue transition op
                                    tr_op.transitionOp = true;
                                    tr_op.transitionDestQueueKind = opSet[i].QueueKind;
                                    tr_op.QueueKind = node.ownerQueue;
                                    tr_op.ownerChangeSemaphoreIdx = semaphoreSet.Count;
                                    //Emit this into both queues
                                    switch (node.ownerQueue)
                                    {
                                        case CommandQueueKind.Compute:
                                            compQ0.Enqueue(tr_op);
                                            break;
                                        case CommandQueueKind.Graphics:
                                            graphQ0.Enqueue(tr_op);
                                            break;
                                        case CommandQueueKind.Transfer:
                                            transQ0.Enqueue(tr_op);
                                            break;
                                        default:
                                            throw new Exception();
                                    };
                                    var waiter_op = new GpuOp(tr_op);
                                    waiter_op.ownerChangeSemaphoreIdx = semaphoreSet.Count;
                                    semaphoreSet.Add(new SemaphoreState()
                                    {
                                        signaler = tr_op,
                                        waiter = waiter_op
                                    });
                                    switch (opSet[i].QueueKind)
                                    {
                                        case CommandQueueKind.Compute:
                                            compQ0.Enqueue(waiter_op);
                                            break;
                                        case CommandQueueKind.Graphics:
                                            graphQ0.Enqueue(waiter_op);
                                            break;
                                        case CommandQueueKind.Transfer:
                                            transQ0.Enqueue(waiter_op);
                                            break;
                                        default:
                                            throw new Exception();
                                    };
                                }
                                else
                                {
                                    switch (opSet[i].QueueKind)
                                    {
                                        case CommandQueueKind.Compute:
                                            compQ0.Enqueue(tr_op);
                                            break;
                                        case CommandQueueKind.Graphics:
                                            graphQ0.Enqueue(tr_op);
                                            break;
                                        case CommandQueueKind.Transfer:
                                            transQ0.Enqueue(tr_op);
                                            break;
                                        default:
                                            throw new Exception();
                                    };
                                }

                                //update the buffers state
                                buffers[resourceName].lastStoreOp = opSet[i];
                                buffers[resourceName].lastStoreStage = opSet[i].Resources[j].LastStoreStage;
                                buffers[resourceName].lastStoreAccess = opSet[i].Resources[j].Stores;
                                buffers[resourceName].ownerQueue = opSet[i].QueueKind;
                            }
                        }
                        else if (GpuBufferViews.ContainsKey(resourceName))
                        {
                            var node = bufferViews[resourceName];
                            if (node.lastStoreOp != null)
                            {
                                var tr_op = new GpuOp();
                                tr_op.Resources = new GpuResourceRequest[]
                                {
                                        new GpuResourceRequest()
                                        {
                                            Name = resourceName,
                                        }
                                };

                                //propogate any necessary layout transitions and barriers
                                tr_op.barrierOp = true;
                                tr_op.Resources[0].transitionSourceStage = node.lastStoreStage;
                                tr_op.Resources[0].transitionDestStage = opSet[i].Resources[j].FirstLoadStage;

                                //Prepare a memory barrier for the last store ops to be ready for the latest load ops 
                                tr_op.Resources[0].barrierSourceAccess = node.lastStoreAccess;
                                tr_op.Resources[0].barrierDestAccess = opSet[i].Resources[j].Accesses;

                                if (node.ownerQueue != opSet[i].QueueKind)
                                {
                                    //Insert a queue transition op
                                    tr_op.transitionOp = true;
                                    tr_op.transitionDestQueueKind = opSet[i].QueueKind;
                                    tr_op.QueueKind = node.ownerQueue;
                                    tr_op.ownerChangeSemaphoreIdx = semaphoreSet.Count;
                                    //Emit this into both queues
                                    switch (node.ownerQueue)
                                    {
                                        case CommandQueueKind.Compute:
                                            compQ0.Enqueue(tr_op);
                                            break;
                                        case CommandQueueKind.Graphics:
                                            graphQ0.Enqueue(tr_op);
                                            break;
                                        case CommandQueueKind.Transfer:
                                            transQ0.Enqueue(tr_op);
                                            break;
                                        default:
                                            throw new Exception();
                                    };
                                    var waiter_op = new GpuOp(tr_op);
                                    waiter_op.ownerChangeSemaphoreIdx = semaphoreSet.Count;
                                    semaphoreSet.Add(new SemaphoreState()
                                    {
                                        signaler = tr_op,
                                        waiter = waiter_op
                                    });
                                    switch (opSet[i].QueueKind)
                                    {
                                        case CommandQueueKind.Compute:
                                            compQ0.Enqueue(waiter_op);
                                            break;
                                        case CommandQueueKind.Graphics:
                                            graphQ0.Enqueue(waiter_op);
                                            break;
                                        case CommandQueueKind.Transfer:
                                            transQ0.Enqueue(waiter_op);
                                            break;
                                        default:
                                            throw new Exception();
                                    };
                                }
                                else
                                {
                                    switch (opSet[i].QueueKind)
                                    {
                                        case CommandQueueKind.Compute:
                                            compQ0.Enqueue(tr_op);
                                            break;
                                        case CommandQueueKind.Graphics:
                                            graphQ0.Enqueue(tr_op);
                                            break;
                                        case CommandQueueKind.Transfer:
                                            transQ0.Enqueue(tr_op);
                                            break;
                                        default:
                                            throw new Exception();
                                    };
                                }

                                //update the bufferViews state
                                bufferViews[resourceName].lastStoreOp = opSet[i];
                                bufferViews[resourceName].lastStoreStage = opSet[i].Resources[j].LastStoreStage;
                                bufferViews[resourceName].lastStoreAccess = opSet[i].Resources[j].Stores;
                                bufferViews[resourceName].ownerQueue = opSet[i].QueueKind;
                            }
                        }
                    }

                    switch (opSet[i].QueueKind)
                    {
                        case CommandQueueKind.Compute:
                            compQ0.Enqueue(opSet[i]);
                            break;
                        case CommandQueueKind.Graphics:
                            graphQ0.Enqueue(opSet[i]);
                            break;
                        case CommandQueueKind.Transfer:
                            transQ0.Enqueue(opSet[i]);
                            break;
                        default:
                            throw new Exception();
                    };
                }
            }

            while (graphQ0.Count > 0)
            {
                var op = graphQ0.Dequeue();

                if (op.transitionOp)
                {
                    //Generate a semaphore for this if one doesn't already exist
                    if (SemaphoreCache[GraphicsDevice.CurrentFrameID].Count <= semaphoreCntr)
                    {
                        var sem = new GpuSemaphore()
                        {
                            Name = $"Semaphore_{GraphicsDevice.CurrentFrameID}_{semaphoreCntr}"
                        };
                        sem.Build(DeviceIndex, false, 0);
                        SemaphoreCache[GraphicsDevice.CurrentFrameID].Add(sem);
                    }
                    semaphoreSet[op.ownerChangeSemaphoreIdx].semaphore = SemaphoreCache[GraphicsDevice.CurrentFrameID][semaphoreCntr++];
                }
                if (!op.transitionOp && !op.layoutOp && !op.barrierOp)
                {
                    if (ComputePasses.ContainsKey(op.PassName))
                    {
                        var computePass = ComputePasses[op.PassName];
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
                    if (GraphicsPasses.ContainsKey(op.PassName))
                    {
                        var graphicsPass = GraphicsPasses[op.PassName];
                        var renderLayout = graphicsPass.RenderLayout;
                        var descriptorSetup = graphicsPass.DescriptorSetup;

                        //Allocate the renderpass object
                        var rpass = CreateRenderPass(ref renderLayout, graphicsPass.Name);

                        //allocate the framebuffer object
                        CreateFramebuffer(op.ColorAttachments, op.DepthAttachment, ref renderLayout);

                        if (!GraphicsPipelines.ContainsKey(graphicsPass.Name))
                        {
                            //build the pipeline layout
                            var pipelineLayout = CreatePipelineLayout(descriptorSetup, graphicsPass.Name);

                            //allocate the pipeline object
                            var gpipe = new GraphicsPipeline();
                            gpipe.Name = graphicsPass.Name;
                            gpipe.Topology = graphicsPass.Topology;
                            gpipe.DepthClamp = graphicsPass.DepthClamp;
                            gpipe.RasterizerDiscard = graphicsPass.RasterizerDiscard;
                            gpipe.LineWidth = graphicsPass.LineWidth;
                            gpipe.CullMode = graphicsPass.CullMode;
                            gpipe.EnableBlending = graphicsPass.EnableBlending;
                            gpipe.DepthTest = graphicsPass.DepthTest;
                            gpipe.RenderPass = rpass;
                            gpipe.PipelineLayout = pipelineLayout;
                            gpipe.ViewportX = graphicsPass.ViewportX;
                            gpipe.ViewportY = graphicsPass.ViewportY;
                            gpipe.ViewportWidth = graphicsPass.ViewportWidth;
                            gpipe.ViewportHeight = graphicsPass.ViewportHeight;
                            gpipe.ViewportMinDepth = graphicsPass.ViewportMinDepth;
                            gpipe.ViewportMaxDepth = graphicsPass.ViewportMaxDepth;
                            gpipe.ViewportDynamic = graphicsPass.ViewportDynamic;

                            gpipe.Shaders = new ShaderSource[graphicsPass.Shaders.Length];
                            gpipe.SpecializationData = new Memory<int>[graphicsPass.Shaders.Length];
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
                graphQ1.AddLast(op);
            }


            while (compQ0.Count > 0)
            {
                var op = compQ0.Dequeue();

                if (op.transitionOp)
                {
                    //Generate a semaphore for this if one doesn't already exist
                    if (SemaphoreCache[GraphicsDevice.CurrentFrameID].Count <= semaphoreCntr)
                    {
                        var sem = new GpuSemaphore()
                        {
                            Name = $"Semaphore_{GraphicsDevice.CurrentFrameID}_{semaphoreCntr}"
                        };
                        sem.Build(DeviceIndex, false, 0);
                        SemaphoreCache[GraphicsDevice.CurrentFrameID].Add(sem);
                    }
                    semaphoreSet[op.ownerChangeSemaphoreIdx].semaphore = SemaphoreCache[GraphicsDevice.CurrentFrameID][semaphoreCntr++];
                }

                if (ComputePasses.ContainsKey(op.PassName))
                {
                    var computePass = ComputePasses[op.PassName];
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

                compQ1.AddLast(op);
            }

            while (transQ0.Count > 0)
            {
                var op = transQ0.Dequeue();

                if (op.transitionOp)
                {
                    //Generate a semaphore for this if one doesn't already exist
                    if (SemaphoreCache[GraphicsDevice.CurrentFrameID].Count <= semaphoreCntr)
                    {
                        var sem = new GpuSemaphore()
                        {
                            Name = $"Semaphore_{GraphicsDevice.CurrentFrameID}_{semaphoreCntr}"
                        };
                        sem.Build(DeviceIndex, false, 0);
                        SemaphoreCache[GraphicsDevice.CurrentFrameID].Add(sem);
                    }
                    semaphoreSet[op.ownerChangeSemaphoreIdx].semaphore = SemaphoreCache[GraphicsDevice.CurrentFrameID][semaphoreCntr++];
                }

                transQ1.AddLast(op);
            }

            if (SemaphoreCache[GraphicsDevice.CurrentFrameID].Count <= semaphoreCntr)
            {
                var sem = new GpuSemaphore()
                {
                    Name = $"Semaphore_FinalGraphicsSync"
                };
                sem.Build(DeviceIndex, false, 0);
                SemaphoreCache[GraphicsDevice.CurrentFrameID].Add(sem);
            }
            finalGfxSem = SemaphoreCache[GraphicsDevice.CurrentFrameID][semaphoreCntr++];

            if (graphCmds[GraphicsDevice.CurrentFrameID].Length != 0)
            {
                if (transitionBuffer[GraphicsDevice.CurrentFrameID] != null) transitionBuffer[GraphicsDevice.CurrentFrameID].Dispose();
                for (int i = 0; i < graphCmds[GraphicsDevice.CurrentFrameID].Length; i++)
                    graphCmds[GraphicsDevice.CurrentFrameID][i].CmdBuffer.Dispose();
                GraphicsCmdPool[GraphicsDevice.CurrentFrameID].Reset();
            }
            if (compCmds[GraphicsDevice.CurrentFrameID].Length != 0)
            {
                for (int i = 0; i < compCmds[GraphicsDevice.CurrentFrameID].Length; i++)
                    compCmds[GraphicsDevice.CurrentFrameID][i].CmdBuffer.Dispose();
                AsyncComputeCmdPool[GraphicsDevice.CurrentFrameID].Reset();
            }
            if (transCmds[GraphicsDevice.CurrentFrameID].Length != 0)
            {
                for (int i = 0; i < transCmds[GraphicsDevice.CurrentFrameID].Length; i++)
                    transCmds[GraphicsDevice.CurrentFrameID][i].CmdBuffer.Dispose();
                TransferCmdPool[GraphicsDevice.CurrentFrameID].Reset();
            }

            //Process state into semaphores and command buffers
            graphCmds[GraphicsDevice.CurrentFrameID] = GenerateCommands(graphQ1, GraphicsCmdPool[GraphicsDevice.CurrentFrameID], semaphoreSet);
            compCmds[GraphicsDevice.CurrentFrameID] = GenerateCommands(compQ1, AsyncComputeCmdPool[GraphicsDevice.CurrentFrameID], semaphoreSet);
            transCmds[GraphicsDevice.CurrentFrameID] = GenerateCommands(transQ1, TransferCmdPool[GraphicsDevice.CurrentFrameID], semaphoreSet);


            int maxlen = System.Math.Max(graphCmds[GraphicsDevice.CurrentFrameID].Length, System.Math.Max(compCmds[GraphicsDevice.CurrentFrameID].Length, transCmds[GraphicsDevice.CurrentFrameID].Length));
            for (int i = 0; i < maxlen; i++)
            {
                if (i < graphCmds[GraphicsDevice.CurrentFrameID].Length - 2)
                    GraphicsDevice.SubmitCommandBuffer(graphCmds[GraphicsDevice.CurrentFrameID][i].CmdBuffer, graphCmds[GraphicsDevice.CurrentFrameID][i].waiting, graphCmds[GraphicsDevice.CurrentFrameID][i].signalling, null);

                if (i < compCmds[GraphicsDevice.CurrentFrameID].Length)
                    GraphicsDevice.SubmitCommandBuffer(compCmds[GraphicsDevice.CurrentFrameID][i].CmdBuffer, compCmds[GraphicsDevice.CurrentFrameID][i].waiting, compCmds[GraphicsDevice.CurrentFrameID][i].signalling, null);

                if (i < transCmds[GraphicsDevice.CurrentFrameID].Length)
                    GraphicsDevice.SubmitCommandBuffer(transCmds[GraphicsDevice.CurrentFrameID][i].CmdBuffer, transCmds[GraphicsDevice.CurrentFrameID][i].waiting, transCmds[GraphicsDevice.CurrentFrameID][i].signalling, null);
            }

            transitionBuffer[GraphicsDevice.CurrentFrameID] = new CommandBuffer();
            transitionBuffer[GraphicsDevice.CurrentFrameID].Name = "transitionBuffer";
            transitionBuffer[GraphicsDevice.CurrentFrameID].OneTimeSubmit = true;
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

            GraphicsDevice.SubmitCommandBuffer(graphCmds[GraphicsDevice.CurrentFrameID][graphCmds[GraphicsDevice.CurrentFrameID].Length - 1].CmdBuffer, graphCmds[GraphicsDevice.CurrentFrameID][graphCmds[GraphicsDevice.CurrentFrameID].Length - 1].waiting, new GpuSemaphore[] { finalGfxSem }, null);

            //Submit the last graphics command with an additional sync + fence for the frame
            GraphicsDevice.SubmitGraphicsCommandBuffer(transitionBuffer[GraphicsDevice.CurrentFrameID], finalGfxSem);

            buildLock.Release();
        }
    }
}
