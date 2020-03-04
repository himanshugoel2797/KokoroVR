using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class PoolEntry
    {
        public DescriptorType Type;
        public uint Count;
    }

    public class DescriptorPool
    {
        public string Name { get; set; }
        public IReadOnlyList<PoolEntry> PoolEntries { get => poolEntries; }

        internal IntPtr hndl;

        private int devID;
        private bool locked;
        private readonly List<PoolEntry> poolEntries;

        public DescriptorPool()
        {
            poolEntries = new List<PoolEntry>();
        }

        public void Add(DescriptorType type, uint cnt)
        {
            int i = 0;
            for (; i < PoolEntries.Count; i++)
                if (PoolEntries[i].Type == type)
                {
                    PoolEntries[i].Count += cnt;
                    break;
                }
            if (i == PoolEntries.Count)
            {
                var p = new PoolEntry
                {
                    Count = cnt,
                    Type = type
                };
                poolEntries.Add(p);
            }
        }

        public void Build(int devId, uint poolSz)
        {
            if (!locked)
            {
                if (PoolEntries.Count == 0)
                    return;

                unsafe
                {
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
                        flags = VkDescriptorPoolCreateFlags.DescriptorPoolCreateUpdateAfterBindBit,
                    };

                    IntPtr poolPtr = IntPtr.Zero;
                    if (vkCreateDescriptorPool(GraphicsDevice.GetDeviceInfo(devId).Device, poolCreatInfo.Pointer(), null, &poolPtr) != VkResult.Success)
                        throw new Exception("Failed to create descriptor pool.");
                    hndl = poolPtr;

                    if (GraphicsDevice.EnableValidation)
                    {
                        var objName = new VkDebugUtilsObjectNameInfoEXT()
                        {
                            sType = VkStructureType.StructureTypeDebugUtilsObjectNameInfoExt,
                            pObjectName = Name + "_pool",
                            objectType = VkObjectType.ObjectTypeDescriptorPool,
                            objectHandle = (ulong)hndl
                        };
                        GraphicsDevice.SetDebugUtilsObjectNameEXT(GraphicsDevice.GetDeviceInfo(devID).Device, objName.Pointer());
                    }
                }
                devID = devId;
                locked = true;
            }
            else throw new Exception("DescriptorSet is locked.");
        }

    }
}
