#pragma once
#ifdef _WIN32
#define VK_USE_PLATFORM_WIN32_KHR
#endif
#define GLFW_INCLUDE_VULKAN
#include "vulkan/vulkan.h"
#include "GLFW/glfw3.h"
using namespace System;

namespace Kokoro::Graphics {
	public ref class GameWindow
	{
	private:
		GLFWwindow* window;
	public:
		GameWindow(int w, int h, String^ winTitle);
		void Poll();
		VkResult GetSurface(VkInstance inst, VkSurfaceKHR* surf);
		int GetWidth();
		int GetHeight();
		~GameWindow();
	};
}

