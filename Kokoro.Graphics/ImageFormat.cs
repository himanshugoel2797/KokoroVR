using System;
using System.Collections.Generic;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public enum ImageFormat
    {
        R8G8B8A8Unorm = VkFormat.FormatR8g8b8a8Unorm,
        R8G8B8A8Snorm = VkFormat.FormatR8g8b8a8Snorm,
        B8G8R8A8Unorm = VkFormat.FormatB8g8r8a8Unorm,
        B8G8R8A8Snorm = VkFormat.FormatB8g8r8a8Snorm,
        Depth32f = VkFormat.FormatD32Sfloat,
        Depth16f = VkFormat.FormatD16Unorm,
    }
}
