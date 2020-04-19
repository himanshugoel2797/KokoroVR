using System.Reflection.Metadata;
using System;
using System.Collections.Generic;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class GpuQueue
    {
        public CommandQueueKind QueueKind { get; }
        public uint Family { get; }
        public IntPtr Handle { get; }
        public int DeviceIndex { get; }

        internal GpuQueue(CommandQueueKind queue_kind, IntPtr hndl, uint family, int device_index)
        {
            QueueKind = queue_kind;
            Handle = hndl;
            Family = family;
            DeviceIndex = device_index;

            if (GraphicsDevice.EnableValidation)
            {
                var objName = new VkDebugUtilsObjectNameInfoEXT()
                {
                    sType = VkStructureType.StructureTypeDebugUtilsObjectNameInfoExt,
                    pObjectName = $"{QueueKind.ToString()}_{family}",
                    objectType = VkObjectType.ObjectTypeQueue,
                    objectHandle = (ulong)hndl
                };

                var p = objName.Pointer();
                GraphicsDevice.SetDebugUtilsObjectNameEXT(GraphicsDevice.GetDeviceInfo(device_index).Device, p);
            }
        }


        #region Submit
        public void SubmitCommandBuffer(CommandBuffer buffer, GpuSemaphore[] waitSems, GpuSemaphore[] signalSems, Fence fence)
        {
            if (buffer.IsEmpty)
                throw new Exception();

            unsafe
            {
                var waitSemaphores = stackalloc IntPtr[waitSems == null ? 0 : waitSems.Length];
                var signalSemaphores = stackalloc IntPtr[signalSems == null ? 0 : signalSems.Length];
                var waitSemaphoreVals = stackalloc ulong[waitSems == null ? 0 : waitSems.Length];
                var signalSemaphoreVals = stackalloc ulong[signalSems == null ? 0 : signalSems.Length];

                bool hasTimeline = false;

                for (int i = 0; i < (waitSems == null ? 0 : waitSems.Length); i++)
                {
                    waitSemaphoreVals[i] = waitSems[i].timeline ? GraphicsDevice.CurrentFrameCount : 1;
                    waitSemaphores[i] = waitSems[i].hndl;

                    if (waitSems[i].timeline)
                        hasTimeline = true;
                }

                for (int i = 0; i < (signalSems == null ? 0 : signalSems.Length); i++)
                {
                    signalSemaphoreVals[i] = signalSems[i].timeline ? GraphicsDevice.CurrentFrameCount + 1 : 0;
                    signalSemaphores[i] = signalSems[i].hndl;

                    if (signalSems[i].timeline)
                        hasTimeline = true;
                }

                var waitStages = stackalloc VkPipelineStageFlags[waitSems == null ? 0 : waitSems.Length];
                var cmdBuffers = stackalloc IntPtr[] { buffer.hndl };

                for (int i = 0; i < (waitSems == null ? 0 : waitSems.Length); i++)
                    waitStages[i] = VkPipelineStageFlags.PipelineStageColorAttachmentOutputBit;

                var timelineSems = new VkTimelineSemaphoreSubmitInfo()
                {
                    sType = VkStructureType.StructureTypeTimelineSemaphoreSubmitInfo,
                    signalSemaphoreValueCount = signalSems == null ? 0 : (uint)signalSems.Length,
                    waitSemaphoreValueCount = waitSems == null ? 0 : (uint)waitSems.Length,
                    pSignalSemaphoreValues = signalSemaphoreVals,
                    pWaitSemaphoreValues = waitSemaphoreVals
                };
                var timelineSems_ptr = timelineSems.Pointer();

                var submitInfo = new VkSubmitInfo()
                {
                    sType = VkStructureType.StructureTypeSubmitInfo,
                    waitSemaphoreCount = waitSems == null ? 0 : (uint)waitSems.Length,
                    pWaitSemaphores = waitSemaphores,
                    pWaitDstStageMask = waitStages,
                    commandBufferCount = 1,
                    pCommandBuffers = cmdBuffers,
                    signalSemaphoreCount = signalSems == null ? 0 : (uint)signalSems.Length,
                    pSignalSemaphores = signalSemaphores,
                    pNext = hasTimeline ? timelineSems_ptr : IntPtr.Zero
                };
                var ptr = submitInfo.Pointer();
                if (vkQueueSubmit(Handle, 1, ptr, fence == null ? IntPtr.Zero : fence.hndl) != VkResult.Success)
                    throw new Exception("Failed to submit command buffer.");
            }
        }
        #endregion
    }
}
