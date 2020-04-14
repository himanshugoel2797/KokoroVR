using System;
using System.Collections.Generic;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class ShaderResource
    {
        public DescriptorType Type { get; set; }
        public GpuBuffer[] Buffers { get; set; }
        public GpuBufferView[] BufferViews { get; set; }
        public Sampler[] Samplers { get; set; }
        public Image[] Images { get; set; }
        public ImageView[] ImageViews { get; set; }
    }

    public class ShaderResourceSet
    {
        public string Name { get; set; }
        public ShaderResource[] Resources { get; set; }
    }

    public class AsyncComputeTask
    {
        public string Name { get; set; }
        public ShaderSource Task { get; set; }
        public ShaderResourceSetReference[] ShaderParameters { get; set; }
        public ShaderResourceSetReference IndirectParameters { get; set; }
        public uint[] WorkDims { get; set; }
    }

    public class TransferCmd
    {
        public GpuBuffer SrcBuffer { get; set; }
        public Image DstImage { get; set; }
        public GpuBuffer DstBuffer { get; set; }
        public ulong SrcOffset { get; set; }
        public ulong DstOffset { get; set; }
        public ulong Length { get; set; }
    }

    public class ShaderResourceUpdateCmd
    {
        public string TargetSetName { get; set; }
        public TransferCmd TransferOp { get; set; }
        public AsyncComputeTask AsyncComputeOp { get; set; }
    }

    public class ShaderResourceManager
    {
        class BuiltShaderResourceSet
        {
            public ShaderResourceSet setDesc;
            public DescriptorLayout layout;
            public DescriptorSet set;
            public List<GpuSemaphore> pendingSemaphores;
        }

        class BuiltUpdateCmd
        {
            public ShaderResourceUpdateCmd cmd;
            public CommandBuffer transferBuffer;

            public GpuSemaphore fin_semaphore;
            public CommandBuffer cmdbuffer;
        }

        public string Name { get; private set; }
        private Dictionary<string, BuiltShaderResourceSet> ShaderResources;
        private List<BuiltUpdateCmd> Updates;
        private DescriptorPool pool;
        private CommandPool transferPool;
        private int devID;

        public ShaderResourceManager(string name, uint maxSets, uint descTypeCnt, int deviceIndex)
        {
            Name = name;
            ShaderResources = new Dictionary<string, BuiltShaderResourceSet>();
            Updates = new List<BuiltUpdateCmd>();
            devID = deviceIndex;

            pool = new DescriptorPool();
            pool.Add(DescriptorType.CombinedImageSampler, descTypeCnt);
            pool.Add(DescriptorType.SampledImage, descTypeCnt);
            pool.Add(DescriptorType.Sampler, descTypeCnt);
            pool.Add(DescriptorType.StorageBuffer, descTypeCnt);
            pool.Add(DescriptorType.StorageImage, descTypeCnt);
            pool.Add(DescriptorType.StorageTexelBuffer, descTypeCnt);
            pool.Add(DescriptorType.UniformBuffer, descTypeCnt);
            pool.Add(DescriptorType.UniformBufferDynamic, descTypeCnt);
            pool.Add(DescriptorType.UniformTexelBuffer, descTypeCnt);
            pool.Build(0, maxSets);

            transferPool = new CommandPool()
            {
                Name = name + "_Transfer",
                Transient = true
            };
            transferPool.Build(deviceIndex, CommandQueueKind.Transfer);
        }

        public void Add(ShaderResourceSet set)
        {
            DescriptorLayout layout = new DescriptorLayout();
            for (uint i = 0; i < set.Resources.Length; i++)
            {
                uint cnt = 1;
                switch (set.Resources[i].Type)
                {
                    case DescriptorType.CombinedImageSampler:
                    case DescriptorType.StorageImage:
                    case DescriptorType.SampledImage:
                        cnt = (uint)set.Resources[i].ImageViews.Length;
                        break;
                    case DescriptorType.Sampler:
                        cnt = (uint)set.Resources[i].Samplers.Length;
                        break;
                    case DescriptorType.StorageBuffer:
                    case DescriptorType.UniformBuffer:
                    case DescriptorType.UniformBufferDynamic:
                        cnt = (uint)set.Resources[i].Buffers.Length;
                        break;
                    case DescriptorType.StorageTexelBuffer:
                    case DescriptorType.UniformTexelBuffer:
                        cnt = (uint)set.Resources[i].BufferViews.Length;
                        break;
                    default:
                        throw new Exception("Unrecognized Descriptor Type.");
                }

                if (set.Resources[i].Type == DescriptorType.CombinedImageSampler || set.Resources[i].Type == DescriptorType.Sampler)
                    layout.Add(i, set.Resources[i].Type, cnt, ShaderType.All, set.Resources[i].Samplers);
                else
                    layout.Add(i, set.Resources[i].Type, cnt, ShaderType.All);
            }
            layout.Build(devID);

            var dset = new DescriptorSet()
            {
                Layout = layout,
                Pool = pool,
                Name = set.Name
            };

            for (uint i = 0; i < set.Resources.Length; i++)
                switch (set.Resources[i].Type)
                {
                    case DescriptorType.StorageImage:
                    case DescriptorType.SampledImage:
                        {
                            for (uint j = 0; j < set.Resources[i].ImageViews.Length; j++)
                                dset.Set(i, j, set.Resources[i].ImageViews[j], set.Resources[i].Type == DescriptorType.StorageImage ? true : false);
                        }
                        break;
                    case DescriptorType.CombinedImageSampler:
                        {
                            for (uint j = 0; j < set.Resources[i].ImageViews.Length; j++)
                                dset.Set(i, j, set.Resources[i].ImageViews[j], set.Resources[i].Samplers[j]);
                        }
                        break;
                    case DescriptorType.StorageBuffer:
                    case DescriptorType.UniformBuffer:
                    case DescriptorType.UniformBufferDynamic:
                        {
                            for (uint j = 0; j < set.Resources[i].Buffers.Length; j++)
                                dset.Set(i, j, set.Resources[i].Buffers[j], 0, set.Resources[i].Buffers[j].Size);
                        }
                        break;
                    case DescriptorType.StorageTexelBuffer:
                    case DescriptorType.UniformTexelBuffer:
                        {
                            for (uint j = 0; j < set.Resources[i].BufferViews.Length; j++)
                                dset.Set(i, j, set.Resources[i].BufferViews[j]);
                        }
                        break;
                }
            dset.Build(devID);

            ShaderResources[set.Name] = new BuiltShaderResourceSet()
            {
                setDesc = set,
                set = dset,
                layout = layout,
                pendingSemaphores = new List<GpuSemaphore>(),
            };
        }

        public void ResetUpdates()
        {
            Updates.Clear();
        }

        public void RegisterUpdate(ShaderResourceUpdateCmd cmd)
        {
            var x = new BuiltUpdateCmd()
            {
                cmd = cmd,
                transferBuffer = new CommandBuffer(),
            };
            x.transferBuffer.Build(transferPool);
            Updates.Add(x);
        }

        public void Process()
        {
            transferPool.Reset();

            //Generate a command buffer for each command
            for (int i = 0; i < Updates.Count; i++)
            {
                var u = Updates[i];
                var uc = u.cmd;

                ShaderResources[uc.TargetSetName].pendingSemaphores.Clear();    //Clear pending semaphore lists from previous operations

                u.cmdbuffer = null;
                u.fin_semaphore = null;

                if (uc.TransferOp != null)
                {
                    //Generate transfer op
                    u.fin_semaphore = new GpuSemaphore();
                    u.fin_semaphore.Build(devID, true, GraphicsDevice.CurrentFrameCount);

                    u.cmdbuffer = u.transferBuffer;
                    u.cmdbuffer.BeginRecording();
                    if (uc.TransferOp.DstImage != null)
                    {
                        //Image upload
                        u.cmdbuffer.Stage(uc.TransferOp.SrcBuffer, uc.TransferOp.SrcOffset, uc.TransferOp.DstImage);
                    }
                    else
                    {
                        //Buffer upload
                        u.cmdbuffer.Stage(uc.TransferOp.SrcBuffer, uc.TransferOp.SrcOffset, uc.TransferOp.DstBuffer, uc.TransferOp.DstOffset, uc.TransferOp.Length);
                    }
                    u.cmdbuffer.EndRecording();
                }

                //TODO: Add method for CommandBuffers to take ownership for images
                //Submit these commands
                if (u.cmdbuffer != null)
                {
                    GraphicsDevice.SubmitCommandBuffer(u.cmdbuffer, null, new GpuSemaphore[] { u.fin_semaphore }, null);
                    ShaderResources[uc.TargetSetName].pendingSemaphores.Add(u.fin_semaphore);
                }
                else
                    throw new Exception("finalCmds should never be null.");
            }
        }
    }
}
