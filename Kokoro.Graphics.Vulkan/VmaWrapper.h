#pragma once
#ifdef _WIN32
#define NOMINMAX
#define WIN32_LEAN_AND_MEAN
#define VK_USE_PLATFORM_WIN32_KHR
#endif
#define VMA_VULKAN_VERSION 1001000 // Vulkan 1.1
#include "vulkan/vulkan.h"

#include "MemoryUsage.h"

namespace Kokoro::Graphics {
	class VmaWrapper;
	class WVmaAllocation_T {
		void* info;
		void* alloc;

		friend class VmaWrapper;
	public:
		VkDeviceMemory GetMemory();
		void* GetPtr();
		WVmaAllocation_T();
		~WVmaAllocation_T();
	};
	typedef WVmaAllocation_T* WVmaAllocation;

	class VmaWrapper
	{
	private:
		void* allocator;
		VmaWrapper();
	public:
		static VmaWrapper* Create(VkPhysicalDevice phys_dev, VkDevice dev);
		int CreateBuffer(VkBufferCreateInfo* creatInfo, MemoryUsage memUsage, bool persistent_map, uint32_t* queueFams, uint32_t queueFamCount, VkBuffer* buf, WVmaAllocation* alloc);
		void DestroyBuffer(VkBuffer buf, WVmaAllocation alloc);

		int CreateImage(VkImageCreateInfo* creatInfo, uint32_t* queueFams, uint32_t queueFamCount, VkImage* img, WVmaAllocation* alloc);
		void DestroyImage(VkImage img, WVmaAllocation alloc);

		~VmaWrapper();
	};
}

