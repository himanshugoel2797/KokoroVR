using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class CommandBuffer : IDisposable
    {
        public string Name { get; set; }
        public bool IsRecording { get; private set; }

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
                IsRecording = false;
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

                IsRecording = true;
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

                IsRecording = false;
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
        public void SetPipeline(GraphicsPipeline pipeline, Framebuffer framebuffer, float depthClearVal)
        {
            if (locked)
            {
                unsafe
                {
                    //var clearVal_arr = new VkClearValue[pipeline.Framebuffer.Attachments.Count];
                    var clearVal_len = (framebuffer.ColorAttachments == null ? 0 : framebuffer.ColorAttachments.Length) * 4 + (framebuffer.DepthAttachment == null ? 0 : 4);
                    var clearVal_ptrs = stackalloc float[clearVal_len];
                    for (int i = 0; i < clearVal_len; i++)
                    {
                        clearVal_ptrs[i] = 0;
                    }

                    var beginInfo = new VkRenderPassBeginInfo()
                    {
                        sType = VkStructureType.StructureTypeRenderPassBeginInfo,
                        renderPass = pipeline.RenderPass.hndl,
                        framebuffer = framebuffer.hndl,
                        renderArea = new VkRect2D()
                        {
                            offset = new VkOffset2D()
                            {
                                x = 0,
                                y = 0
                            },
                            extent = new VkExtent2D()
                            {
                                width = framebuffer.Width,
                                height = framebuffer.Height
                            }
                        },
                        clearValueCount = (uint)clearVal_len / 4,
                        pClearValues = (IntPtr)clearVal_ptrs,
                    };
                    vkCmdBeginRenderPass(hndl, beginInfo.Pointer(), VkSubpassContents.SubpassContentsInline);
                    vkCmdBindPipeline(hndl, VkPipelineBindPoint.PipelineBindPointGraphics, pipeline.hndl);
                }
            }
            else
                throw new Exception("Command buffer not built.");
        }

        public void SetPipeline(ComputePipeline pipeline)
        {
            if (locked)
            {
                unsafe
                {
                    vkCmdBindPipeline(hndl, VkPipelineBindPoint.PipelineBindPointCompute, pipeline.hndl);
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
                        var dyn_cnt = set.Layout.Layouts.Count(a => a.Type == DescriptorType.UniformBufferDynamic);
                        var ptrs = stackalloc IntPtr[] { set.hndl };
                        var dyn_off = stackalloc uint[dyn_cnt];  //NOTE Added for the 0th binding global uniform buffer
                        for (int i = 0; i < dyn_cnt; i++) dyn_off[i] = 0;
                        vkCmdBindDescriptorSets(hndl, (VkPipelineBindPoint)bindPoint, layout.hndl, set_binding, 1, ptrs, (uint)dyn_cnt, dyn_off);
                    }
                }
            }
            else
                throw new Exception("Command buffer not built.");
        }
        #endregion

        #region Staging
        public void Stage(GpuBuffer src, ulong src_off, GpuBuffer dst, ulong dst_off, ulong len)
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
                /*vkCmdPipelineBarrier(hndl, VkPipelineStageFlags.PipelineStageTransferBit, VkPipelineStageFlags.PipelineStageAllCommandsBit, 0, 0, null, 1, new VkBufferMemoryBarrier()
                {
                    sType = VkStructureType.StructureTypeBufferMemoryBarrier,
                    buffer = dst.hndl,
                    offset = dst_off,
                    srcAccessMask = VkAccessFlags.AccessTransferWriteBit,
                    dstAccessMask = 0,
                    srcQueueFamilyIndex = cmdPool.queueFamily,
                    dstQueueFamilyIndex = VkQueueFamilyIgnored,
                    size = len,
                }.Pointer(), 0, null);*/
            }
            else
                throw new Exception("Command buffer not built.");
        }

        public void Barrier(PipelineStage srcStage, PipelineStage dstStage, BufferMemoryBarrier[] bufferBarriers, ImageMemoryBarrier[] imageBarriers)
        {
            if (locked)
            {
                var bufferBarrier = new VkBufferMemoryBarrier[bufferBarriers == null ? 0 : bufferBarriers.Length];
                if(bufferBarriers != null)
                    for(int i = 0; i < bufferBarrier.Length; i++)
                    {
                        bufferBarrier[i] = new VkBufferMemoryBarrier()
                        {
                            sType = VkStructureType.StructureTypeBufferMemoryBarrier,
                            buffer = bufferBarriers[i].Buffer.hndl,
                            srcQueueFamilyIndex = GraphicsDevice.GetFamilyIndex(devID, bufferBarriers[i].SrcFamily),
                            dstQueueFamilyIndex = GraphicsDevice.GetFamilyIndex(devID, bufferBarriers[i].DstFamily),
                            srcAccessMask = (VkAccessFlags)bufferBarriers[i].Accesses,
                            dstAccessMask = (VkAccessFlags)bufferBarriers[i].Stores,
                            offset = bufferBarriers[i].Offset,
                            size = bufferBarriers[i].Size,
                        };
                    }

                var imageBarrier = new VkImageMemoryBarrier[imageBarriers == null ? 0 : imageBarriers.Length];
                if (imageBarriers != null)
                    for (int i = 0; i < imageBarrier.Length; i++)
                    {
                        imageBarrier[i] = new VkImageMemoryBarrier()
                        {
                            sType = VkStructureType.StructureTypeImageMemoryBarrier,
                            oldLayout = (VkImageLayout)imageBarriers[i].OldLayout,
                            newLayout = (VkImageLayout)imageBarriers[i].NewLayout,
                            image = imageBarriers[i].Image.hndl,
                            srcQueueFamilyIndex = GraphicsDevice.GetFamilyIndex(devID, imageBarriers[i].SrcFamily),
                            dstQueueFamilyIndex = GraphicsDevice.GetFamilyIndex(devID, imageBarriers[i].DstFamily),
                            srcAccessMask = (VkAccessFlags)imageBarriers[i].Accesses,
                            dstAccessMask = (VkAccessFlags)imageBarriers[i].Stores,
                            subresourceRange = new VkImageSubresourceRange()
                            {
                                aspectMask = (imageBarriers[i].Image.Format == ImageFormat.Depth32f ? VkImageAspectFlags.ImageAspectDepthBit : VkImageAspectFlags.ImageAspectColorBit),
                                baseArrayLayer = imageBarriers[i].BaseArrayLayer,
                                baseMipLevel = imageBarriers[i].BaseMipLevel,
                                layerCount = imageBarriers[i].LayerCount,
                                levelCount = imageBarriers[i].LevelCount,
                            },
                        };
                    }
                var imageBarrier_ptr = imageBarrier.Pointer();
                var bufferBarrier_ptr = bufferBarrier.Pointer();
                vkCmdPipelineBarrier(hndl, (VkPipelineStageFlags)srcStage, (VkPipelineStageFlags)dstStage, 0, 0, IntPtr.Zero, (uint)bufferBarrier.Length, bufferBarrier_ptr, (uint)imageBarrier.Length, imageBarrier_ptr);
            }
            else
                throw new Exception("Command buffer not built.");
        }

        public void Stage(GpuBuffer src, ulong src_off, Image dst)
        {
            if (locked)
            {
                unsafe
                {
                    var bufCopy = new VkBufferImageCopy()
                    {
                        bufferOffset = src_off,
                        bufferRowLength = 0,
                        bufferImageHeight = 0,
                        imageSubresource = new VkImageSubresourceLayers()
                        {
                            aspectMask = VkImageAspectFlags.ImageAspectColorBit,
                            mipLevel = 0,
                            baseArrayLayer = 0,
                            layerCount = 1
                        },
                        imageOffset = new VkOffset3D()
                        {
                            x = 0,
                            y = 0,
                            z = 0
                        },
                        imageExtent = new VkExtent3D()
                        {
                            width = dst.Width,
                            height = dst.Height,
                            depth = dst.Depth
                        }
                    };
                    vkCmdCopyBufferToImage(hndl, src.hndl, dst.hndl, (VkImageLayout)dst.InitialLayout, 1, bufCopy.Pointer());
                }
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

        public void DrawIndexed(GpuBuffer indexBuffer, ulong offset, IndexType indexType, uint indexCnt, uint instanceCnt, uint firstIndex, int firstVertex, uint baseInstance)
        {
            if (locked)
            {
                vkCmdBindIndexBuffer(hndl, indexBuffer.hndl, offset, (VkIndexType)indexType);
                vkCmdDrawIndexed(hndl, indexCnt, instanceCnt, firstIndex, firstVertex, baseInstance);
            }
            else
                throw new Exception("Command buffer not built.");
        }

        public void DrawIndirect(GpuBuffer drawBuffer, ulong offset, GpuBuffer cntBuffer, ulong cntOffset, uint maxCnt, uint stride)
        {
            if (locked)
            {
                vkCmdDrawIndirectCount(hndl, drawBuffer.hndl, offset, cntBuffer.hndl, cntOffset, maxCnt, stride);
            }
            else
                throw new Exception("Command buffer not built.");
        }

        public void DrawIndexedIndirect(GpuBuffer indexBuffer, ulong indexOffset, IndexType indexType, GpuBuffer drawBuffer, ulong offset, GpuBuffer cntBuffer, ulong cntOffset, uint maxCnt, uint stride)
        {
            if (locked)
            {
                vkCmdBindIndexBuffer(hndl, indexBuffer.hndl, indexOffset, (VkIndexType)indexType);
                vkCmdDrawIndexedIndirectCount(hndl, drawBuffer.hndl, offset, cntBuffer.hndl, cntOffset, maxCnt, stride);
            }
            else
                throw new Exception("Command buffer not built.");
        }
        #endregion

        #region Compute
        public void Dispatch(uint x, uint y, uint z)
        {
            if (locked)
            {
                vkCmdDispatch(hndl, x, y, z);
            }
            else
                throw new Exception("Command buffer not built.");
        }

        public void DispatchIndirect(GpuBuffer indirectBuf, ulong offset)
        {
            if (locked)
            {
                vkCmdDispatchIndirect(hndl, indirectBuf.hndl, offset);
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
                    unsafe
                    {
                        IntPtr buf_hndl = hndl;
                        vkFreeCommandBuffers(GraphicsDevice.GetDeviceInfo(devID).Device, cmdPool.hndl, 1, &buf_hndl);
                    }
                    //throw new NotImplementedException("Figure out how to handle command buffer resets");
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
