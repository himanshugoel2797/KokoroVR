#define VMA_IMPLEMENTATION
#ifdef _WIN32
#define NOMINMAX
#define WIN32_LEAN_AND_MEAN
#define VK_USE_PLATFORM_WIN32_KHR
#endif
#define VMA_VULKAN_VERSION 1001000 // Vulkan 1.1
#include "vulkan/vulkan.h"
#include <memory>
#include "vk_mem_alloc.h"

#include "VmaWrapper.h"

Kokoro::Graphics::WVmaAllocation_T::WVmaAllocation_T() {
	info = new VmaAllocationInfo;
	alloc = new VmaAllocation_T;
}

Kokoro::Graphics::WVmaAllocation_T::~WVmaAllocation_T() {
	delete alloc;
	delete info;
}

VkDeviceMemory Kokoro::Graphics::WVmaAllocation_T::GetMemory() {
	return ((VmaAllocation)alloc)->GetMemory();
}

void* Kokoro::Graphics::WVmaAllocation_T::GetPtr() {
	return ((VmaAllocationInfo*)info)->pMappedData;
}

Kokoro::Graphics::VmaWrapper::VmaWrapper() {

}

Kokoro::Graphics::VmaWrapper::~VmaWrapper() {
	vmaDestroyAllocator((VmaAllocator)allocator);
	delete allocator;
}

Kokoro::Graphics::VmaWrapper* Kokoro::Graphics::VmaWrapper::Create(VkPhysicalDevice phys_dev, VkDevice dev) {
	auto wrapper = new VmaWrapper();
	wrapper->allocator = (void*)new VmaAllocation_T;
	VmaAllocatorCreateInfo creatInfo = {};
	creatInfo.device = dev;
	creatInfo.physicalDevice = phys_dev;
	vmaCreateAllocator(&creatInfo, (VmaAllocator*)&wrapper->allocator);
	return wrapper;
}

int Kokoro::Graphics::VmaWrapper::CreateBuffer(VkBufferCreateInfo* creatInfo, MemoryUsage memUsage, bool persistent_map, uint32_t *queueFams, uint32_t queueFamCount,  VkBuffer* buf, WVmaAllocation* alloc) {
	VmaAllocationCreateInfo allocCreatInfo = {};
	allocCreatInfo.usage = (VmaMemoryUsage)MemoryUsageConv::Convert(memUsage);
	if (persistent_map)
		allocCreatInfo.flags = VMA_ALLOCATION_CREATE_MAPPED_BIT;

	if (creatInfo->sharingMode == VK_SHARING_MODE_CONCURRENT) {
		creatInfo->queueFamilyIndexCount = queueFamCount;
		creatInfo->pQueueFamilyIndices = queueFams;
	}
	else {
		creatInfo->queueFamilyIndexCount = 0;
	}

	return vmaCreateBuffer((VmaAllocator)allocator, creatInfo, &allocCreatInfo, buf, (VmaAllocation*)&(*alloc)->alloc, (VmaAllocationInfo*)(*alloc)->info);
}

void Kokoro::Graphics::VmaWrapper::DestroyBuffer(VkBuffer buf, WVmaAllocation alloc) {
	vmaDestroyBuffer((VmaAllocator)allocator, buf, (VmaAllocation)alloc->alloc);
}

int Kokoro::Graphics::VmaWrapper::CreateImage(VkImageCreateInfo* creatInfo, uint32_t* queueFams, uint32_t queueFamCount, VkImage* img, WVmaAllocation* alloc) {
	VmaAllocationCreateInfo allocCreatInfo = {};
	allocCreatInfo.usage = VMA_MEMORY_USAGE_GPU_ONLY;
	if (creatInfo->sharingMode == VK_SHARING_MODE_CONCURRENT) {
		creatInfo->queueFamilyIndexCount = queueFamCount;
		creatInfo->pQueueFamilyIndices = queueFams;
	}
	else {
		creatInfo->queueFamilyIndexCount = 0;
	}

	return vmaCreateImage((VmaAllocator)allocator, creatInfo, &allocCreatInfo, img, (VmaAllocation*)&(*alloc)->alloc, (VmaAllocationInfo*)(*alloc)->info);
}

void Kokoro::Graphics::VmaWrapper::DestroyImage(VkImage img, WVmaAllocation alloc) {
	vmaDestroyImage((VmaAllocator)allocator, img, (VmaAllocation)alloc->alloc);
}