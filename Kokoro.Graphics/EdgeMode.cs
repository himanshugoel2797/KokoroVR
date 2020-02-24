using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public enum EdgeMode
    {
        ClampToEdge = VkSamplerAddressMode.SamplerAddressModeClampToEdge,
        ClampToBorder = VkSamplerAddressMode.SamplerAddressModeClampToBorder,
        Repeat = VkSamplerAddressMode.SamplerAddressModeRepeat
    }
}
