#pragma once
#include "GraphicsDevice.h"

namespace Kokoro::Graphics {
	enum class EdgeMode {
		ClampToEdge,
		ClampToBorder,
		Repeat
	};

	class EdgeModeConverter {
	public:
		static VkSamplerAddressMode Convert(EdgeMode s) {
			switch (s) {
			case EdgeMode::ClampToEdge:
				return VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE;
			case EdgeMode::ClampToBorder:
				return VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_BORDER;
			case EdgeMode::Repeat:
				return VK_SAMPLER_ADDRESS_MODE_REPEAT;
			default:
				return (VkSamplerAddressMode)0;
			}
		}
	};

	enum class BorderColor {
		OpaqueFloatBlack,
		OpaqueFloatWhite,
		TransparentFloatBlack,
		OpaqueIntBlack,
		OpaqueIntWhite,
		TransparentIntBlack,
	};

	class BorderColorConverter {
	public:
		static VkBorderColor Convert(BorderColor s) {
			switch (s) {
			case BorderColor::OpaqueFloatBlack:
				return VK_BORDER_COLOR_FLOAT_OPAQUE_BLACK;
			case BorderColor::OpaqueFloatWhite:
				return VK_BORDER_COLOR_FLOAT_OPAQUE_WHITE;
			case BorderColor::TransparentFloatBlack:
				return VK_BORDER_COLOR_FLOAT_TRANSPARENT_BLACK;
			case BorderColor::OpaqueIntBlack:
				return VK_BORDER_COLOR_INT_OPAQUE_BLACK;
			case BorderColor::OpaqueIntWhite:
				return VK_BORDER_COLOR_INT_OPAQUE_WHITE;
			case BorderColor::TransparentIntBlack:
				return VK_BORDER_COLOR_INT_TRANSPARENT_BLACK;
			default:
				return (VkBorderColor)0;
			}
		}
	};

	ref class Sampler
	{
	private:
		VkSampler sampler;
		bool locked;
	internal:
		VkSampler GetSampler();
	public:
		property bool UnnormalizedCoords;
		property bool LinearFilter;
		property EdgeMode Edge;
		property BorderColor Border;
		property float AnistropicSamples;
		property int MinLod;
		property int MaxLod;

		Sampler();
		~Sampler();
		void Build();
	};
}
