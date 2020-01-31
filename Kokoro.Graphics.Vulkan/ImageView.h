#pragma once
#include "GraphicsDevice.h"
#include "ImageFormat.h"
#include "Image.h"

namespace Kokoro::Graphics {
	enum class ImageViewType {
		View1D,
		View1DArray,
		View2D,
		View2DArray,
		View3D,
		ViewCube,
		ViewCubeArray,
	};

	class ImageViewTypeConverter {
	public:
		static VkImageViewType Convert(ImageViewType s) {
			switch (s) {
			case ImageViewType::View1D:
				return VK_IMAGE_VIEW_TYPE_1D;
			case ImageViewType::View1DArray:
				return VK_IMAGE_VIEW_TYPE_1D_ARRAY;
			case ImageViewType::View2D:
				return VK_IMAGE_VIEW_TYPE_2D;
			case ImageViewType::View2DArray:
				return VK_IMAGE_VIEW_TYPE_2D_ARRAY;
			case ImageViewType::View3D:
				return VK_IMAGE_VIEW_TYPE_3D;
			case ImageViewType::ViewCube:
				return VK_IMAGE_VIEW_TYPE_CUBE;
			case ImageViewType::ViewCubeArray:
				return VK_IMAGE_VIEW_TYPE_CUBE_ARRAY;
			default:
				return (VkImageViewType)0;
			}
		}
	};

	ref class ImageView
	{
	private:
		VkImageView view;
		bool locked;
	internal:
		VkImageView GetImageView();
	public:
		property ImageFormat Format;
		property ImageViewType ViewType;
		property int BaseLevel;
		property int LevelCount;
		property int BaseLayer;
		property int LayerCount;
		ImageView();
		~ImageView();
		void Build(Image img);
	};
}

