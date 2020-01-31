#include "Sampler.h"

Kokoro::Graphics::Sampler::Sampler()
{

}

Kokoro::Graphics::Sampler::~Sampler()
{
	if (!locked) {
		vkDestroySampler(GraphicsDevice::GetDevice(), sampler, nullptr);
	}
}

void Kokoro::Graphics::Sampler::Build()
{
	if (!locked) {
		VkSamplerCreateInfo creatInfo = {};
		creatInfo.sType = VK_STRUCTURE_TYPE_SAMPLER_CREATE_INFO;
		creatInfo.flags = 0;
		creatInfo.magFilter = LinearFilter ? VK_FILTER_LINEAR : VK_FILTER_NEAREST;
		creatInfo.minFilter = LinearFilter ? VK_FILTER_LINEAR : VK_FILTER_NEAREST;
		creatInfo.mipmapMode = VK_SAMPLER_MIPMAP_MODE_NEAREST;
		creatInfo.addressModeU = EdgeModeConverter::Convert(Edge);
		creatInfo.addressModeV = EdgeModeConverter::Convert(Edge);
		creatInfo.addressModeW = EdgeModeConverter::Convert(Edge);
		creatInfo.mipLodBias = 0;
		creatInfo.anisotropyEnable = AnistropicSamples == 0 ? VK_FALSE : VK_TRUE;
		creatInfo.maxAnisotropy = AnistropicSamples;
		creatInfo.compareEnable = VK_FALSE;
		creatInfo.compareOp = VK_COMPARE_OP_ALWAYS;
		creatInfo.minLod = MinLod;
		creatInfo.maxLod = MaxLod;
		creatInfo.borderColor = BorderColorConverter::Convert(Border);
		creatInfo.unnormalizedCoordinates = UnnormalizedCoords ? VK_TRUE : VK_FALSE;

		pin_ptr<VkSampler> sampler_ptr = &sampler;
		if (vkCreateSampler(GraphicsDevice::GetDevice(), &creatInfo, nullptr, sampler_ptr) != VK_SUCCESS) {
			throw gcnew System::Exception("Failed to create sampler.");
		}
		locked = true;
	}
}

VkSampler Kokoro::Graphics::Sampler::GetSampler() {
	return sampler;
}