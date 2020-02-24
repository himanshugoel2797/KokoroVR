﻿using System;
using System.Collections.Generic;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class Sampler : IDisposable
    {
        public bool UnnormalizedCoords { get; set; }
        public bool MinLinearFilter { get; set; }
        public bool MagLinearFilter { get; set; }
        public bool MipLinearFilter { get; set; }
        public EdgeMode EdgeU { get; set; }
        public EdgeMode EdgeV { get; set; }
        public EdgeMode EdgeW { get; set; }
        public BorderColor Border { get; set; }
        public float AnisotropicSamples { get; set; }

        internal IntPtr samplerPtr { get; private set; }
        private int devID;
        private bool locked;

        public Sampler() { }

        public void Build(int deviceIndex)
        {
            if (!locked)
            {
                unsafe
                {
                    var samplerCreatInfo = new VkSamplerCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeSamplerCreateInfo,
                        magFilter = MagLinearFilter ? VkFilter.FilterLinear : VkFilter.FilterNearest,
                        minFilter = MinLinearFilter ? VkFilter.FilterLinear : VkFilter.FilterNearest,
                        mipmapMode = MipLinearFilter ? VkSamplerMipmapMode.SamplerMipmapModeLinear : VkSamplerMipmapMode.SamplerMipmapModeNearest,
                        addressModeU = (VkSamplerAddressMode)EdgeU,
                        addressModeV = (VkSamplerAddressMode)EdgeV,
                        addressModeW = (VkSamplerAddressMode)EdgeW,
                        mipLodBias = 0,
                        anisotropyEnable = AnisotropicSamples == 0,
                        maxAnisotropy = AnisotropicSamples,
                        compareEnable = false,
                        compareOp = VkCompareOp.CompareOpAlways,
                        minLod = -1000,
                        maxLod = 1000,
                        borderColor = (VkBorderColor)Border,
                        unnormalizedCoordinates = UnnormalizedCoords
                    };

                    IntPtr samplerPtr_l = IntPtr.Zero;
                    if (vkCreateSampler(GraphicsDevice.GetDeviceInfo(deviceIndex).Device, samplerCreatInfo.Pointer(), null, &samplerPtr_l) != VkResult.Success)
                        throw new Exception("Failed to create sampler.");
                    samplerPtr = samplerPtr_l;
                    devID = deviceIndex;
                }
                locked = true;
            }
            else
                throw new Exception("Sampler is locked.");
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
                if (locked) vkDestroySampler(GraphicsDevice.GetDeviceInfo(devID).Device, samplerPtr, null);

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Sampler()
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
