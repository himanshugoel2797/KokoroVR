using System;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    [Flags]
    public enum AccessFlags
    {
        None = 0,
        IndirectCommandRead = VkAccessFlags.AccessIndirectCommandReadBit,
        IndexRead = VkAccessFlags.AccessIndexReadBit,
        UniformRead = VkAccessFlags.AccessUniformReadBit,
        ShaderRead = VkAccessFlags.AccessShaderReadBit,
        ShaderWrite = VkAccessFlags.AccessShaderWriteBit,
        ColorAttachmentWrite = VkAccessFlags.AccessColorAttachmentWriteBit,
        DepthAttachmentWrite = VkAccessFlags.AccessDepthStencilAttachmentWriteBit,
        TransferRead = VkAccessFlags.AccessTransferReadBit,
        TransferWrite = VkAccessFlags.AccessTransferWriteBit,
        HostRead = VkAccessFlags.AccessHostReadBit,
        HostWrite = VkAccessFlags.AccessHostWriteBit,
        MemoryRead = VkAccessFlags.AccessMemoryReadBit,
        MemoryWrite = VkAccessFlags.AccessMemoryWriteBit
    }
}
