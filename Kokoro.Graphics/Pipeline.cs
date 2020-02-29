using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class Pipeline
    {
        public string Name { get; set; }
        public List<ShaderSource> Shaders { get; }
        public PrimitiveType Topology { get; set; } = PrimitiveType.Triangle;
        public bool DepthClamp { get; set; }
        public bool RasterizerDiscard { get; set; } = false;
        public float LineWidth { get; set; } = 1.0f;
        public CullMode CullMode { get; set; } = CullMode.None;
        public bool EnableBlending { get; set; }
        public DepthTest DepthTest { get; set; } = DepthTest.Greater;
        public RenderPass RenderPass { get; set; }
        public Framebuffer Framebuffer { get; set; }
        public PipelineLayout PipelineLayout { get; set; }

        internal IntPtr hndl;
        private int devID;
        private bool locked;

        public Pipeline() {
            Shaders = new List<ShaderSource>();
        }

        public void Build(int deviceIndex)
        {
            if (!locked)
            {
                unsafe
                {
                    //create pipeline shader stages
                    var shaderStages = new VkPipelineShaderStageCreateInfo[Shaders.Count];
                    for (int i = 0; i < shaderStages.Length; i++)
                    {
                        shaderStages[i].sType = VkStructureType.StructureTypePipelineShaderStageCreateInfo;
                        shaderStages[i].stage = (VkShaderStageFlags)Shaders[i].ShaderType;
                        shaderStages[i].module = Shaders[i].ids[deviceIndex];
                        shaderStages[i].pName = "main";
                    }
                    var shaderStages_ptr = shaderStages.Pointer();

                    //VkPipelineVertexInputStateCreateInfo - dummy, engine doesn't support fix function vertex input
                    var vertexInputInfo = new VkPipelineVertexInputStateCreateInfo()
                    {
                        sType = VkStructureType.StructureTypePipelineVertexInputStateCreateInfo,
                        vertexBindingDescriptionCount = 0,
                        vertexAttributeDescriptionCount = 0,
                    };
                    var vertexInputInfo_ptr = vertexInputInfo.Pointer();

                    //VkPipelineInputAssemblyStateCreateInfo - state
                    var inputAssembly = new VkPipelineInputAssemblyStateCreateInfo()
                    {
                        sType = VkStructureType.StructureTypePipelineInputAssemblyStateCreateInfo,
                        topology = (VkPrimitiveTopology)Topology,
                        primitiveRestartEnable = false
                    };
                    var inputAssembly_ptr = inputAssembly.Pointer();

                    //VkPipelineViewportStateCreateInfo - dynamic state, no scissor
                    var viewport = new VkViewport()
                    {
                        x = 0,
                        y = 0,
                        width = Framebuffer.Width,
                        height = Framebuffer.Height,
                        minDepth = 0,
                        maxDepth = 1,
                    };

                    var scissor = new VkRect2D()
                    {
                        offset = new VkOffset2D()
                        {
                            x = 0,
                            y = 0
                        },
                        extent = new VkExtent2D()
                        {
                            width = Framebuffer.Width,
                            height = Framebuffer.Height
                        }
                    };

                    var viewport_ptr = viewport.Pointer();
                    var scissor_ptr = scissor.Pointer();
                    var viewportCreateInfo = new VkPipelineViewportStateCreateInfo()
                    {
                        sType = VkStructureType.StructureTypePipelineViewportStateCreateInfo,
                        viewportCount = 1,
                        pViewports = viewport_ptr,
                        scissorCount = 1,
                        pScissors = scissor_ptr
                    };
                    var viewportCreateInfo_ptr = viewportCreateInfo.Pointer();

                    //VkPipelineRasterizationStateCreateInfo - state
                    var rasterizer = new VkPipelineRasterizationStateCreateInfo()
                    {
                        sType = VkStructureType.StructureTypePipelineRasterizationStateCreateInfo,
                        depthClampEnable = DepthClamp,
                        rasterizerDiscardEnable = RasterizerDiscard,
                        polygonMode = VkPolygonMode.PolygonModeFill,
                        lineWidth = LineWidth,
                        cullMode = (VkCullModeFlags)CullMode,
                        frontFace = VkFrontFace.FrontFaceCounterClockwise,   //OpenGL default
                        depthBiasEnable = false,
                        depthBiasConstantFactor = 0,
                        depthBiasClamp = 0,
                        depthBiasSlopeFactor = 0
                    };
                    var rasterizer_ptr = rasterizer.Pointer();

                    //VkPipelineMultisampleStateCreateInfo - don't support for now
                    var multisampling = new VkPipelineMultisampleStateCreateInfo()
                    {
                        sType = VkStructureType.StructureTypePipelineMultisampleStateCreateInfo,
                        sampleShadingEnable = false,
                        rasterizationSamples = VkSampleCountFlags.SampleCount1Bit,
                        minSampleShading = 1,
                        pSampleMask = null,
                        alphaToCoverageEnable = false,
                        alphaToOneEnable = false
                    };
                    var multisampling_ptr = multisampling.Pointer();

                    //VkPipelineDepthStencilStateCreateInfo - state
                    var depthStencil = new VkPipelineDepthStencilStateCreateInfo()
                    {
                        sType = VkStructureType.StructureTypePipelineDepthStencilStateCreateInfo,
                        depthTestEnable = (DepthTest == DepthTest.Always) ? false : true,
                        depthCompareOp = (VkCompareOp)DepthTest
                    };
                    var depthStencil_ptr = depthStencil.Pointer();


                    //VkPipelineColorBlendStateCreateInfo - state
                    var colorBlendStates = new VkPipelineColorBlendAttachmentState[RenderPass.InitialLayout.Keys.Count(a => a != AttachmentKind.DepthAttachment)];
                    for (int i = 0; i < colorBlendStates.Length; i++)
                    {
                        colorBlendStates[i] = new VkPipelineColorBlendAttachmentState()
                        {
                            colorWriteMask = VkColorComponentFlags.ColorComponentRBit | VkColorComponentFlags.ColorComponentGBit | VkColorComponentFlags.ColorComponentBBit | VkColorComponentFlags.ColorComponentABit,
                            blendEnable = false,
                        };
                    }
                    var colorBlendStates_ptr = colorBlendStates.Pointer();

                    var colorBlend = new VkPipelineColorBlendStateCreateInfo()
                    {
                        sType = VkStructureType.StructureTypePipelineColorBlendStateCreateInfo,
                        logicOpEnable = false,
                        logicOp = VkLogicOp.LogicOpCopy,
                        attachmentCount = (uint)colorBlendStates.Length,
                        pAttachments = colorBlendStates_ptr,
                    };
                    colorBlend.blendConstants[0] = 0;
                    colorBlend.blendConstants[1] = 0;
                    colorBlend.blendConstants[2] = 0;
                    colorBlend.blendConstants[3] = 0;
                    var colorBlend_ptr = colorBlend.Pointer();

                    var dynamicStates = stackalloc VkDynamicState[]
                    {
                        VkDynamicState.DynamicStateViewport
                    };

                    var dynamicState = new VkPipelineDynamicStateCreateInfo()
                    {
                        sType = VkStructureType.StructureTypePipelineDynamicStateCreateInfo,
                        dynamicStateCount = 1,
                        pDynamicStates = dynamicStates
                    };
                    var dynamicState_ptr = dynamicState.Pointer();

                    //Setup graphics pipeline
                    var pipelineInfo = new VkGraphicsPipelineCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeGraphicsPipelineCreateInfo,
                        stageCount = (uint)shaderStages.Length,
                        pStages = shaderStages_ptr,
                        pVertexInputState = vertexInputInfo_ptr,
                        pInputAssemblyState = inputAssembly_ptr,
                        pViewportState = viewportCreateInfo_ptr,
                        pRasterizationState = rasterizer_ptr,
                        pMultisampleState = multisampling_ptr,
                        pDepthStencilState = depthStencil_ptr,
                        pColorBlendState = colorBlend_ptr,
                        pDynamicState = dynamicState_ptr,
                        layout = PipelineLayout.hndl,
                        renderPass = RenderPass.hndl,
                        subpass = 0,
                        basePipelineHandle = IntPtr.Zero,
                        basePipelineIndex = -1,
                    };

                    IntPtr pipeline_l = IntPtr.Zero;
                    if (vkCreateGraphicsPipelines(GraphicsDevice.GetDeviceInfo(deviceIndex).Device, IntPtr.Zero, 1, pipelineInfo.Pointer(), null, &pipeline_l) != VkResult.Success)
                        throw new Exception("Failed to create graphics pipeline.");
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
