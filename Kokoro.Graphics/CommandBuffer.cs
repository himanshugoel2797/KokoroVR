using System;
using System.Collections.Generic;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class CommandBuffer : IDisposable
    {
        public string Name { get; set; }

        internal IntPtr hndl;
        internal CommandPool cmdPool;
        private int devID;
        private bool locked;

        public CommandBuffer() { }

        public void Build(CommandPool pool)
        {
            if (!locked)
            {
                unsafe
                {
                    var allocInfo = new VkCommandBufferAllocateInfo()
                    {
                        sType = VkStructureType.StructureTypeCommandBufferAllocateInfo,
                        commandBufferCount = 1,
                        commandPool = pool.hndl,
                        level = VkCommandBufferLevel.CommandBufferLevelPrimary,
                    };

                    devID = pool.devID;
                    IntPtr cmdBufferPtr_l = IntPtr.Zero;
                    if (vkAllocateCommandBuffers(GraphicsDevice.GetDeviceInfo(devID).Device, allocInfo.Pointer(), &cmdBufferPtr_l) != VkResult.Success)
                        throw new Exception("Failed to allocate command buffer.");
                    cmdPool = pool;
                    hndl = cmdBufferPtr_l;

                    if (GraphicsDevice.EnableValidation)
                    {
                        var objName = new VkDebugUtilsObjectNameInfoEXT()
                        {
                            sType = VkStructureType.StructureTypeDebugUtilsObjectNameInfoExt,
                            pObjectName = Name,
                            objectType = VkObjectType.ObjectTypeCommandBuffer,
                            objectHandle = (ulong)hndl
                        };
                        GraphicsDevice.SetDebugUtilsObjectNameEXT(GraphicsDevice.GetDeviceInfo(devID).Device, objName.Pointer());
                    }
                }
                locked = true;
            }
            else
                throw new Exception("Command buffer locked.");
        }

        #region Recording
        public void Reset()
        {
            if (locked)
            {
                vkResetCommandBuffer(hndl, 0);
            }
            else
                throw new Exception("Command buffer not built.");
        }

        public void BeginRecording()
        {
            if (locked)
            {
                var beginInfo = new VkCommandBufferBeginInfo()
                {
                    sType = VkStructureType.StructureTypeCommandBufferBeginInfo,
                    flags = 0,
                    pInheritanceInfo = IntPtr.Zero
                };

                if (vkBeginCommandBuffer(hndl, beginInfo.Pointer()) != VkResult.Success)
                    throw new Exception("Failed to begin recording command buffer.");
            }
            else
                throw new Exception("Command buffer not built.");
        }

        public void EndRecording()
        {
            if (locked)
            {
                if (vkEndCommandBuffer(hndl) != VkResult.Success)
                    throw new Exception("Failed to end recording command buffer.");
            }
            else
                throw new Exception("Command buffer not built.");
        }
        #endregion

        #region Viewport
        public void SetViewport(uint x, uint y, uint w, uint h)
        {
            if (locked)
            {
                var vpt = new VkViewport()
                {
                    x = x,
                    y = y,
                    width = w,
                    height = h,
                    minDepth = 0,
                    maxDepth = 1,
                };
                var vpt_ptr = vpt.Pointer();
                vkCmdSetViewport(hndl, 0, 1, vpt_ptr);
            }
            else
                throw new Exception("Command buffer not built.");
        }
        #endregion

        #region Render Pass
        public void SetPipeline(Pipeline pipeline, float depthClearVal)
        {
            if (locked)
            {
                unsafe
                {
                    //var clearVal_arr = new VkClearValue[pipeline.Framebuffer.Attachments.Count];
                    var clearVal_ptrs = stackalloc float[pipeline.Framebuffer.Attachments.Count * 4];
                    for (int i = 0; i < pipeline.Framebuffer.Attachments.Count * 4; i++)
                    {
                        clearVal_ptrs[i] = 0;
                    }

                    var beginInfo = new VkRenderPassBeginInfo()
                    {
                        sType = VkStructureType.StructureTypeRenderPassBeginInfo,
                        renderPass = pipeline.RenderPass.hndl,
                        framebuffer = pipeline.Framebuffer.hndl,
                        renderArea = new VkRect2D()
                        {
                            offset = new VkOffset2D()
                            {
                                x = 0,
                                y = 0
                            },
                            extent = new VkExtent2D()
                            {
                                width = pipeline.Framebuffer.Width,
                                height = pipeline.Framebuffer.Height
                            }
                        },
                        clearValueCount = (uint)pipeline.Framebuffer.Attachments.Count,
                        pClearValues = (IntPtr)clearVal_ptrs,
                    };
                    vkCmdBeginRenderPass(hndl, beginInfo.Pointer(), VkSubpassContents.SubpassContentsInline);
                    vkCmdBindPipeline(hndl, VkPipelineBindPoint.PipelineBindPointGraphics, pipeline.hndl);
                }
            }
            else
                throw new Exception("Command buffer not built.");
        }

        public void EndRenderPass()
        {
            if (locked)
            {
                vkCmdEndRenderPass(hndl);
            }
            else
                throw new Exception("Command buffer not built.");
        }
        #endregion

        #region Descriptors
        public void SetDescriptors(PipelineLayout layout, DescriptorSet set, DescriptorBindPoint bindPoint, uint set_binding)
        {
            if (locked)
            {
                unsafe
                {
                    if (set != null && set.hndl != IntPtr.Zero)
                    {
                        var ptrs = stackalloc IntPtr[] { set.hndl };
                        vkCmdBindDescriptorSets(hndl, (VkPipelineBindPoint)bindPoint, layout.hndl, set_binding, 1, ptrs, 0, null);
                    }
                }
            }
            else
                throw new Exception("Command buffer not built.");
        }
        #endregion

        #region Staging
        public void Stage<T>(GpuBuffer src, ulong src_off, GpuBuffer dst, ulong dst_off, ulong len) where T : unmanaged
        {
            if (locked)
            {
                var bufCopy = new VkBufferCopy()
                {
                    dstOffset = dst_off,
                    srcOffset = src_off,
                    size = len
                };
                vkCmdCopyBuffer(hndl, src.hndl, dst.hndl, 1, bufCopy.Pointer());
            }
            else
                throw new Exception("Command buffer not built.");
        }
        #endregion

        #region Draw
        public void Draw(uint vertexCnt, uint instanceCnt, uint firstVertex, uint baseInstance)
        {
            if (locked)
            {
                vkCmdDraw(hndl, vertexCnt, instanceCnt, firstVertex, baseInstance);
            }
            else
                throw new Exception("Command buffer not built.");
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                if (locked)
                {
                    throw new NotImplementedException("Figure out how to handle command buffer resets");
                }

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~CommandBuffer()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
