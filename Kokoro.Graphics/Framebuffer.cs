using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public enum AttachmentKind
    {
        ColorAttachment0,
        ColorAttachment1,
        ColorAttachment2,
        ColorAttachment3,
        ColorAttachment4,
        ColorAttachment5,
        ColorAttachment6,
        ColorAttachment7,
        ColorAttachment8,
        ColorAttachment9,
        DepthAttachment = 1024,
    }
    public class Framebuffer
    {
        public string Name { get; set; }
        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public IDictionary<AttachmentKind, ImageView> Attachments { get; }
        public RenderPass RenderPass { get; set; }
        internal IntPtr hndl;
        private bool locked;

        public Framebuffer(uint w, uint h)
        {
            Attachments = new Dictionary<AttachmentKind, ImageView>();
            Width = w;
            Height = h;
        }

        public ImageView this[AttachmentKind attachment]
        {
            set
            {
                Attachments[attachment] = value;
            }
            get
            {
                return Attachments[attachment];
            }
        }

        public void Build(int deviceIndex)
        {
            if (!locked)
            {
                unsafe
                {
                    //Setup framebuffer
                    uint attachmentCnt = (uint)Attachments.Count;
                    var attachmentIndices = Attachments.Keys.OrderBy(a => a).ToArray();
                    var attachments = stackalloc IntPtr[(int)attachmentCnt];
                    if (Attachments.ContainsKey(AttachmentKind.DepthAttachment))
                        attachments[attachmentCnt - 1] = Attachments[AttachmentKind.DepthAttachment].hndl;
                    for (int i = 0; i < attachmentCnt; i++)
                        attachments[i] = Attachments[attachmentIndices[i]].hndl;

                    var framebufferInfo = new VkFramebufferCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeFramebufferCreateInfo,
                        attachmentCount = attachmentCnt,
                        pAttachments = attachments,
                        renderPass = RenderPass.hndl,
                        width = Width,
                        height = Height,
                        layers = 1
                    };

                    IntPtr framebuffer_l = IntPtr.Zero;
                    if (vkCreateFramebuffer(GraphicsDevice.GetDeviceInfo(deviceIndex).Device, framebufferInfo.Pointer(), null, &framebuffer_l) != VkResult.Success)
                        throw new Exception("Failed to create framebuffer");
                    hndl = framebuffer_l;

                    if (GraphicsDevice.EnableValidation)
                    {
                        var objName = new VkDebugUtilsObjectNameInfoEXT()
                        {
                            sType = VkStructureType.StructureTypeDebugUtilsObjectNameInfoExt,
                            pObjectName = Name,
                            objectType = VkObjectType.ObjectTypeFramebuffer,
                            objectHandle = (ulong)hndl
                        };
                        GraphicsDevice.SetDebugUtilsObjectNameEXT(GraphicsDevice.GetDeviceInfo(deviceIndex).Device, objName.Pointer());
                    }

                    locked = true;
                }
            }
            else
                throw new Exception("Framebuffer is locked.");
        }
    }
}
