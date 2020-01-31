#pragma once
#include "GraphicsDevice.h"

namespace Kokoro::Graphics {
	enum class ImageFormat {
		R8G8B8A8Unorm,
		R8G8B8A8Snorm,
		Depth32f,
		Depth16f,
		//TODO: Add formats as needed
	};

	class ImageFormatConv {
	public:
		static VkFormat Convert(ImageFormat s) {
			switch (s) {
			case ImageFormat::R8G8B8A8Unorm:
				return VK_FORMAT_R8G8B8A8_UNORM;
			case ImageFormat::R8G8B8A8Snorm:
				return VK_FORMAT_R8G8B8A8_SNORM;
			case ImageFormat::Depth32f:
				return VK_FORMAT_D32_SFLOAT;
			case ImageFormat::Depth16f:
				return VK_FORMAT_D16_UNORM;
			default:
				return VK_FORMAT_UNDEFINED;
			}
		}
	};
}