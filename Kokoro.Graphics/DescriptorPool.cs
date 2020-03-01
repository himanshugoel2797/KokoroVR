using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class DescriptorLayout
    {
        public uint BindingIndex;
        public DescriptorType Type;
        public uint Count;
        public ShaderType Stages;
        public Sampler[] ImmutableSamplers;
    }

    public class PoolEntry
    {
        public DescriptorType Type;
        public uint Count;
    }

    public class DescriptorPool
    {

        public string Name { get; set; }
        public IReadOnlyList<DescriptorLayout> Layouts { get => layouts; }
        public IReadOnlyList<PoolEntry> PoolEntries { get => poolEntries; }
        
        internal int layoutCount = 0;
        internal IntPtr layout_hndl;
        internal IntPtr pool_hndl;
        
        private int devID;
        private bool locked;
        private readonly List<DescriptorLayout> layouts;
        private readonly List<PoolEntry> poolEntries;

        public DescriptorPool()
        {
            layouts = new List<DescriptorLayout>();
            poolEntries = new List<PoolEntry>();
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
            layoutCount++;
            layouts.Add(l);

            int i = 0;
            for (; i < PoolEntries.Count; i++)
                if (PoolEntries[i].Type == type)
                {
                    PoolEntries[i].Count++;
                    break;
                }
            if (i == PoolEntries.Count)
            {
                var p = new PoolEntry
                {
                    Count = 1,
                    Type = type
                };
                poolEntries.Add(p);
            }
        }

        public void Add(uint bindingIdx, DescriptorType type, uint cnt, ShaderType stage, Sampler[] samplers)
        {
            var l = new DescriptorLayout()
            {
                BindingIndex = bindingIdx,
                Type = type,
                Count = cnt,
                Stages = stage,
                ImmutableSamplers = samplers
            };
            layoutCount++;
            layouts.Add(l);

            int i = 0;
            for (; i < PoolEntries.Count; i++)
                if (PoolEntries[i].Type == type)
                {
                    PoolEntries[i].Count++;
                    break;
                }
            if (i == PoolEntries.Count)
            {
                var p = new PoolEntry
                {
                    Count = 1,
                    Type = type
                };
                poolEntries.Add(p);
            }
        }

        public void Build(int devId, uint poolSz)
        {
            if (!locked)
            {
                if (Layouts.Count == 0 | PoolEntries.Count == 0)
                    return;

                unsafe
                {
                    var bindings = new VkDescriptorSetLayoutBinding[Layouts.Count];
                    var bindingFlagSet = stackalloc VkDescriptorBindingFlags[bindings.Length];
                    var bindingSamplers = new Memory<IntPtr>[bindings.Length];
                    var bindingSamplers_hndls = new MemoryHandle[bindings.Length];
                    for (int i = 0; i < Layouts.Count; i++)
                    {
                        if (Layouts[i].ImmutableSamplers != null)
                        {
                            bindingSamplers[i] = new Memory<IntPtr>(Layouts[i].ImmutableSamplers.Select(a => a.hndl).ToArray());
                            bindingSamplers_hndls[i] = bindingSamplers[i].Pin();
                        }

                        bindingFlagSet[i] =
                            VkDescriptorBindingFlags.DescriptorBindingPartiallyBoundBit |
                            VkDescriptorBindingFlags.DescriptorBindingUpdateAfterBindBit |
                            VkDescriptorBindingFlags.DescriptorBindingUpdateUnusedWhilePendingBit;
                        bindings[i].binding = Layouts[i].BindingIndex;
                        bindings[i].descriptorCount = (uint)Layouts[i].Count;
                        bindings[i].descriptorType = (VkDescriptorType)Layouts[i].Type;
                        bindings[i].stageFlags = (VkShaderStageFlags)Layouts[i].Stages;
                        bindings[i].pImmutableSamplers = Layouts[i].ImmutableSamplers == null ? null : (IntPtr*)bindingSamplers_hndls[i].Pointer;
                    }
                    var bindings_ptr = bindings.Pointer();

                    var bindingFlags = new VkDescriptorSetLayoutBindingFlagsCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeDescriptorSetLayoutBindingFlagsCreateInfo,
                        bindingCount = (uint)bindings.Length,
                        pBindingFlags = bindingFlagSet
                    };
                    var bindingFlags_ptr = bindingFlags.Pointer();

                    var creatInfo = new VkDescriptorSetLayoutCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeDescriptorSetLayoutCreateInfo,
                        bindingCount = (uint)bindings.Length,
                        pBindings = bindings_ptr,
                        pNext = bindingFlags_ptr,
                        flags = VkDescriptorSetLayoutCreateFlags.DescriptorSetLayoutCreateUpdateAfterBindPoolBit
                    };
                    IntPtr setLayoutPtr = IntPtr.Zero;
                    if (vkCreateDescriptorSetLayout(GraphicsDevice.GetDeviceInfo(devId).Device, creatInfo.Pointer(), null, &setLayoutPtr) != VkResult.Success)
                        throw new Exception("Failed to create descriptor set.");
                    layout_hndl = setLayoutPtr;

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
                    pool_hndl = poolPtr;

                    if (GraphicsDevice.EnableValidation)
                    {
                        var objName = new VkDebugUtilsObjectNameInfoEXT()
                        {
                            sType = VkStructureType.StructureTypeDebugUtilsObjectNameInfoExt,
                            pObjectName = Name + "_layout",
                            objectType = VkObjectType.ObjectTypeDescriptorSetLayout,
                            objectHandle = (ulong)layout_hndl
                        };
                        GraphicsDevice.SetDebugUtilsObjectNameEXT(GraphicsDevice.GetDeviceInfo(devID).Device, objName.Pointer());

                        objName = new VkDebugUtilsObjectNameInfoEXT()
                        {
                            sType = VkStructureType.StructureTypeDebugUtilsObjectNameInfoExt,
                            pObjectName = Name + "_pool",
                            objectType = VkObjectType.ObjectTypeDescriptorPool,
                            objectHandle = (ulong)pool_hndl
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
