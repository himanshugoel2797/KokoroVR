using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public enum PrimitiveType
    {
        Triangle = VkPrimitiveTopology.PrimitiveTopologyTriangleList,
        TriangleStrip = VkPrimitiveTopology.PrimitiveTopologyTriangleStrip,
        Line = VkPrimitiveTopology.PrimitiveTopologyLineList,
        LineStrip = VkPrimitiveTopology.PrimitiveTopologyLineStrip,
        Point = VkPrimitiveTopology.PrimitiveTopologyPointList,
    }

    public enum CullMode
    {
        None = VkCullModeFlags.CullModeNone,
        Front = VkCullModeFlags.CullModeFrontBit,
        Back = VkCullModeFlags.CullModeBackBit
    }

    public enum DepthTest
    {
        Greater = VkCompareOp.CompareOpGreater,
        GreaterEqual = VkCompareOp.CompareOpGreaterOrEqual,
        Less = VkCompareOp.CompareOpLess,
        LessEqual = VkCompareOp.CompareOpLessOrEqual,
        Equal = VkCompareOp.CompareOpEqual,
        NotEqual = VkCompareOp.CompareOpNotEqual,
        Always = VkCompareOp.CompareOpAlways
    }

    public class PipelineLayout : IDisposable
    {
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
        public DescriptorSet[] Descriptors { get; set; }

        internal IntPtr pipelineLayout;
        internal IntPtr pipeline;
        internal IntPtr framebuffer;
        private int devID;
        private bool locked;

        public PipelineLayout()
        {
            Shaders = new List<ShaderSource>();
        }

        public void Rebuild()
        {
            if (locked)
            {
                Dispose(false);
                locked = false;
                Build(devID);
            }
            else
                throw new Exception("PipelineLayout has not been built.");
        }

        public void Build(int deviceIndex)
        {
            if (!locked)
            {
                unsafe
                {
                    devID = deviceIndex;

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

                    var descLayouts = stackalloc IntPtr[Descriptors == null ? 0 : Descriptors.Length];
                    if (Descriptors != null) 
                        for (int i = 0; i < Descriptors.Length; i++) descLayouts[i] = Descriptors[i].descSetLayout;
                    var pipelineLayoutInfo = new VkPipelineLayoutCreateInfo()
                    {
                        sType = VkStructureType.StructureTypePipelineLayoutCreateInfo,
                        setLayoutCount = Descriptors == null ? 0 : (uint)Descriptors.Length, //TODO: add descriptor support
                        pSetLayouts = descLayouts,
                        pushConstantRangeCount = 0, //TODO: setup push constants
                        pPushConstantRanges = IntPtr.Zero,
                    };

                    IntPtr pipelineLayout_l = IntPtr.Zero;
                    if (vkCreatePipelineLayout(GraphicsDevice.GetDeviceInfo(deviceIndex).Device, pipelineLayoutInfo.Pointer(), null, &pipelineLayout_l) != VkResult.Success)
                        throw new Exception("Failed to create pipeline layout.");
                    pipelineLayout = pipelineLayout_l;

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
                        layout = pipelineLayout_l,
                        renderPass = RenderPass.renderPass,
                        subpass = 0,
                        basePipelineHandle = IntPtr.Zero,
                        basePipelineIndex = -1,
                    };

                    IntPtr pipeline_l = IntPtr.Zero;
                    if (vkCreateGraphicsPipelines(GraphicsDevice.GetDeviceInfo(deviceIndex).Device, IntPtr.Zero, 1, pipelineInfo.Pointer(), null, &pipeline_l) != VkResult.Success)
                        throw new Exception("Failed to create graphics pipeline.");
                    pipeline = pipeline_l;

                    //Setup framebuffer
                    uint attachmentCnt = (uint)Framebuffer.Attachments.Count;
                    var attachmentIndices = Framebuffer.Attachments.Keys.OrderBy(a => a).ToArray();
                    var attachments = stackalloc IntPtr[(int)attachmentCnt];
                    if (Framebuffer.Attachments.ContainsKey(AttachmentKind.DepthAttachment))
                        attachments[attachmentCnt - 1] = Framebuffer.Attachments[AttachmentKind.DepthAttachment].viewPtr;
                    for (int i = 0; i < attachmentCnt; i++)
                        attachments[i] = Framebuffer.Attachments[attachmentIndices[i]].viewPtr;

                    var framebufferInfo = new VkFramebufferCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeFramebufferCreateInfo,
                        attachmentCount = attachmentCnt,
                        pAttachments = attachments,
                        renderPass = RenderPass.renderPass,
                        width = Framebuffer.Width,
                        height = Framebuffer.Height,
                        layers = 1
                    };

                    IntPtr framebuffer_l = IntPtr.Zero;
                    if (vkCreateFramebuffer(GraphicsDevice.GetDeviceInfo(deviceIndex).Device, framebufferInfo.Pointer(), null, &framebuffer_l) != VkResult.Success)
                        throw new Exception("Failed to create framebuffer");
                    framebuffer = framebuffer_l;
                }
                locked = true;
            }
            else
                throw new Exception("PipelineLayout is locked.");
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
                {
                    var device = GraphicsDevice.GetDeviceInfo(devID).Device;
                    vkDestroyFramebuffer(device, framebuffer, null);
                    vkDestroyPipeline(device, pipeline, null);
                    vkDestroyPipelineLayout(device, pipelineLayout, null);
                }

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~PipelineLayout()
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
