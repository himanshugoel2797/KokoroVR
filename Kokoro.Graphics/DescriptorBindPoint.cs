using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public enum DescriptorBindPoint
    {
        Graphics = VkPipelineBindPoint.PipelineBindPointGraphics,
        Compute = VkPipelineBindPoint.PipelineBindPointCompute,
    }
}
