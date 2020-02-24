using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public enum ImageLayout
    {
        Undefined = VkImageLayout.ImageLayoutUndefined,
        General = VkImageLayout.ImageLayoutGeneral,
        ColorAttachmentOptimal = VkImageLayout.ImageLayoutColorAttachmentOptimal,
        DepthAttachmentOptimal = VkImageLayout.ImageLayoutDepthStencilAttachmentOptimal,
        DepthReadOnlyOptimal = VkImageLayout.ImageLayoutDepthStencilReadOnlyOptimal,
        ShaderReadOnlyOptimal = VkImageLayout.ImageLayoutShaderReadOnlyOptimal,
        TransferSrcOptimal = VkImageLayout.ImageLayoutTransferSrcOptimal,
        TransferDstOptimal = VkImageLayout.ImageLayoutTransferDstOptimal,
        Preinitialized = VkImageLayout.ImageLayoutPreinitialized,
        PresentSrc = VkImageLayout.ImageLayoutPresentSrcKhr
    }
}
