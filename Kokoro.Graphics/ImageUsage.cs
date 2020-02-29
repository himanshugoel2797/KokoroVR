using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public enum ImageUsage
    {
        None = 0,
        Sampled = VkImageUsageFlags.ImageUsageSampledBit,
        TransferDst = VkImageUsageFlags.ImageUsageTransferDstBit,
        TransferSrc = VkImageUsageFlags.ImageUsageTransferSrcBit,
        Storage = VkImageUsageFlags.ImageUsageStorageBit,
        DepthAttachment = VkImageUsageFlags.ImageUsageDepthStencilAttachmentBit,
        ColorAttachment = VkImageUsageFlags.ImageUsageColorAttachmentBit,
    }
}
