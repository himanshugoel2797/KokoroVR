#pragma once
#include "GraphicsDevice.h"

#include "ImageFormat.h"

namespace Kokoro::Graphics {
	enum class ImageUsage {
		None = 0,
		Sampled = (1 << 0),
		TransferDst = (1 << 1),
		Storage = (1 << 2)
	};
	inline ImageUsage operator |(ImageUsage lhs, ImageUsage rhs)
	{
		return static_cast<ImageUsage>(static_cast<char>(lhs) | static_cast<char>(rhs));
	}
	inline ImageUsage& operator |= (ImageUsage& lhs, ImageUsage rhs)
	{
		lhs = lhs | rhs;
		return lhs;
	}
	inline ImageUsage operator &(ImageUsage lhs, ImageUsage rhs)
	{
		return static_cast<ImageUsage>(static_cast<char>(lhs) & static_cast<char>(rhs));
	}
	inline ImageUsage& operator &= (ImageUsage& lhs, ImageUsage rhs)
	{
		lhs = lhs & rhs;
		return lhs;
	}

	class ImageUsageConverter {
	public:
		static VkImageUsageFlags Convert(ImageUsage s) {
			uint32_t f = 0;
			if ((s & ImageUsage::Sampled) != ImageUsage::None)
				f |= VK_IMAGE_USAGE_SAMPLED_BIT;
			if ((s & ImageUsage::TransferDst) != ImageUsage::None)
				f |= VK_IMAGE_USAGE_TRANSFER_DST_BIT;
			if ((s & ImageUsage::Storage) != ImageUsage::None)
				f |= VK_IMAGE_USAGE_STORAGE_BIT;
			return (VkImageUsageFlags)f;
		}
	};

	ref class Image
	{
	private:
		VkImage img;
		WVmaAllocation img_alloc;
		bool locked;
	internal:
		VkImage GetImage();
	public:
		property int Width;
		property int Height;
		property int Depth;
		property int Levels;
		property int Layers;
		property int Dimensions;
		property ImageFormat Format;
		property ImageUsage Usage;
		property bool Cubemappable;

		Image();
		~Image();
		void Build();
	};
}

