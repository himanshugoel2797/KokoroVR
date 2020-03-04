using System.Buffers;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class DescriptorSet
    {
        public string Name { get; set; }
        public DescriptorPool Pool { get; set; }
        public DescriptorLayout Layout { get; set; }
        internal IntPtr hndl;
        private int devID;
        private bool locked;

        public void Build(int devId)
        {
            if (!locked)
            {
                if (Layout.Layouts.Count == 0 | Pool.PoolEntries.Count == 0)
                    return;

                unsafe
                {
                    var layout_sets = stackalloc IntPtr[] { Layout.hndl };

                    var desc_set_alloc_info = new VkDescriptorSetAllocateInfo()
                    {
                        sType = VkStructureType.StructureTypeDescriptorSetAllocateInfo,
                        descriptorPool = Pool.hndl,
                        descriptorSetCount = 1,
                        pSetLayouts = layout_sets,
                    };

                    fixed(IntPtr* hndl_p = &hndl)
                    if (vkAllocateDescriptorSets(GraphicsDevice.GetDeviceInfo(devId).Device, desc_set_alloc_info.Pointer(), hndl_p) != VkResult.Success)
                        throw new Exception("Failed to allocate descriptor sets.");
                    devID = devId;

                    if (GraphicsDevice.EnableValidation)
                    {
                        var objName = new VkDebugUtilsObjectNameInfoEXT()
                        {
                            sType = VkStructureType.StructureTypeDebugUtilsObjectNameInfoExt,
                            pObjectName = Name,
                            objectType = VkObjectType.ObjectTypeDescriptorSet,
                            objectHandle = (ulong)hndl
                        };
                        GraphicsDevice.SetDebugUtilsObjectNameEXT(GraphicsDevice.GetDeviceInfo(devID).Device, objName.Pointer());
                    }
                }
                locked = true;
            }
            else throw new Exception("DescriptorSet is locked.");
        }

        public void Set(uint binding, uint idx, ImageView img, Sampler sampler)
        {
            var img_info = new VkDescriptorImageInfo()
            {
                sampler = sampler.hndl,
                imageView = img.hndl,
                imageLayout = VkImageLayout.ImageLayoutShaderReadOnlyOptimal
            };
            var img_info_ptr = img_info.Pointer();

            var desc_write = new VkWriteDescriptorSet()
            {
                sType = VkStructureType.StructureTypeWriteDescriptorSet,
                dstSet = hndl,
                dstBinding = binding,
                dstArrayElement = idx,
                descriptorCount = 1,
                pImageInfo = img_info_ptr,
                pBufferInfo = IntPtr.Zero,
                pTexelBufferView = null,
                descriptorType = (VkDescriptorType)Layout.Layouts[(int)binding].Type
            };

            vkUpdateDescriptorSets(GraphicsDevice.GetDeviceInfo(devID).Device, 1, desc_write.Pointer(), 0, null);
        }

        public void Set(uint binding, uint idx, ImageView img, bool rw)
        {
            var img_info = new VkDescriptorImageInfo()
            {
                sampler = IntPtr.Zero,
                imageView = img.hndl,
                imageLayout = VkImageLayout.ImageLayoutGeneral
            };
            var img_info_ptr = img_info.Pointer();

            var desc_write = new VkWriteDescriptorSet()
            {
                sType = VkStructureType.StructureTypeWriteDescriptorSet,
                dstSet = hndl,
                dstBinding = binding,
                dstArrayElement = idx,
                descriptorCount = 1,
                pImageInfo = img_info_ptr,
                pBufferInfo = IntPtr.Zero,
                pTexelBufferView = null,
                descriptorType = (VkDescriptorType)Layout.Layouts[(int)binding].Type
            };

            vkUpdateDescriptorSets(GraphicsDevice.GetDeviceInfo(devID).Device, 1, desc_write.Pointer(), 0, null);
        }

        public void Set(uint binding, uint idx, GpuBuffer buf, ulong off, ulong len)
        {
            var buf_info = new VkDescriptorBufferInfo()
            {
                buffer = buf.hndl,
                offset = off,
                range = len
            };
            var buf_info_ptr = buf_info.Pointer();

            var desc_write = new VkWriteDescriptorSet()
            {
                sType = VkStructureType.StructureTypeWriteDescriptorSet,
                dstSet = hndl,
                dstBinding = binding,
                dstArrayElement = idx,
                descriptorCount = 1,
                pImageInfo = IntPtr.Zero,
                pBufferInfo = buf_info_ptr,
                pTexelBufferView = null,
                descriptorType = (VkDescriptorType)Layout.Layouts[(int)binding].Type
            };

            vkUpdateDescriptorSets(GraphicsDevice.GetDeviceInfo(devID).Device, 1, desc_write.Pointer(), 0, null);
        }

        public void Set(uint binding, uint idx, GpuBufferView buf)
        {
            unsafe
            {
                IntPtr p_l = buf.hndl;
                var desc_write = new VkWriteDescriptorSet()
                {
                    sType = VkStructureType.StructureTypeWriteDescriptorSet,
                    dstSet = hndl,
                    dstBinding = binding,
                    dstArrayElement = idx,
                    descriptorCount = 1,
                    pImageInfo = IntPtr.Zero,
                    pBufferInfo = IntPtr.Zero,
                    pTexelBufferView = &p_l,
                    descriptorType = (VkDescriptorType)Layout.Layouts[(int)binding].Type
                };

                vkUpdateDescriptorSets(GraphicsDevice.GetDeviceInfo(devID).Device, 1, desc_write.Pointer(), 0, null);
            }
        }
    }
}
