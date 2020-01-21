#include "GameWindow.h"
#include <vcclr.h>

using namespace Kokoro::Graphics;

GameWindow::GameWindow(int w, int h, String^ winTitle) {
	using namespace Runtime::InteropServices;

	const char* cPtr = (const char*)Marshal::StringToHGlobalAnsi(winTitle).ToPointer();
	glfwInit();
	glfwWindowHint(GLFW_CLIENT_API, GLFW_NO_API);
	glfwWindowHint(GLFW_RESIZABLE, GLFW_FALSE);
	window = glfwCreateWindow(w, h, cPtr, nullptr, nullptr);
	Marshal::FreeHGlobal(IntPtr((void*)cPtr));
}

void GameWindow::Poll() {
	while (!glfwWindowShouldClose(window)) {
		glfwPollEvents();
	}
}

VkResult GameWindow::GetSurface(VkInstance inst, VkSurfaceKHR* surf)
{
	return glfwCreateWindowSurface(inst, window, nullptr, surf);
}

int Kokoro::Graphics::GameWindow::GetWidth()
{
	int w = 0;
	glfwGetWindowSize(window, &w, NULL);
	return w;
}

int Kokoro::Graphics::GameWindow::GetHeight()
{
	int h = 0;
	glfwGetWindowSize(window, NULL, &h);
	return h;
}

GameWindow::~GameWindow() {
	glfwDestroyWindow(window);
	glfwTerminate();
}