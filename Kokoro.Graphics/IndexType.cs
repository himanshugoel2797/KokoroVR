using System;
using System.Collections.Generic;
using System.Text;

namespace Kokoro.Graphics
{
    public enum IndexType
    {
        U16 = VulkanSharp.Raw.Vk.VkIndexType.IndexTypeUint16,
        U32 = VulkanSharp.Raw.Vk.VkIndexType.IndexTypeUint32
    }
}
