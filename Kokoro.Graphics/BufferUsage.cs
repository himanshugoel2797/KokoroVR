using System;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    [Flags]
    public enum BufferUsage
    {
        Index = VkBufferUsageFlags.BufferUsageIndexBufferBit,
        Uniform = VkBufferUsageFlags.BufferUsageUniformBufferBit,
        Storage = VkBufferUsageFlags.BufferUsageStorageBufferBit,
        Indirect = VkBufferUsageFlags.BufferUsageIndirectBufferBit,
        TransferSrc = VkBufferUsageFlags.BufferUsageTransferSrcBit,
        TransferDst = VkBufferUsageFlags.BufferUsageTransferDstBit,
        UniformTexel = VkBufferUsageFlags.BufferUsageUniformTexelBufferBit,
        StorageTexel = VkBufferUsageFlags.BufferUsageStorageTexelBufferBit,
    }
}
