using System;
using System.Collections.Generic;
using System.Text;
using VulkanSharp.Raw;
using static VulkanSharp.Raw.Vk;
using static VulkanSharp.Raw.Vma;

namespace Kokoro.Graphics
{
    public class Image : IDisposable
    {
        public string Name { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public uint Depth { get; set; }
        public uint Levels { get; set; }
        public uint Layers { get; set; }
        public uint Dimensions { get; set; }
        public ImageFormat Format { get; set; }
        public ImageUsage Usage { get; set; }
        public bool Cubemappable { get; set; }
        public MemoryUsage MemoryUsage { get; set; }
        public ImageLayout InitialLayout { get; set; }
        public ImageLayout CurrentLayout { get; set; }
        public AccessFlags CurrentAccesses { get; set; } = AccessFlags.None;
        public PipelineStage CurrentUsageStage { get; set; } = PipelineStage.Top;
        public CommandQueueKind OwningQueue { get; set; } = CommandQueueKind.Ignored;

        internal IntPtr hndl { get; private set; }
        internal IntPtr imgAlloc { get; private set; }
        internal VmaAllocationInfo allocInfo { get; private set; }
        internal int devID { get; private set; }
        private bool swapchainImg;
        private bool locked;

        public Image() { }

        internal void Build(int deviceIndex, IntPtr img)
        {
            if (!locked)
            {
                swapchainImg = true;
                devID = deviceIndex;
                this.hndl = img;
                locked = true;
            }
            else throw new Exception("Image is locked.");
        }

        public void Build(int deviceIndex)
        {
            if (!locked)
            {
                var devInfo = GraphicsDevice.GetDeviceInfo(deviceIndex);

                unsafe
                {
                    fixed (uint* queueFamInds = devInfo.QueueFamilyIndices)
                    {
                        var creatInfo = new VkImageCreateInfo()
                        {
                            sType = VkStructureType.StructureTypeImageCreateInfo,
                            flags = 0,
                            format = (VkFormat)Format,
                            usage = (VkImageUsageFlags)Usage,
                            mipLevels = Levels,
                            arrayLayers = Layers,
                            extent = new VkExtent3D()
                            {
                                width = Width,
                                height = Height,
                                depth = Depth
                            },
                            samples = VkSampleCountFlags.SampleCount1Bit,
                            tiling = VkImageTiling.ImageTilingOptimal,
                            sharingMode = VkSharingMode.SharingModeExclusive,
                            initialLayout = (VkImageLayout)InitialLayout,
                            pQueueFamilyIndices = queueFamInds,
                            queueFamilyIndexCount = (uint)devInfo.QueueFamilyIndices.Length,
                        };
                        creatInfo.imageType = Dimensions switch
                        {
                            1 => VkImageType.ImageType1d,
                            2 => VkImageType.ImageType2d,
                            3 => VkImageType.ImageType3d,
                            _ => throw new Exception("Unknown Image Shape.")
                        };
                        var vmaCreatInfo = new VmaAllocationCreateInfo()
                        {
                            usage = (VmaMemoryUsage)MemoryUsage
                        };

                        var allocInfo_p = new ManagedPtr<VmaAllocationInfo>();
                        var res = GraphicsDevice.CreateImage(deviceIndex, creatInfo.Pointer(), vmaCreatInfo.Pointer(), out var img_l, out var imgAlloc_l, allocInfo_p);
                        hndl = img_l;
                        imgAlloc = imgAlloc_l;
                        allocInfo = allocInfo_p.Value;
                        devID = deviceIndex;

                        CurrentLayout = InitialLayout;
                        CurrentAccesses = AccessFlags.None;
                        CurrentUsageStage = PipelineStage.Top;

                        if (GraphicsDevice.EnableValidation)
                        {
                            var objName = new VkDebugUtilsObjectNameInfoEXT()
                            {
                                sType = VkStructureType.StructureTypeDebugUtilsObjectNameInfoExt,
                                pObjectName = Name,
                                objectType = VkObjectType.ObjectTypeImage,
                                objectHandle = (ulong)hndl
                            };
                            GraphicsDevice.SetDebugUtilsObjectNameEXT(GraphicsDevice.GetDeviceInfo(deviceIndex).Device, objName.Pointer());
                        }
                    }
                }
                locked = true;
            }
            else
                throw new Exception("Image is locked.");
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
                if (locked && !swapchainImg) GraphicsDevice.DestroyImage(devID, hndl, imgAlloc);

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Image()
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
