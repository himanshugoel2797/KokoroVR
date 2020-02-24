using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public enum BorderColor
    {
        OpaqueFloatBlack = VkBorderColor.BorderColorFloatOpaqueBlack,
        OpaqueFloatWhite = VkBorderColor.BorderColorFloatOpaqueWhite,
        TransparentFloatBlack = VkBorderColor.BorderColorFloatTransparentBlack,
        OpaqueIntBlack = VkBorderColor.BorderColorIntOpaqueBlack,
        OpaqueIntWhite = VkBorderColor.BorderColorIntOpaqueWhite,
        TransparentIntBlack = VkBorderColor.BorderColorIntTransparentBlack,
    }
}
