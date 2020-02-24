using System;
using System.Collections.Generic;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public enum CommandQueue
    {
        Graphics,
        Compute,
        Transfer,
        Present
    }
    public class CommandPool : IDisposable
    {
        public bool Transient { get; set; }

        internal IntPtr commandPoolPtr;
        internal int devID;
        internal uint queueFamily;
        internal IntPtr queueFam;
        private bool locked;

        public CommandPool() { }

        public void Build(int device_index, CommandQueue queue)
        {
            if (!locked)
            {
                unsafe
                {
                    var devInfo = GraphicsDevice.GetDeviceInfo(device_index);
                    queueFamily = queue switch
                    {
                        CommandQueue.Graphics => devInfo.GraphicsFamily,
                        CommandQueue.Compute => devInfo.ComputeFamily,
                        CommandQueue.Transfer => devInfo.TransferFamily,
                        CommandQueue.Present => devInfo.PresentFamily,
                        _ => throw new Exception("Unknown command queue type.")
                    };
                    queueFam = queue switch
                    {
                        CommandQueue.Graphics => devInfo.GraphicsQueue,
                        CommandQueue.Compute => devInfo.ComputeQueue,
                        CommandQueue.Transfer => devInfo.TransferQueue,
                        CommandQueue.Present => devInfo.PresentQueue,
                        _ => throw new Exception("Unknown command queue type.")
                    };

                    var poolInfo = new VkCommandPoolCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeCommandPoolCreateInfo,
                        queueFamilyIndex = queueFamily,
                        flags = Transient ? VkCommandPoolCreateFlags.CommandPoolCreateTransientBit : VkCommandPoolCreateFlags.CommandPoolCreateResetCommandBufferBit,
                    };

                    IntPtr commandPoolPtr_l = IntPtr.Zero;
                    if (vkCreateCommandPool(devInfo.Device, poolInfo.Pointer(), null, &commandPoolPtr_l) != VkResult.Success)
                        throw new Exception("Failed to create command pool.");
                    commandPoolPtr = commandPoolPtr_l;
                    devID = device_index;
                }
                locked = true;
            }
            else
                throw new Exception("CommandPool is locked.");
        }

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
                    vkDestroyCommandPool(GraphicsDevice.GetDeviceInfo(devID).Device, commandPoolPtr, null);
                }

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~CommandPool()
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
