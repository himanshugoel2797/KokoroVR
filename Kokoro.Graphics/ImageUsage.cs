using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public enum ImageUsage
    {
        None = 0,
        Sampled = VkImageUsageFlags.ImageUsageSampledBit,
        TransferDst = VkImageUsageFlags.ImageUsageTransferDstBit,
        Storage = VkImageUsageFlags.ImageUsageStorageBit,
        Depth = VkImageUsageFlags.ImageUsageDepthStencilAttachmentBit
    }
}
