#include "Image.h"

Kokoro::Graphics::Image::Image()
{
	Width = 1;
	Height = 1;
	Depth = 1;
	Levels = 1;
	Layers = 1;
	Dimensions = 2;
	Format = ImageFormat::R8G8B8A8Unorm;
	Usage = ImageUsage::Sampled | ImageUsage::TransferDst;
	locked = false;
}

Kokoro::Graphics::Image::~Image()
{
	if (!locked) {
		GraphicsDevice::DestroyImage(img, img_alloc);
	}
}

void Kokoro::Graphics::Image::Build()
{
	if (!locked) {
		VkImageCreateInfo creatInfo = {};
		creatInfo.sType = VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO;
		creatInfo.flags = VK_IMAGE_CREATE_MUTABLE_FORMAT_BIT;

		switch (Dimensions) {
		case 1:
			creatInfo.imageType = VK_IMAGE_TYPE_1D;
			break;
		case 2:
			creatInfo.imageType = VK_IMAGE_TYPE_2D;
			if (Layers > 1)
				creatInfo.flags |= VK_IMAGE_CREATE_2D_ARRAY_COMPATIBLE_BIT;
			if (Cubemappable)
				creatInfo.flags |= VK_IMAGE_CREATE_CUBE_COMPATIBLE_BIT;
			break;
		case 3:
			creatInfo.imageType = VK_IMAGE_TYPE_3D;
			break;
		}
		creatInfo.format = ImageFormatConv::Convert(Format);
		creatInfo.extent = {
			static_cast<uint32_t>(Width),
			static_cast<uint32_t>(Height),
			static_cast<uint32_t>(Depth)
		};
		creatInfo.mipLevels = Levels;
		creatInfo.arrayLayers = Layers;
		creatInfo.samples = VK_SAMPLE_COUNT_1_BIT;
		creatInfo.tiling = VK_IMAGE_TILING_OPTIMAL;
		creatInfo.usage = ImageUsageConverter::Convert(Usage);
		creatInfo.sharingMode = VK_SHARING_MODE_CONCURRENT;
		creatInfo.initialLayout = VK_IMAGE_LAYOUT_UNDEFINED;

		pin_ptr<VkImage> img_ptr = &img;
		pin_ptr<WVmaAllocation> img_alloc_ptr = &img_alloc;
		if (GraphicsDevice::CreateImage(&creatInfo, img_ptr, img_alloc_ptr) != VK_SUCCESS)
			throw gcnew System::Exception("Failed to create image.");
		locked = true;
	}
}

VkImage Kokoro::Graphics::Image::GetImage() {
	return img;
}