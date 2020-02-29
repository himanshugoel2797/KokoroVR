using System;
using System.Collections.Generic;
using System.Text;
using VulkanSharp.Raw;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class ImageView : IDisposable
    {
        public string Name { get; set; }
        public ImageFormat Format { get; set; }
        public ImageViewType ViewType { get; set; }
        public uint BaseLevel { get; set; }
        public uint LevelCount { get; set; }
        public uint BaseLayer { get; set; }
        public uint LayerCount { get; set; }

        internal IntPtr hndl { get; private set; }
        internal Image parent;
        internal int devID;
        private bool locked;

        public ImageView() { }

        public void Build(Image img)
        {
            if (!locked)
            {
                unsafe
                {
                    var creatInfo = new VkImageViewCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeImageViewCreateInfo,
                        flags = 0,
                        image = img.hndl,
                        viewType = (VkImageViewType)ViewType,
                        format = (VkFormat)Format,
                        components = new VkComponentMapping()
                        {
                            a = VkComponentSwizzle.ComponentSwizzleIdentity,
                            b = VkComponentSwizzle.ComponentSwizzleIdentity,
                            g = VkComponentSwizzle.ComponentSwizzleIdentity,
                            r = VkComponentSwizzle.ComponentSwizzleIdentity,
                        },
                    };

                    switch (Format)
                    {
                        case ImageFormat.Depth16f:
                        case ImageFormat.Depth32f:
                            creatInfo.subresourceRange = new VkImageSubresourceRange()
                            {
                                aspectMask = VkImageAspectFlags.ImageAspectDepthBit,
                                baseMipLevel = BaseLevel,
                                levelCount = LevelCount,
                                baseArrayLayer = BaseLayer,
                                layerCount = LayerCount,
                            };
                            break;
                        default:
                            creatInfo.subresourceRange = new VkImageSubresourceRange()
                            {
                                aspectMask = VkImageAspectFlags.ImageAspectColorBit,
                                baseMipLevel = BaseLevel,
                                levelCount = LevelCount,
                                baseArrayLayer = BaseLayer,
                                layerCount = LayerCount,
                            };
                            break;
                    }

                    var devInfo = GraphicsDevice.GetDeviceInfo(img.devID);
                    unsafe
                    {
                        IntPtr viewPtr_p = IntPtr.Zero;
                        var res = vkCreateImageView(devInfo.Device, creatInfo.Pointer(), null, &viewPtr_p);
                        hndl = viewPtr_p;
                        parent = img;
                        devID = img.devID;
                        if (res != VkResult.Success)
                            throw new Exception("Failed to create view.");
                    }

                    if (GraphicsDevice.EnableValidation)
                    {
                        var objName = new VkDebugUtilsObjectNameInfoEXT()
                        {
                            sType = VkStructureType.StructureTypeDebugUtilsObjectNameInfoExt,
                            pObjectName = Name,
                            objectType = VkObjectType.ObjectTypeImageView,
                            objectHandle = (ulong)hndl
                        };
                        GraphicsDevice.SetDebugUtilsObjectNameEXT(GraphicsDevice.GetDeviceInfo(devID).Device, objName.Pointer());
                    }
                }

                locked = true;
            }
            else
                throw new Exception("ImageView is locked.");
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
                    vkDestroyImageView(GraphicsDevice.GetDeviceInfo(devID).Device, hndl, null);

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~ImageView()
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
