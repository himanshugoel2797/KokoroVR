#pragma once
#ifdef _WIN32
#define NOMINMAX
#define WIN32_LEAN_AND_MEAN
#define VK_USE_PLATFORM_WIN32_KHR
#endif
#define VMA_VULKAN_VERSION 1001000 // Vulkan 1.1
#include "vulkan/vulkan.h"

#include "VmaWrapper.h"
#include "GameWindow.h"
#include "MemoryUsage.h"

using namespace System;

namespace Kokoro::Graphics {
	public ref class GraphicsDevice
	{
	private:
		static bool initialized;
		static bool validationEnabled;
		static VkInstance instance;
		static VkDebugUtilsMessengerEXT debugMessenger;
		static VkPhysicalDevice physDevice;
		static VkDevice device;
		static VkQueue graphicsQueue;
		static VkQueue computeQueue;
		static VkQueue transferQueue;
		static VkQueue presentQueue;
		static array<uint32_t>^ queueFams;

		static VkSurfaceKHR surface;
		static VkPresentModeKHR present_mode;

		static VkSwapchainKHR swapchain;
		static uint32_t swapchain_img_cnt;

		static const char* appName;
		static const char* engineName;

		static GameWindow^ window;

		//static VmaAllocator allocator;
		static VmaWrapper* allocator;

		static bool extnsSupported(VkPhysicalDevice device);
		static int rateDevice(VkPhysicalDevice device);

	internal:
		static VkDevice GetDevice();
		static int CreateBuffer(VkBufferCreateInfo* creatInfo, MemoryUsage memUsage, bool persistent_map, VkBuffer* buf, WVmaAllocation* alloc);
		static void DestroyBuffer(VkBuffer buf, WVmaAllocation alloc);
		static int CreateImage(VkImageCreateInfo* creatInfo, VkImage* img, WVmaAllocation* alloc);
		static void DestroyImage(VkImage img, WVmaAllocation alloc);

	public:
		static uint32_t GetWidth();
		static uint32_t GetHeight();
		static void SetNames(String^ appName, String^ engineName);
		static void Destroy();
		static void CreateInstance(bool enableValidation);
	};
}

