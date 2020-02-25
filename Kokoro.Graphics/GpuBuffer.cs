using System;
using System.Collections.Generic;
using System.Text;
using VulkanSharp.Raw;
using static VulkanSharp.Raw.Vk;
using static VulkanSharp.Raw.Vma;

namespace Kokoro.Graphics
{
    public class GpuBuffer : IDisposable
    {
        public ulong Size { get; set; }
        public BufferUsage Usage { get; set; }
        public MemoryUsage MemoryUsage { get; set; }
        public bool Mapped { get; set; }

        internal IntPtr buf { get; private set; }
        internal IntPtr bufAlloc { get; private set; }
        internal VmaAllocationInfo allocInfo { get; private set; }
        internal int devID { get; private set; }
        private bool locked;

        public GpuBuffer() { }

        public void Build(int device_index)
        {
            if (!locked)
            {
                unsafe
                {
                    var devInfo = GraphicsDevice.GetDeviceInfo(device_index);
                    fixed (uint* queueFamInds = devInfo.QueueFamilyIndices)
                    {
                        var creatInfo = new VkBufferCreateInfo()
                        {
                            sType = VkStructureType.StructureTypeBufferCreateInfo,
                            flags = 0,
                            size = Size,
                            usage = (VkBufferUsageFlags)Usage,
                            sharingMode = VkSharingMode.SharingModeConcurrent,
                            pQueueFamilyIndices = queueFamInds,
                            queueFamilyIndexCount = (uint)devInfo.QueueFamilyIndices.Length,
                        };

                        var vmaCreatInfo = new VmaAllocationCreateInfo()
                        {
                            flags = Mapped ? VmaAllocationCreateFlags.MappedBit : 0,
                            usage = (VmaMemoryUsage)MemoryUsage
                        };

                        var allocInfo_p = new ManagedPtr<VmaAllocationInfo>();
                        var res = GraphicsDevice.CreateBuffer(device_index, creatInfo.Pointer(), vmaCreatInfo.Pointer(), out var buf_l, out var bufAlloc_l, allocInfo_p);
                        buf = buf_l;
                        bufAlloc = bufAlloc_l;
                        allocInfo = allocInfo_p.Value;
                        devID = device_index;
                    }
                }
                locked = true;
            }
            else
                throw new Exception("Buffer is locked.");
        }

        public IntPtr GetAddress()
        {
            unsafe
            {
                return (IntPtr)allocInfo.pMappedData;
            }
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
                if (locked) GraphicsDevice.DestroyBuffer(devID, buf, bufAlloc);

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~GpuBuffer()
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
