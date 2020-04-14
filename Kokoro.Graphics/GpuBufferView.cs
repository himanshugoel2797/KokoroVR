using System;
using System.Collections.Generic;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class GpuBufferView : IDisposable
    {
        public string Name { get; set; }
        public ImageFormat Format { get; set; }
        public ulong Offset { get; set; }
        public ulong Size { get; set; }

        internal IntPtr hndl { get; private set; }
        internal GpuBuffer parent;
        private int devID;
        private bool locked;

        public GpuBufferView() { }

        public void Build(GpuBuffer buf)
        {
            if (!locked)
            {
                unsafe
                {
                    var creatInfo = new VkBufferViewCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeBufferViewCreateInfo,
                        buffer = buf.hndl,
                        format = (VkFormat)Format,
                        offset = Offset,
                        range = Size
                    };

                    var devInfo = GraphicsDevice.GetDeviceInfo(buf.devID);
                    IntPtr bufferPtr_l = IntPtr.Zero;
                    if (vkCreateBufferView(devInfo.Device, creatInfo.Pointer(), null, &bufferPtr_l) != VkResult.Success)
                        throw new Exception("Failed to create buffer view.");
                    hndl = bufferPtr_l;
                    parent = buf;
                    devID = buf.devID;

                    if (GraphicsDevice.EnableValidation && Name != null)
                    {
                        var objName = new VkDebugUtilsObjectNameInfoEXT()
                        {
                            sType = VkStructureType.StructureTypeDebugUtilsObjectNameInfoExt,
                            pObjectName = Name,
                            objectType = VkObjectType.ObjectTypeBufferView,
                            objectHandle = (ulong)hndl
                        };
                        GraphicsDevice.SetDebugUtilsObjectNameEXT(GraphicsDevice.GetDeviceInfo(devID).Device, objName.Pointer());
                    }
                }
                locked = true;
            }
            else
                throw new Exception("BufferView is locked.");
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
                    vkDestroyBufferView(GraphicsDevice.GetDeviceInfo(devID).Device, hndl, null);

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~GpuBufferView()
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
