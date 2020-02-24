using System;
using System.Collections.Generic;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class DescriptorSet
    {
        private class DescriptorLayout
        {
            public uint BindingIndex;
            public DescriptorType Type;
            public uint Count;
            public ShaderType Stages;
        }

        private class PoolEntry
        {
            public DescriptorType Type;
            public uint Count;
        }

        private List<DescriptorLayout> Layouts;
        private List<PoolEntry> PoolEntries;
        private IntPtr descSetLayout;
        private IntPtr descPool;
        private IntPtr[] sets;
        private uint setCnt;
        private int devID;
        private bool locked;

        public DescriptorSet()
        {
            Layouts = new List<DescriptorLayout>();
            PoolEntries = new List<PoolEntry>();
        }

        public void Add(uint bindingIdx, DescriptorType type, uint cnt, ShaderType stage)
        {
            var l = new DescriptorLayout()
            {
                BindingIndex = bindingIdx,
                Type = type,
                Count = cnt,
                Stages = stage
            };

            int i = 0;
            for (; i < PoolEntries.Count; i++)
                if (PoolEntries[i].Type == type)
                {
                    PoolEntries[i].Count++;
                    break;
                }
            if (i == PoolEntries.Count)
            {
                var p = new PoolEntry();
                p.Count = 1;
                p.Type = type;
                PoolEntries.Add(p);
            }
        }

        public void Build(int devId, uint poolSz)
        {
            if (!locked)
            {
                unsafe
                {
                    var bindings = new VkDescriptorSetLayoutBinding[Layouts.Count];
                    for (int i = 0; i < Layouts.Count; i++)
                    {
                        bindings[i].binding = Layouts[i].BindingIndex;
                        bindings[i].descriptorCount = (uint)Layouts[i].Count;
                        bindings[i].descriptorType = (VkDescriptorType)Layouts[i].Type;
                        bindings[i].stageFlags = (VkShaderStageFlags)Layouts[i].Stages;
                        bindings[i].pImmutableSamplers = null;
                    }
                    var bindings_ptr = bindings.Pointer();

                    var creatInfo = new VkDescriptorSetLayoutCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeDescriptorSetLayoutCreateInfo,
                        bindingCount = (uint)bindings.Length,
                        pBindings = bindings_ptr
                    };
                    IntPtr setLayoutPtr = IntPtr.Zero;
                    if (vkCreateDescriptorSetLayout(GraphicsDevice.GetDeviceInfo(devId).Device, creatInfo.Pointer(), null, &setLayoutPtr) != VkResult.Success)
                        throw new Exception("Failed to create descriptor set.");
                    descSetLayout = setLayoutPtr;

                    var psize = new VkDescriptorPoolSize[PoolEntries.Count];
                    for (int i = 0; i < PoolEntries.Count; i++)
                    {
                        psize[i].type = (VkDescriptorType)PoolEntries[i].Type;
                        psize[i].descriptorCount = PoolEntries[i].Count;
                    }
                    var psize_p = psize.Pointer();
                    var poolCreatInfo = new VkDescriptorPoolCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeDescriptorPoolCreateInfo,
                        maxSets = poolSz,
                        poolSizeCount = (uint)PoolEntries.Count,
                        pPoolSizes = psize_p,
                    };

                    IntPtr poolPtr = IntPtr.Zero;
                    if (vkCreateDescriptorPool(GraphicsDevice.GetDeviceInfo(devId).Device, poolCreatInfo.Pointer(), null, &poolPtr) != VkResult.Success)
                        throw new Exception("Failed to create descriptor pool.");

                    var layout_sets = new IntPtr[poolSz];
                    for (uint i = 0; i < poolSz; i++)
                        layout_sets[i] = descSetLayout;

                    fixed (IntPtr* layout_sets_ptr = layout_sets)
                    {
                        var desc_set_alloc_info = new VkDescriptorSetAllocateInfo()
                        {
                            sType = VkStructureType.StructureTypeDescriptorSetAllocateInfo,
                            descriptorPool = descPool,
                            descriptorSetCount = poolSz,
                            pSetLayouts = layout_sets_ptr
                        };

                        setCnt = poolSz;
                        sets = new IntPtr[setCnt];
                        fixed (IntPtr* sets_p = sets)
                            if (vkAllocateDescriptorSets(GraphicsDevice.GetDeviceInfo(devId).Device, desc_set_alloc_info.Pointer(), sets_p) != VkResult.Success)
                                throw new Exception("Failed to allocate descriptor sets.");
                        devID = devId;
                    }
                }
                locked = true;
            }
            else throw new Exception("DescriptorSet is locked.");
        }

        public void Set(uint set, uint binding, uint idx, ImageView img, Sampler sampler)
        {
            if (set > setCnt)
                throw new IndexOutOfRangeException("set is out of range.");

            var img_info = new VkDescriptorImageInfo()
            {
                sampler = sampler.samplerPtr,
                imageView = img.viewPtr,
                imageLayout = VkImageLayout.ImageLayoutShaderReadOnlyOptimal
            };
            var img_info_ptr = img_info.Pointer();

            var desc_write = new VkWriteDescriptorSet()
            {
                sType = VkStructureType.StructureTypeWriteDescriptorSet,
                dstSet = sets[set],
                dstBinding = binding,
                dstArrayElement = idx,
                descriptorCount = 1,
                pImageInfo = img_info_ptr,
                pBufferInfo = IntPtr.Zero,
                pTexelBufferView = null
            };

            vkUpdateDescriptorSets(GraphicsDevice.GetDeviceInfo(devID).Device, 1, desc_write.Pointer(), 0, null);
        }

        public void Set(uint set, uint binding, uint idx, ImageView img, bool rw)
        {
            if (set > setCnt)
                throw new IndexOutOfRangeException("set is out of range.");

            var img_info = new VkDescriptorImageInfo()
            {
                sampler = IntPtr.Zero,
                imageView = img.viewPtr,
                imageLayout = VkImageLayout.ImageLayoutGeneral
            };
            var img_info_ptr = img_info.Pointer();

            var desc_write = new VkWriteDescriptorSet()
            {
                sType = VkStructureType.StructureTypeWriteDescriptorSet,
                dstSet = sets[set],
                dstBinding = binding,
                dstArrayElement = idx,
                descriptorCount = 1,
                pImageInfo = img_info_ptr,
                pBufferInfo = IntPtr.Zero,
                pTexelBufferView = null
            };

            vkUpdateDescriptorSets(GraphicsDevice.GetDeviceInfo(devID).Device, 1, desc_write.Pointer(), 0, null);
        }

        public void Set(uint set, uint binding, uint idx, GpuBuffer buf, ulong off, ulong len)
        {
            if (set > setCnt)
                throw new IndexOutOfRangeException("set is out of range.");

            var buf_info = new VkDescriptorBufferInfo()
            {
                buffer = buf.buf,
                offset = off,
                range = len
            };
            var buf_info_ptr = buf_info.Pointer();

            var desc_write = new VkWriteDescriptorSet()
            {
                sType = VkStructureType.StructureTypeWriteDescriptorSet,
                dstSet = sets[set],
                dstBinding = binding,
                dstArrayElement = idx,
                descriptorCount = 1,
                pImageInfo = IntPtr.Zero,
                pBufferInfo = buf_info_ptr,
                pTexelBufferView = null
            };

            vkUpdateDescriptorSets(GraphicsDevice.GetDeviceInfo(devID).Device, 1, desc_write.Pointer(), 0, null);
        }

        public void Set(uint set, uint binding, uint idx, GpuBufferView buf)
        {
            if (set > setCnt)
                throw new IndexOutOfRangeException("set is out of range.");
            unsafe
            {
                IntPtr p_l = buf.bufferPtr;
                var desc_write = new VkWriteDescriptorSet()
                {
                    sType = VkStructureType.StructureTypeWriteDescriptorSet,
                    dstSet = sets[set],
                    dstBinding = binding,
                    dstArrayElement = idx,
                    descriptorCount = 1,
                    pImageInfo = IntPtr.Zero,
                    pBufferInfo = IntPtr.Zero,
                    pTexelBufferView = &p_l
                };

                vkUpdateDescriptorSets(GraphicsDevice.GetDeviceInfo(devID).Device, 1, desc_write.Pointer(), 0, null);
            }
        }
    }
}
