using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class Framebuffer
    {
        public string Name { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public ImageView[] ColorAttachments { get; set; }
        public ImageView DepthAttachment { get; set; }
        public RenderPass RenderPass { get; set; }
        internal IntPtr hndl;
        private bool locked;

        public Framebuffer()
        {
        }

        public void Build(int deviceIndex)
        {
            if (!locked)
            {
                unsafe
                {
                    //Setup framebuffer
                    var attachmentCnt = (ColorAttachments == null ? 0 : ColorAttachments.Length) + (DepthAttachment == null ? 0 : 1);
                    var attachments = stackalloc IntPtr[attachmentCnt];
                    if (ColorAttachments != null)
                        for (int i = 0; i < ColorAttachments.Length; i++)
                        {
                            attachments[i] = ColorAttachments[i].hndl;
                            if (ColorAttachments[i].Width != Width)
                                throw new Exception();
                            if (ColorAttachments[i].Height != Height)
                                throw new Exception();
                        }
                    if (DepthAttachment != null)
                        attachments[attachmentCnt - 1] = DepthAttachment.hndl;

                    var framebufferInfo = new VkFramebufferCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeFramebufferCreateInfo,
                        attachmentCount = (uint)attachmentCnt,
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
