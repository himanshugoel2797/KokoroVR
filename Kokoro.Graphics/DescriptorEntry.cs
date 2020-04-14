using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class DescriptorEntry
    {
        public uint BindingIndex;
        public DescriptorType Type;
        public uint Count;
        public ShaderType Stages;
        public Sampler[] ImmutableSamplers;
    }

    public class DescriptorLayout
    {
        public string Name { get; set; }
        public IReadOnlyList<DescriptorEntry> Layouts { get => layouts; }

        internal int layoutCount = 0;
        internal IntPtr hndl;
        private readonly List<DescriptorEntry> layouts;
        private int devID;
        private bool locked;

        public DescriptorLayout()
        {
            layouts = new List<DescriptorEntry>();
        }

        public void Add(uint bindingIdx, DescriptorType type, uint cnt, ShaderType stage)
        {
            var l = new DescriptorEntry()
            {
                BindingIndex = bindingIdx,
                Type = type,
                Count = cnt,
                Stages = stage
            };
            layoutCount++;
            layouts.Add(l);
        }

        public void Add(uint bindingIdx, DescriptorType type, uint cnt, ShaderType stage, Sampler[] samplers)
        {
            var l = new DescriptorEntry()
            {
                BindingIndex = bindingIdx,
                Type = type,
                Count = cnt,
                Stages = stage,
                ImmutableSamplers = samplers
            };
            layoutCount++;
            layouts.Add(l);
        }

        public void Build(int devId)
        {
            if (!locked)
            {
                if (Layouts.Count == 0)
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
                            (Layouts[i].Type == DescriptorType.UniformBufferDynamic ? 0 : VkDescriptorBindingFlags.DescriptorBindingUpdateAfterBindBit) |
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
                    hndl = setLayoutPtr;

                    if (GraphicsDevice.EnableValidation)
                    {
                        var objName = new VkDebugUtilsObjectNameInfoEXT()
                        {
                            sType = VkStructureType.StructureTypeDebugUtilsObjectNameInfoExt,
                            pObjectName = Name + "_layout",
                            objectType = VkObjectType.ObjectTypeDescriptorSetLayout,
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
