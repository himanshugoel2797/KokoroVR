#pragma once
#ifdef _WIN32
#define VK_USE_PLATFORM_WIN32_KHR
#endif
#include "vulkan/vulkan.h"

#include "GameWindow.h"

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

		static VkSurfaceKHR surface;
		static VkPresentModeKHR present_mode;
		
		static VkSwapchainKHR swapchain;
		static uint32_t swapchain_img_cnt;

		static const char* appName;
		static const char* engineName;

		static GameWindow^ window;

		static bool extnsSupported(VkPhysicalDevice device);
		static int rateDevice(VkPhysicalDevice device);

	public:
		static void SetNames(String^ appName, String^ engineName);
		static void Destroy();
		static void CreateInstance(bool enableValidation);
	};
}

