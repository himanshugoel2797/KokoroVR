using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public enum AttachmentLoadOp
    {
        Clear = VkAttachmentLoadOp.AttachmentLoadOpClear,
        DontCare = VkAttachmentLoadOp.AttachmentLoadOpDontCare,
        Load = VkAttachmentLoadOp.AttachmentLoadOpLoad
    }
    public enum AttachmentStoreOp
    {
        DontCare = VkAttachmentStoreOp.AttachmentStoreOpDontCare,
        Store = VkAttachmentStoreOp.AttachmentStoreOpStore
    }

    public class RenderPassEntry
    {
        public AttachmentLoadOp LoadOp { get; set; }
        public AttachmentStoreOp StoreOp { get; set; }
        public ImageLayout InitialLayout { get; set; }
        public ImageLayout StartLayout { get; set; }
        public ImageLayout FinalLayout { get; set; }
        public ImageFormat Format { get; set; }
    }

    public class RenderPass
    {
        public string Name { get; set; }
        public RenderPassEntry[] ColorAttachments { get; set; }
        public RenderPassEntry DepthAttachment { get; set; }

        internal IntPtr hndl;
        private int devID;
        private bool locked;

        public RenderPass()
        {

        }

        public void Build(int device_index)
        {
            if (!locked)
            {
                unsafe
                {
                    uint colorAttachmentCnt = (uint)(ColorAttachments == null ? 0 : ColorAttachments.Length);
                    uint depthAttachmentCnt = DepthAttachment != null ? 1u : 0u;

                    var colorAttachments = new VkAttachmentReference[colorAttachmentCnt];
                    for (int i = 0; i < colorAttachmentCnt; i++)
                    {
                        colorAttachments[i].attachment = (uint)i;
                        colorAttachments[i].layout = (VkImageLayout)ColorAttachments[i].StartLayout;
                    }
                    var depthAttachment = new VkAttachmentReference()
                    {
                        attachment = colorAttachmentCnt,
                        layout = DepthAttachment == null ? VkImageLayout.ImageLayoutUndefined : (VkImageLayout)DepthAttachment.StartLayout
                    };

                    var colorAttachments_ptr = colorAttachments.Pointer();
                    var depthAttachments_ptr = depthAttachment.Pointer();
                    var preserveAttachments = stackalloc uint[] { colorAttachmentCnt };

                    var subpassDesc = new VkSubpassDescription()
                    {
                        pipelineBindPoint = VkPipelineBindPoint.PipelineBindPointGraphics,
                        colorAttachmentCount = colorAttachmentCnt,
                        pColorAttachments = colorAttachments_ptr,
                        pDepthStencilAttachment = depthAttachmentCnt > 0 ? depthAttachments_ptr : IntPtr.Zero,
                        preserveAttachmentCount = 0u,
                        pPreserveAttachments = preserveAttachments,
                    };
                    var subpassDesc_ptr = subpassDesc.Pointer();

                    var colorAttachmentDesc = new VkAttachmentDescription[colorAttachmentCnt + depthAttachmentCnt];
                    for (int i = 0; i < colorAttachmentCnt; i++)
                    {
                        colorAttachmentDesc[i].format = (VkFormat)ColorAttachments[i].Format;
                        colorAttachmentDesc[i].samples = VkSampleCountFlags.SampleCount1Bit;
                        colorAttachmentDesc[i].loadOp = (VkAttachmentLoadOp)ColorAttachments[i].LoadOp;
                        colorAttachmentDesc[i].storeOp = (VkAttachmentStoreOp)ColorAttachments[i].StoreOp;
                        colorAttachmentDesc[i].stencilLoadOp = VkAttachmentLoadOp.AttachmentLoadOpDontCare;
                        colorAttachmentDesc[i].stencilStoreOp = VkAttachmentStoreOp.AttachmentStoreOpDontCare;
                        colorAttachmentDesc[i].initialLayout = (VkImageLayout)ColorAttachments[i].InitialLayout;
                        colorAttachmentDesc[i].finalLayout = (VkImageLayout)ColorAttachments[i].FinalLayout;
                    }
                    if (depthAttachmentCnt > 0)
                    {
                        colorAttachmentDesc[colorAttachmentCnt].format = (VkFormat)DepthAttachment.Format;
                        colorAttachmentDesc[colorAttachmentCnt].samples = VkSampleCountFlags.SampleCount1Bit;
                        colorAttachmentDesc[colorAttachmentCnt].loadOp = (VkAttachmentLoadOp)DepthAttachment.LoadOp;
                        colorAttachmentDesc[colorAttachmentCnt].storeOp = (VkAttachmentStoreOp)DepthAttachment.StoreOp;
                        colorAttachmentDesc[colorAttachmentCnt].stencilLoadOp = VkAttachmentLoadOp.AttachmentLoadOpDontCare;
                        colorAttachmentDesc[colorAttachmentCnt].stencilStoreOp = VkAttachmentStoreOp.AttachmentStoreOpDontCare;
                        colorAttachmentDesc[colorAttachmentCnt].initialLayout = (VkImageLayout)DepthAttachment.InitialLayout;
                        colorAttachmentDesc[colorAttachmentCnt].finalLayout = (VkImageLayout)DepthAttachment.FinalLayout;
                    }
                    var colorAttachmentDesc_ptr = colorAttachmentDesc.Pointer();

                    var subpassDependency = new VkSubpassDependency()
                    {
                        srcSubpass = VkSubpassExternal,
                        dstSubpass = 0,
                        srcStageMask = VkPipelineStageFlags.PipelineStageAllCommandsBit,
                        srcAccessMask = 0,
                        dstStageMask = VkPipelineStageFlags.PipelineStageVertexShaderBit,
                        dstAccessMask = VkAccessFlags.AccessMemoryReadBit,
                    };
                    var subpassDependency_ptr = subpassDependency.Pointer();

                    var renderPassInfo = new VkRenderPassCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeRenderPassCreateInfo,
                        attachmentCount = colorAttachmentCnt + depthAttachmentCnt,
                        pAttachments = colorAttachmentDesc_ptr,
                        subpassCount = 1,
                        pSubpasses = subpassDesc_ptr,
                        dependencyCount = 1,
                        pDependencies = subpassDependency_ptr

                    };
                    IntPtr renderPass_l = IntPtr.Zero;
                    if (vkCreateRenderPass(GraphicsDevice.GetDeviceInfo(device_index).Device, renderPassInfo.Pointer(), null, &renderPass_l) != VkResult.Success)
                        throw new Exception("Failed to create RenderPass.");
                    hndl = renderPass_l;
                    devID = device_index;

                    if (GraphicsDevice.EnableValidation)
                    {
                        var objName = new VkDebugUtilsObjectNameInfoEXT()
                        {
                            sType = VkStructureType.StructureTypeDebugUtilsObjectNameInfoExt,
                            pObjectName = Name,
                            objectType = VkObjectType.ObjectTypeRenderPass,
                            objectHandle = (ulong)hndl
                        };
                        GraphicsDevice.SetDebugUtilsObjectNameEXT(GraphicsDevice.GetDeviceInfo(devID).Device, objName.Pointer());
                    }
                }
                locked = true;
            }
            else
                throw new Exception("RenderPass is locked.");
        }
    }
}
