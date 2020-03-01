using System;
using System.Collections.Generic;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class ComputePipeline
    {
        public string Name { get; set; }
        public ShaderSource Shader { get; set; }
        public PipelineLayout PipelineLayout { get; set; }

        internal IntPtr hndl;
        private int devID;
        private bool locked;

        public ComputePipeline()
        {

        }

        public void Build(int deviceIndex)
        {
            if (!locked)
            {
                unsafe
                {
                    var creatInfo = new VkComputePipelineCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeComputePipelineCreateInfo,
                        stage = new VkPipelineShaderStageCreateInfo()
                        {
                            sType = VkStructureType.StructureTypePipelineShaderStageCreateInfo,
                            stage = (VkShaderStageFlags)Shader.ShaderType,
                            module = Shader.ids[deviceIndex],
                            pName = "main"
                        },
                        layout = PipelineLayout.hndl,
                    };

                    IntPtr pipeline_l = IntPtr.Zero;
                    if (vkCreateComputePipelines(GraphicsDevice.GetDeviceInfo(deviceIndex).Device, IntPtr.Zero, 1, creatInfo.Pointer(), null, &pipeline_l) != VkResult.Success)
                        throw new Exception("Failed to create compute pipeline.");
                    hndl = pipeline_l;

                    if (GraphicsDevice.EnableValidation)
                    {
                        var objName = new VkDebugUtilsObjectNameInfoEXT()
                        {
                            sType = VkStructureType.StructureTypeDebugUtilsObjectNameInfoExt,
                            pObjectName = Name,
                            objectType = VkObjectType.ObjectTypePipeline,
                            objectHandle = (ulong)hndl
                        };
                        GraphicsDevice.SetDebugUtilsObjectNameEXT(GraphicsDevice.GetDeviceInfo(deviceIndex).Device, objName.Pointer());
                    }

                    devID = deviceIndex;
                    locked = true;
                }
            }
            else
                throw new Exception("Pipeline is locked.");
        }
    }
}
