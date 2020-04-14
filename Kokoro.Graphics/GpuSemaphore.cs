using System;
using System.Collections.Generic;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class GpuSemaphore : IDisposable
    {
        public string Name { get; set; }
        internal IntPtr hndl;
        internal int devID;
        internal bool timeline;
        private bool locked;

        public GpuSemaphore() { }

        public void Build(int deviceIndex, bool timeline, ulong value)
        {
            if (!locked)
            {
                unsafe
                {
                    IntPtr semaphorePtr_l = IntPtr.Zero;
                    this.timeline = timeline;
                    if (timeline)
                    {
                        var semaphoreTypeInfo = new VkSemaphoreTypeCreateInfo()
                        {
                            sType = VkStructureType.StructureTypeSemaphoreTypeCreateInfo,
                            semaphoreType = VkSemaphoreType.SemaphoreTypeTimeline,
                            initialValue = value
                        };
                        var semaphoreTypeInfo_ptr = semaphoreTypeInfo.Pointer();

                        var semaphoreInfo = new VkSemaphoreCreateInfo()
                        {
                            sType = VkStructureType.StructureTypeSemaphoreCreateInfo,
                            pNext = semaphoreTypeInfo_ptr
                        };

                        if (vkCreateSemaphore(GraphicsDevice.GetDeviceInfo(deviceIndex).Device, semaphoreInfo.Pointer(), null, &semaphorePtr_l) != VkResult.Success)
                            throw new Exception("Failed to create semaphore.");
                    }
                    else
                    {
                        var semaphoreInfo = new VkSemaphoreCreateInfo()
                        {
                            sType = VkStructureType.StructureTypeSemaphoreCreateInfo,
                        };

                        if (vkCreateSemaphore(GraphicsDevice.GetDeviceInfo(deviceIndex).Device, semaphoreInfo.Pointer(), null, &semaphorePtr_l) != VkResult.Success)
                            throw new Exception("Failed to create semaphore.");
                    }
                    hndl = semaphorePtr_l;
                    devID = deviceIndex;

                    if (GraphicsDevice.EnableValidation && Name != null)
                    {
                        var objName = new VkDebugUtilsObjectNameInfoEXT()
                        {
                            sType = VkStructureType.StructureTypeDebugUtilsObjectNameInfoExt,
                            pObjectName = Name,
                            objectType = VkObjectType.ObjectTypeSemaphore,
                            objectHandle = (ulong)hndl
                        };
                        GraphicsDevice.SetDebugUtilsObjectNameEXT(GraphicsDevice.GetDeviceInfo(deviceIndex).Device, objName.Pointer());
                    }
                }
                locked = true;
            }
            else
                throw new Exception("GpuSemaphore is locked.");
        }

        public void Signal(ulong val)
        {
            if (locked)
            {
                if (!timeline) throw new Exception("Only timeline semaphores support signaling.");
                unsafe
                {
                    var signalInfo = new VkSemaphoreSignalInfo()
                    {
                        sType = VkStructureType.StructureTypeSemaphoreSignalInfo,
                        semaphore = hndl,
                        value = val
                    };
                    vkSignalSemaphore(GraphicsDevice.GetDeviceInfo(devID).Device, signalInfo.Pointer());
                }
            }
            else
                throw new Exception("GpuSemaphore is not built.");
        }

        public void Wait(ulong val)
        {
            if (locked)
            {
                if (!timeline) throw new Exception("Only timeline semaphores support waiting.");
                unsafe
                {
                    var ptrs = stackalloc IntPtr[] { hndl };
                    var val_ptrs = stackalloc ulong[] { val };
                    var waitInfo = new VkSemaphoreWaitInfo()
                    {
                        sType = VkStructureType.StructureTypeSemaphoreWaitInfo,
                        semaphoreCount = 1,
                        pSemaphores = ptrs,
                        pValues = val_ptrs
                    };
                    vkWaitSemaphores(GraphicsDevice.GetDeviceInfo(devID).Device, waitInfo.Pointer(), ulong.MaxValue);
                }
            }
            else
                throw new Exception("GpuSemaphore is not built.");
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
                    vkDestroySemaphore(GraphicsDevice.GetDeviceInfo(devID).Device, hndl, null);
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
