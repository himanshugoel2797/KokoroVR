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
        DoneCare = VkAttachmentLoadOp.AttachmentLoadOpDontCare,
        Load = VkAttachmentLoadOp.AttachmentLoadOpLoad
    }
    public enum AttachmentStoreOp
    {
        DoneCare = VkAttachmentStoreOp.AttachmentStoreOpDontCare,
        Store = VkAttachmentStoreOp.AttachmentStoreOpStore
    }

    public class RenderPass
    {
        public IDictionary<AttachmentKind, AttachmentLoadOp> LoadOp { get; private set; }
        public IDictionary<AttachmentKind, AttachmentStoreOp> StoreOp { get; private set; }
        public IDictionary<AttachmentKind, ImageLayout> InitialLayout { get; private set; }
        public IDictionary<AttachmentKind, ImageLayout> StartLayout { get; private set; }
        public IDictionary<AttachmentKind, ImageLayout> FinalLayout { get; private set; }
        public IDictionary<AttachmentKind, ImageFormat> Formats { get; private set; }

        internal IntPtr renderPass;
        private int devID;
        private bool locked;

        public RenderPass()
        {
            LoadOp = new Dictionary<AttachmentKind, AttachmentLoadOp>();
            StoreOp = new Dictionary<AttachmentKind, AttachmentStoreOp>();
            InitialLayout = new Dictionary<AttachmentKind, ImageLayout>();
            StartLayout = new Dictionary<AttachmentKind, ImageLayout>();
            FinalLayout = new Dictionary<AttachmentKind, ImageLayout>();
            Formats = new Dictionary<AttachmentKind, ImageFormat>();
        }

        public void Build(int device_index)
        {
            if (!locked)
            {
                unsafe
                {
                    var colorAttachment_indices = InitialLayout.Keys.Where(a => a != AttachmentKind.DepthAttachment).ToArray();
                    uint colorAttachmentCnt = (uint)colorAttachment_indices.Length;
                    uint depthAttachmentCnt = InitialLayout.ContainsKey(AttachmentKind.DepthAttachment) ? 1u : 0u;

                    var colorAttachments = new VkAttachmentReference[colorAttachmentCnt + 1];
                    for (int i = 0; i < colorAttachmentCnt; i++)
                    {
                        colorAttachments[i].attachment = (uint)colorAttachment_indices[i] + 1;
                        colorAttachments[i].layout = (VkImageLayout)StartLayout[colorAttachment_indices[i]];
                    }
                    colorAttachments[colorAttachments.Length - 1] = new VkAttachmentReference()
                    {
                        attachment = 0,
                        layout = InitialLayout.ContainsKey(AttachmentKind.DepthAttachment) ? (VkImageLayout)StartLayout[AttachmentKind.DepthAttachment] : VkImageLayout.ImageLayoutGeneral
                    };

                    var colorAttachments_ptr = colorAttachments.Pointer();
                    var preserveAttachments = stackalloc uint[] { 0 };

                    var subpassDesc = new VkSubpassDescription()
                    {
                        pipelineBindPoint = VkPipelineBindPoint.PipelineBindPointGraphics,
                        colorAttachmentCount = colorAttachmentCnt,
                        pColorAttachments = colorAttachments_ptr,
                        pDepthStencilAttachment = depthAttachmentCnt > 0 ? (IntPtr)((ulong)colorAttachments_ptr.Pointer + colorAttachmentCnt * (ulong)Marshal.SizeOf<VkAttachmentReference>()) : IntPtr.Zero,
                        preserveAttachmentCount = depthAttachmentCnt > 0 ? 0u : 1u,
                        pPreserveAttachments = preserveAttachments
                    };
                    var subpassDesc_ptr = subpassDesc.Pointer();

                    var colorAttachmentDesc = new VkAttachmentDescription[colorAttachmentCnt + 1];
                    for (int i = 1; i < colorAttachmentCnt + 1; i++)
                    {
                        colorAttachmentDesc[i].format = (VkFormat)Formats[colorAttachment_indices[i - 1]];
                        colorAttachmentDesc[i].samples = VkSampleCountFlags.SampleCount1Bit;
                        colorAttachmentDesc[i].loadOp = (VkAttachmentLoadOp)LoadOp[colorAttachment_indices[i - 1]];
                        colorAttachmentDesc[i].storeOp = (VkAttachmentStoreOp)StoreOp[colorAttachment_indices[i - 1]];
                        colorAttachmentDesc[i].stencilLoadOp = VkAttachmentLoadOp.AttachmentLoadOpDontCare;
                        colorAttachmentDesc[i].stencilStoreOp = VkAttachmentStoreOp.AttachmentStoreOpDontCare;
                        colorAttachmentDesc[i].initialLayout = (VkImageLayout)InitialLayout[colorAttachment_indices[i - 1]];
                        colorAttachmentDesc[i].finalLayout = (VkImageLayout)FinalLayout[colorAttachment_indices[i - 1]];
                    }
                    if (depthAttachmentCnt > 0)
                    {
                        colorAttachmentDesc[0].format = (VkFormat)Formats[AttachmentKind.DepthAttachment];
                        colorAttachmentDesc[0].samples = VkSampleCountFlags.SampleCount1Bit;
                        colorAttachmentDesc[0].loadOp = (VkAttachmentLoadOp)LoadOp[AttachmentKind.DepthAttachment];
                        colorAttachmentDesc[0].storeOp = (VkAttachmentStoreOp)StoreOp[AttachmentKind.DepthAttachment];
                        colorAttachmentDesc[0].stencilLoadOp = VkAttachmentLoadOp.AttachmentLoadOpDontCare;
                        colorAttachmentDesc[0].stencilStoreOp = VkAttachmentStoreOp.AttachmentStoreOpDontCare;
                        colorAttachmentDesc[0].initialLayout = (VkImageLayout)InitialLayout[AttachmentKind.DepthAttachment];
                        colorAttachmentDesc[0].finalLayout = (VkImageLayout)FinalLayout[AttachmentKind.DepthAttachment];
                    }
                    else
                    {
                        colorAttachmentDesc[0].format = VkFormat.FormatD32Sfloat;
                        colorAttachmentDesc[0].samples = VkSampleCountFlags.SampleCount1Bit;
                        colorAttachmentDesc[0].loadOp = VkAttachmentLoadOp.AttachmentLoadOpDontCare;
                        colorAttachmentDesc[0].storeOp = VkAttachmentStoreOp.AttachmentStoreOpDontCare;
                        colorAttachmentDesc[0].stencilLoadOp = VkAttachmentLoadOp.AttachmentLoadOpDontCare;
                        colorAttachmentDesc[0].stencilStoreOp = VkAttachmentStoreOp.AttachmentStoreOpDontCare;
                        colorAttachmentDesc[0].initialLayout = VkImageLayout.ImageLayoutGeneral;
                        colorAttachmentDesc[0].finalLayout = VkImageLayout.ImageLayoutDepthStencilAttachmentOptimal;
                    }
                    var colorAttachmentDesc_ptr = colorAttachmentDesc.Pointer();

                    var renderPassInfo = new VkRenderPassCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeRenderPassCreateInfo,
                        attachmentCount = colorAttachmentCnt + 1,
                        pAttachments = colorAttachmentDesc_ptr,
                        subpassCount = 1,
                        pSubpasses = subpassDesc_ptr
                    };
                    IntPtr renderPass_l = IntPtr.Zero;
                    if (vkCreateRenderPass(GraphicsDevice.GetDeviceInfo(device_index).Device, renderPassInfo.Pointer(), null, &renderPass_l) != VkResult.Success)
                        throw new Exception("Failed to create RenderPass.");
                    renderPass = renderPass_l;
                    devID = device_index;
                }
                locked = true;
            }
            else
                throw new Exception("RenderPass is locked.");
        }
    }
}
