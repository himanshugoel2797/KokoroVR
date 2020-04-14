using System;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class Fence : IDisposable
    {
        public string Name { get; set; }
        public bool CreateSignaled { get; set; } = false;
        internal IntPtr hndl;
        internal int devID;
        private bool locked;
        public Fence()
        {

        }

        public void Build(int devID)
        {
            if (!locked)
            {
                unsafe
                {
                    var fenceCreateInfo = new VkFenceCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeFenceCreateInfo,
                        flags = CreateSignaled ? VkFenceCreateFlags.FenceCreateSignaledBit : 0
                    };

                    IntPtr hndl_l = IntPtr.Zero;
                    if (vkCreateFence(GraphicsDevice.GetDeviceInfo(devID).Device, fenceCreateInfo.Pointer(), null, &hndl_l) != VkResult.Success)
                        throw new Exception("Failed to create fence.");
                    hndl = hndl_l;
                    this.devID = devID;

                    if (GraphicsDevice.EnableValidation && Name != null)
                    {
                        var objName = new VkDebugUtilsObjectNameInfoEXT()
                        {
                            sType = VkStructureType.StructureTypeDebugUtilsObjectNameInfoExt,
                            pObjectName = Name,
                            objectType = VkObjectType.ObjectTypeFence,
                            objectHandle = (ulong)hndl
                        };
                        GraphicsDevice.SetDebugUtilsObjectNameEXT(GraphicsDevice.GetDeviceInfo(devID).Device, objName.Pointer());
                    }
                }
                locked = true;
            }
            else
                throw new Exception("Fence is locked.");
        }

        public void Wait()
        {
            if (locked)
            {
                unsafe
                {
                    IntPtr hndl_l = hndl;
                    vkWaitForFences(GraphicsDevice.GetDeviceInfo(devID).Device, 1, &hndl_l, true, ulong.MaxValue);
                }
            }
            else
                throw new Exception("Fence is not built.");
        }

        public void Reset()
        {
            if (locked)
            {
                unsafe
                {
                    IntPtr hndl_l = hndl;
                    vkResetFences(GraphicsDevice.GetDeviceInfo(devID).Device, 1, &hndl_l);
                }
            }
            else
                throw new Exception("Fence is not built.");
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
                    vkDestroyFence(GraphicsDevice.GetDeviceInfo(devID).Device, hndl, null);
                }

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Fence()
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