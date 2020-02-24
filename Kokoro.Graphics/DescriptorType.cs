using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public enum DescriptorType
    {
        Sampler = VkDescriptorType.DescriptorTypeSampler,
        CombinedImageSampler = VkDescriptorType.DescriptorTypeCombinedImageSampler,
        SampledImage = VkDescriptorType.DescriptorTypeSampledImage,
        StorageImage = VkDescriptorType.DescriptorTypeStorageImage,
        UniformTexelBuffer = VkDescriptorType.DescriptorTypeUniformTexelBuffer,
        StorageTexelBuffer = VkDescriptorType.DescriptorTypeStorageTexelBuffer,
        UniformBuffer = VkDescriptorType.DescriptorTypeUniformBuffer,
        StorageBuffer = VkDescriptorType.DescriptorTypeStorageBuffer,
        InputAttachment = VkDescriptorType.DescriptorTypeInputAttachment
    }
}
