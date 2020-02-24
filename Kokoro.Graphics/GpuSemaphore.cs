using System;
using System.Collections.Generic;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class GpuSemaphore : IDisposable
    {
        internal IntPtr semaphorePtr;
        internal int devID;
        private bool locked;

        public GpuSemaphore() { }

        public void Build(int deviceIndex)
        {
            if (!locked)
            {
                unsafe
                {
                    var semaphoreInfo = new VkSemaphoreCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeSemaphoreCreateInfo,
                    };

                    IntPtr semaphorePtr_l = IntPtr.Zero;
                    if (vkCreateSemaphore(GraphicsDevice.GetDeviceInfo(deviceIndex).Device, semaphoreInfo.Pointer(), null, &semaphorePtr_l) != VkResult.Success)
                        throw new Exception("Failed to create semaphore.");
                    semaphorePtr = semaphorePtr_l;
                    devID = deviceIndex;
                }
                locked = true;
            }
            else
                throw new Exception("GpuSemaphore is locked.");
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
                    vkDestroySemaphore(GraphicsDevice.GetDeviceInfo(devID).Device, semaphorePtr, null);
                }

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~GpuSemaphore()
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
