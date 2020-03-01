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
            unsafe
            {
                var waitSemaphores = stackalloc IntPtr[waitSems.Length];
                var signalSemaphores = stackalloc IntPtr[signalSems.Length];
                var waitSemaphoreVals = stackalloc ulong[waitSems.Length];
                var signalSemaphoreVals = stackalloc ulong[signalSems.Length];

                for (int i = 0; i < waitSems.Length; i++)
                {
                    waitSemaphoreVals[i] = GraphicsDevice.CurrentFrameCount;
                    waitSemaphores[i] = waitSems[i].hndl;
                }

                for (int i = 0; i < signalSems.Length; i++)
                {
                    signalSemaphoreVals[i] = GraphicsDevice.CurrentFrameCount + 1;
                    signalSemaphores[i] = signalSems[i].hndl;
                }

                var waitStages = stackalloc VkPipelineStageFlags[waitSems.Length];
                var cmdBuffers = stackalloc IntPtr[] { buffer.hndl };

                for (int i = 0; i < waitSems.Length; i++)
                    waitStages[i] = VkPipelineStageFlags.PipelineStageColorAttachmentOutputBit;

                var timelineSems = new VkTimelineSemaphoreSubmitInfo()
                {
                    sType = VkStructureType.StructureTypeTimelineSemaphoreSubmitInfo,
                    signalSemaphoreValueCount = (uint)signalSems.Length,
                    waitSemaphoreValueCount = (uint)waitSems.Length,
                    pSignalSemaphoreValues = signalSemaphoreVals,
                    pWaitSemaphoreValues = waitSemaphoreVals
                };
                var timelineSems_ptr = timelineSems.Pointer();

                var submitInfo = new VkSubmitInfo()
                {
                    sType = VkStructureType.StructureTypeSubmitInfo,
                    waitSemaphoreCount = (uint)waitSems.Length,
                    pWaitSemaphores = waitSemaphores,
                    pWaitDstStageMask = waitStages,
                    commandBufferCount = 1,
                    pCommandBuffers = cmdBuffers,
                    signalSemaphoreCount = (uint)signalSems.Length,
                    pSignalSemaphores = signalSemaphores,
                    pNext = timelineSems_ptr
                };
                var ptr = submitInfo.Pointer();
                if (vkQueueSubmit(Handle, 1, ptr, fence == null ? IntPtr.Zero : fence.hndl) != VkResult.Success)
                    throw new Exception("Failed to submit command buffer.");
            }
        }
        #endregion
    }
}
