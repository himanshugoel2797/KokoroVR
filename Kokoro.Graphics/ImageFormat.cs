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
        R8G8B8A8UInt = VkFormat.FormatR8g8b8a8Uint,
        Depth32f = VkFormat.FormatD32Sfloat,
        Depth16f = VkFormat.FormatD16Unorm,
        R32f = VkFormat.FormatR32Sfloat,
        R32UInt = VkFormat.FormatR32Uint,
        Rg32f = VkFormat.FormatR32g32Sfloat,
        Rgba32f = VkFormat.FormatR32g32b32a32Sfloat,
    }
}
