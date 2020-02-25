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
        }


        #region Submit
        public void SubmitCommandBuffer(CommandBuffer buffer, GpuSemaphore[] waitSems, GpuSemaphore[] signalSems, Fence fence)
        {
            unsafe
            {
                var waitSemaphores = stackalloc IntPtr[waitSems.Length];
                var waitTimelines = stackalloc IntPtr[waitSems.Length];
                var signalSemaphores = stackalloc IntPtr[signalSems.Length];
                var signalTimelines = stackalloc IntPtr[signalSems.Length];

                uint waitSemCnt = 0, waitTimeCnt = 0, signalSemCnt = 0, signalTimeCnt = 0;

                for (int i = 0; i < waitSems.Length; i++)
                    if (waitSems[i].timeline)
                        waitTimelines[waitTimeCnt++] = waitSems[i].semaphorePtr;
                    else
                        waitSemaphores[waitSemCnt++] = waitSems[i].semaphorePtr;

                for (int i = 0; i < signalSems.Length; i++)
                    if (signalSems[i].timeline)
                        signalTimelines[signalTimeCnt++] = signalSems[i].semaphorePtr;
                    else
                        signalSemaphores[signalSemCnt++] = signalSems[i].semaphorePtr;

                var waitStages = stackalloc VkPipelineStageFlags[] { VkPipelineStageFlags.PipelineStageTopOfPipeBit };
                var cmdBuffers = stackalloc IntPtr[] { buffer.cmdBufferPtr };

                if (waitTimeCnt > 0 | signalTimeCnt > 0)
                    throw new NotImplementedException("Submit does not support timeline semaphores yet.");

                var submitInfo = new VkSubmitInfo()
                {
                    sType = VkStructureType.StructureTypeSubmitInfo,
                    waitSemaphoreCount = waitSemCnt,
                    pWaitSemaphores = waitSemaphores,
                    pWaitDstStageMask = waitStages,
                    commandBufferCount = 1,
                    pCommandBuffers = cmdBuffers,
                    signalSemaphoreCount = signalSemCnt,
                    pSignalSemaphores = signalSemaphores
                };
                var ptr = submitInfo.Pointer();
                if (vkQueueSubmit(Handle, 1, ptr, fence.hndl) != VkResult.Success)
                    throw new Exception("Failed to submit command buffer.");
            }
        }
        #endregion
    }
}
