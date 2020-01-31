#include "ImageView.h"

Kokoro::Graphics::ImageView::ImageView() {
	locked = false;
}

Kokoro::Graphics::ImageView::~ImageView() {
	if (!locked) {
		vkDestroyImageView(GraphicsDevice::GetDevice(), view, nullptr);
	}
}

void Kokoro::Graphics::ImageView::Build(Image img) {
	if (!locked) {
		VkImageViewCreateInfo creatInfo = {};
		creatInfo.sType = VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO;
		creatInfo.flags = 0;
		creatInfo.image = img.GetImage();
		creatInfo.viewType = ImageViewTypeConverter::Convert(ViewType);
		creatInfo.format = ImageFormatConv::Convert(Format);
		creatInfo.components = {
			VK_COMPONENT_SWIZZLE_IDENTITY,
			VK_COMPONENT_SWIZZLE_IDENTITY,
			VK_COMPONENT_SWIZZLE_IDENTITY,
			VK_COMPONENT_SWIZZLE_IDENTITY,
		};
		switch (Format) {
		case ImageFormat::Depth16f:
		case ImageFormat::Depth32f:
			creatInfo.subresourceRange.aspectMask = VK_IMAGE_ASPECT_DEPTH_BIT;
			break;
		default:
			creatInfo.subresourceRange.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
		}
		creatInfo.subresourceRange.baseMipLevel = BaseLevel;
		creatInfo.subresourceRange.levelCount = LevelCount;
		creatInfo.subresourceRange.baseArrayLayer = BaseLayer;
		creatInfo.subresourceRange.layerCount = LayerCount;

		pin_ptr<VkImageView> view_ptr = &view;
		if (vkCreateImageView(GraphicsDevice::GetDevice(), &creatInfo, nullptr, view_ptr) != VK_SUCCESS) {
			throw gcnew System::Exception("Failed to create image view.");
		}
		locked = true;
	}
}

VkImageView Kokoro::Graphics::ImageView::GetImageView() {
	return view;
}