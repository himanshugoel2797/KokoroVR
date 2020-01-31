#include "GraphicsDevice.h"
#include "GLFW/glfw3.h"

#include <iostream>
#include <vector>
#include <map>
#include <string>
#include <set>
#include <algorithm>

using namespace Runtime::InteropServices;
using namespace Kokoro::Graphics;

const std::vector<const char*> validationLayers = {
	"VK_LAYER_KHRONOS_validation",
};

const std::vector<const char*> deviceExtns = {
	VK_KHR_SWAPCHAIN_EXTENSION_NAME,
	VK_EXT_SUBGROUP_SIZE_CONTROL_EXTENSION_NAME,
};

#pragma unmanaged
static VKAPI_ATTR VkBool32 VKAPI_CALL debugCallback(
	VkDebugUtilsMessageSeverityFlagBitsEXT severity,
	VkDebugUtilsMessageTypeFlagsEXT type,
	const VkDebugUtilsMessengerCallbackDataEXT* callbackData,
	void* userData) {
	std::cerr << "validation layer: " << callbackData->pMessage << std::endl;
	return VK_FALSE;
}

VkResult CreateDebugUtilsMessengerEXT(VkInstance instance, const VkDebugUtilsMessengerCreateInfoEXT* pCreateInfo, const VkAllocationCallbacks* pAllocator, VkDebugUtilsMessengerEXT* pDebugMessenger) {
	auto func = (PFN_vkCreateDebugUtilsMessengerEXT)vkGetInstanceProcAddr(instance, "vkCreateDebugUtilsMessengerEXT");
	if (func != nullptr) {
		return func(instance, pCreateInfo, pAllocator, pDebugMessenger);
	}
	else {
		return VK_ERROR_EXTENSION_NOT_PRESENT;
	}
}

void DestroyDebugUtilsMessengerEXT(VkInstance instance, VkDebugUtilsMessengerEXT debugMessenger, const VkAllocationCallbacks* pAllocator) {
	auto func = (PFN_vkDestroyDebugUtilsMessengerEXT)vkGetInstanceProcAddr(instance, "vkDestroyDebugUtilsMessengerEXT");
	if (func != nullptr) {
		func(instance, debugMessenger, pAllocator);
	}
}
#pragma managed

static std::vector<VkImage> swapChainImages;
static std::vector<VkImageView> swapChainViews;
static VkSurfaceFormatKHR surface_fmt;
static VkExtent2D surface_extent;


void Kokoro::Graphics::GraphicsDevice::SetNames(String^ appName, String^ engineName)
{
	GraphicsDevice::appName = (const char*)Marshal::StringToHGlobalAnsi(appName).ToPointer();
	GraphicsDevice::engineName = (const char*)Marshal::StringToHGlobalAnsi(engineName).ToPointer();
	window = gcnew GameWindow(1280, 720, appName);
}

void Kokoro::Graphics::GraphicsDevice::Destroy()
{
	if (initialized) {
		for (auto imgView : swapChainViews)
			vkDestroyImageView(device, imgView, nullptr);
		vkDestroySwapchainKHR(device, swapchain, nullptr);
		delete allocator;
		vkDestroyDevice(device, nullptr);
		if (validationEnabled) DestroyDebugUtilsMessengerEXT(instance, debugMessenger, nullptr);
		vkDestroySurfaceKHR(instance, surface, nullptr);
		vkDestroyInstance(instance, nullptr);
	}
	Marshal::FreeHGlobal(IntPtr((void*)appName));
	Marshal::FreeHGlobal(IntPtr((void*)engineName));
}

bool Kokoro::Graphics::GraphicsDevice::extnsSupported(VkPhysicalDevice device) {
	uint32_t extn_cnt;
	vkEnumerateDeviceExtensionProperties(device, nullptr, &extn_cnt, nullptr);

	std::vector<VkExtensionProperties> availExtns(extn_cnt);
	vkEnumerateDeviceExtensionProperties(device, nullptr, &extn_cnt, availExtns.data());

	std::set<std::string> reqExtns(deviceExtns.begin(), deviceExtns.end());
	for (const auto& extn : availExtns) {
		reqExtns.erase(extn.extensionName);
	}

	return reqExtns.empty();
}

int Kokoro::Graphics::GraphicsDevice::rateDevice(VkPhysicalDevice device) {
	VkPhysicalDeviceProperties devProps;
	VkPhysicalDeviceFeatures devFeats;

	vkGetPhysicalDeviceProperties(device, &devProps);
	vkGetPhysicalDeviceFeatures(device, &devFeats);

	int score = 0;
	if (devProps.deviceType == VK_PHYSICAL_DEVICE_TYPE_DISCRETE_GPU)
		score += 100;

	score += devProps.limits.maxImageDimension2D;

	if (!devFeats.multiDrawIndirect)
		return 0;

	if (!devFeats.tessellationShader)
		return 0;

	if (!extnsSupported(device))
		return 0;

	VkSurfaceCapabilitiesKHR caps;
	vkGetPhysicalDeviceSurfaceCapabilitiesKHR(device, surface, &caps);

	uint32_t fmt_cnt = 0;
	vkGetPhysicalDeviceSurfaceFormatsKHR(device, surface, &fmt_cnt, nullptr);
	std::vector<VkSurfaceFormatKHR> fmts(fmt_cnt);
	vkGetPhysicalDeviceSurfaceFormatsKHR(device, surface, &fmt_cnt, fmts.data());

	bool fmt_valid = false;
	VkSurfaceFormatKHR chosen_fmt = {};
	for (const auto& avail_fmt : fmts) {
		if (avail_fmt.format == VK_FORMAT_B8G8R8A8_UNORM && avail_fmt.colorSpace == VK_COLOR_SPACE_SRGB_NONLINEAR_KHR) {
			chosen_fmt = avail_fmt;
			fmt_valid = true;
			break;
		}
	}
	if (!fmt_valid && fmts.size() > 0) {
		chosen_fmt = fmts[0];
		fmt_valid = true;
	}

	uint32_t present_mode_cnt = 0;
	vkGetPhysicalDeviceSurfacePresentModesKHR(device, surface, &present_mode_cnt, nullptr);
	std::vector<VkPresentModeKHR> present_modes(present_mode_cnt);
	vkGetPhysicalDeviceSurfacePresentModesKHR(device, surface, &present_mode_cnt, present_modes.data());

	bool present_valid = false;
	VkPresentModeKHR chosen_present = {};
	for (const auto& avail_present : present_modes) {
		if (avail_present == VK_PRESENT_MODE_MAILBOX_KHR) {
			present_valid = true;
			chosen_present = VK_PRESENT_MODE_MAILBOX_KHR;
		}
		else if (avail_present == VK_PRESENT_MODE_FIFO_RELAXED_KHR && avail_present != VK_PRESENT_MODE_MAILBOX_KHR) {
			present_valid = true;
			chosen_present = VK_PRESENT_MODE_FIFO_RELAXED_KHR;
		}
	}
	if (!present_valid) {
		chosen_present = VK_PRESENT_MODE_FIFO_KHR;
		present_valid = true;
	}

	if (!fmt_valid)
		return 0;
	if (!present_valid)
		return 0;

	present_mode = chosen_present;
	surface_fmt = chosen_fmt;

	return score;
}

void Kokoro::Graphics::GraphicsDevice::CreateInstance(bool enableValidation)
{
	validationEnabled = enableValidation;

	VkApplicationInfo appInfo = {};
	appInfo.sType = VK_STRUCTURE_TYPE_APPLICATION_INFO;
	appInfo.pApplicationName = appName;
	appInfo.applicationVersion = VK_MAKE_VERSION(1, 0, 0);
	appInfo.pEngineName = engineName;
	appInfo.engineVersion = VK_MAKE_VERSION(1, 0, 0);
	appInfo.apiVersion = VK_API_VERSION_1_1;

	VkInstanceCreateInfo createInfo = {};
	createInfo.sType = VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO;
	createInfo.pApplicationInfo = &appInfo;
	if (enableValidation) {
		createInfo.enabledLayerCount = static_cast<uint32_t>(validationLayers.size());
		createInfo.ppEnabledLayerNames = validationLayers.data();
	}
	uint32_t glfwExtnCnt = 0;
	const char** glfwExtns = glfwGetRequiredInstanceExtensions(&glfwExtnCnt);
	std::vector<const char*> extns(glfwExtns, glfwExtns + glfwExtnCnt);
	if (enableValidation) {
		extns.push_back(VK_EXT_DEBUG_UTILS_EXTENSION_NAME);
	}

	createInfo.enabledExtensionCount = static_cast<uint32_t>(extns.size());
	createInfo.ppEnabledExtensionNames = extns.data();

	createInfo.enabledLayerCount = 0;

	pin_ptr<VkInstance> inst_ptr = &instance;
	auto result = vkCreateInstance(&createInfo, nullptr, inst_ptr);
	if (result != VK_SUCCESS)
		throw gcnew System::Exception("Failed to create instance.");

	if (enableValidation) {
		VkDebugUtilsMessengerCreateInfoEXT debugCreatInfo = {};
		debugCreatInfo.sType = VK_STRUCTURE_TYPE_DEBUG_UTILS_MESSENGER_CREATE_INFO_EXT;
		debugCreatInfo.messageSeverity = VK_DEBUG_UTILS_MESSAGE_SEVERITY_ERROR_BIT_EXT | VK_DEBUG_UTILS_MESSAGE_SEVERITY_WARNING_BIT_EXT | VK_DEBUG_UTILS_MESSAGE_SEVERITY_VERBOSE_BIT_EXT;
		debugCreatInfo.messageType = VK_DEBUG_UTILS_MESSAGE_TYPE_GENERAL_BIT_EXT | VK_DEBUG_UTILS_MESSAGE_TYPE_PERFORMANCE_BIT_EXT | VK_DEBUG_UTILS_MESSAGE_TYPE_VALIDATION_BIT_EXT;
		debugCreatInfo.pfnUserCallback = debugCallback;
		debugCreatInfo.pUserData = nullptr;

		pin_ptr<VkDebugUtilsMessengerEXT> dbg_ptr = &debugMessenger;
		result = CreateDebugUtilsMessengerEXT(instance, &debugCreatInfo, nullptr, dbg_ptr);
		if (result != VK_SUCCESS)
			throw gcnew System::Exception("Failed to setup debugging.");
	}

	pin_ptr<VkSurfaceKHR> surf_ptr = &surface;
	result = window->GetSurface(instance, surf_ptr);
	if (result != VK_SUCCESS) {
		throw gcnew System::Exception("Failed to create surface.");
	}

	uint32_t devCount = 0;
	vkEnumeratePhysicalDevices(instance, &devCount, nullptr);
	if (devCount == 0)
		throw gcnew System::Exception("Failed to find Vulkan compatible device.");

	std::vector<VkPhysicalDevice> devices(devCount);
	vkEnumeratePhysicalDevices(instance, &devCount, devices.data());

	std::multimap<int, VkPhysicalDevice> device_map;
	for (const auto& device : devices) {
		auto score = rateDevice(device);
		device_map.insert(std::make_pair(score, device));
	}

	if (device_map.rbegin()->first > 0)
		physDevice = device_map.rbegin()->second;
	else
		throw gcnew System::Exception("Failed to find a suitable GPU.");

	int graphicsFamily = -1;
	int computeFamily = -1;
	int transferFamily = -1;
	int presentFamily = -1;

	uint32_t qfam_cnt = 0;
	vkGetPhysicalDeviceQueueFamilyProperties(physDevice, &qfam_cnt, nullptr);

	std::vector<VkQueueFamilyProperties> qFams(qfam_cnt);
	vkGetPhysicalDeviceQueueFamilyProperties(physDevice, &qfam_cnt, qFams.data());

	int i = 0;
	for (const auto& qFam : qFams) {

		VkBool32 presentSupport = false;
		vkGetPhysicalDeviceSurfaceSupportKHR(physDevice, i, surface, &presentSupport);

		if (qFam.queueFlags & VK_QUEUE_GRAPHICS_BIT) {
			graphicsFamily = i;
			if (presentSupport) presentFamily = i;
		}

		if ((qFam.queueFlags & VK_QUEUE_COMPUTE_BIT) && i != graphicsFamily)
			computeFamily = i;

		if ((qFam.queueFlags & VK_QUEUE_TRANSFER_BIT) && i != computeFamily)
			transferFamily = i;

		if (graphicsFamily != -1 && computeFamily != -1 && transferFamily != -1 && presentFamily != -1)
			break;
		i++;
	}

	if (presentFamily == -1)
		throw gcnew System::NotImplementedException("Separate present queue support hasn't been implemented.");

	float max_q_priority = 1.0f;
	float dual_graph_q_priority[] = { 1.0f };
	float triple_graph_q_priority[] = { 1.0f, 1.0f, 1.0f };

	VkDeviceQueueCreateInfo graphics_qCreatInfo = {};
	graphics_qCreatInfo.sType = VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO;
	graphics_qCreatInfo.queueFamilyIndex = graphicsFamily;
	graphics_qCreatInfo.queueCount = 1;
	graphics_qCreatInfo.pQueuePriorities = &max_q_priority;

	VkDeviceQueueCreateInfo compute_qCreatInfo = {};
	compute_qCreatInfo.sType = VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO;
	compute_qCreatInfo.queueFamilyIndex = computeFamily;
	compute_qCreatInfo.queueCount = 1;
	compute_qCreatInfo.pQueuePriorities = &max_q_priority;

	VkDeviceQueueCreateInfo transfer_qCreatInfo = {};
	if (transferFamily != graphicsFamily) {
		transfer_qCreatInfo.sType = VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO;
		transfer_qCreatInfo.queueFamilyIndex = transferFamily;
		transfer_qCreatInfo.queueCount = 1;
		transfer_qCreatInfo.pQueuePriorities = &max_q_priority;
	}
	else {
		graphics_qCreatInfo.queueCount = 2;
		graphics_qCreatInfo.pQueuePriorities = dual_graph_q_priority;
	}

	VkDeviceQueueCreateInfo present_qCreatInfo = {};
	if (presentFamily != graphicsFamily) {
		present_qCreatInfo.sType = VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO;
		present_qCreatInfo.queueFamilyIndex = presentFamily;
		present_qCreatInfo.queueCount = 1;
		present_qCreatInfo.pQueuePriorities = &max_q_priority;
	}
	else {
		if (transferFamily == graphicsFamily) {
			graphics_qCreatInfo.queueCount = 3;
			graphics_qCreatInfo.pQueuePriorities = triple_graph_q_priority;
		}
		else {
			graphics_qCreatInfo.queueCount = 2;
			graphics_qCreatInfo.pQueuePriorities = dual_graph_q_priority;
		}
	}

	std::vector<VkDeviceQueueCreateInfo> qCreatInfos(3);
	qCreatInfos.push_back(graphics_qCreatInfo);
	qCreatInfos.push_back(compute_qCreatInfo);
	if (transferFamily != graphicsFamily)qCreatInfos.push_back(transfer_qCreatInfo);
	if (presentFamily != graphicsFamily)qCreatInfos.push_back(present_qCreatInfo);

	int idx = 0;
	queueFams = gcnew array<uint32_t>(qCreatInfos.size());
	queueFams[idx++] = static_cast<uint32_t>(graphicsFamily);
	queueFams[idx++] = static_cast<uint32_t>(computeFamily);
	if (transferFamily != graphicsFamily)queueFams[idx++] = static_cast<uint32_t>(transferFamily);
	if (presentFamily != graphicsFamily)queueFams[idx++] = static_cast<uint32_t>(presentFamily);

	VkPhysicalDeviceFeatures devFeats = {};
	devFeats.multiDrawIndirect = VK_TRUE;
	devFeats.tessellationShader = VK_TRUE;
	devFeats.fragmentStoresAndAtomics = VK_TRUE;
	devFeats.vertexPipelineStoresAndAtomics = VK_TRUE;
	if (enableValidation) {
		devFeats.robustBufferAccess = VK_TRUE;
	}

	VkDeviceCreateInfo devCreatInfo = {};
	devCreatInfo.sType = VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO;
	devCreatInfo.queueCreateInfoCount = static_cast<uint32_t>(qCreatInfos.size());
	devCreatInfo.pQueueCreateInfos = qCreatInfos.data();
	devCreatInfo.pEnabledFeatures = &devFeats;

	devCreatInfo.enabledExtensionCount = static_cast<uint32_t>(deviceExtns.size());
	devCreatInfo.ppEnabledExtensionNames = deviceExtns.data();

	if (enableValidation) {
		devCreatInfo.enabledLayerCount = static_cast<uint32_t>(validationLayers.size());
		devCreatInfo.ppEnabledLayerNames = validationLayers.data();
	}
	else {
		devCreatInfo.enabledLayerCount = 0;
	}

	pin_ptr<VkDevice> dev_ptr = &device;
	result = vkCreateDevice(physDevice, &devCreatInfo, nullptr, dev_ptr);
	if (result != VK_SUCCESS) {
		throw gcnew System::Exception("Failed to create logical device.");
	}

	allocator = VmaWrapper::Create(physDevice, device);

	pin_ptr<VkQueue> graph_q_hndl = &graphicsQueue;
	pin_ptr<VkQueue> comp_q_hndl = &computeQueue;
	pin_ptr<VkQueue> trans_q_hndl = &transferQueue;
	pin_ptr<VkQueue> pres_q_hndl = &presentQueue;

	vkGetDeviceQueue(device, graphicsFamily, 0, graph_q_hndl);
	vkGetDeviceQueue(device, computeFamily, 0, comp_q_hndl);
	if (transferFamily != graphicsFamily) vkGetDeviceQueue(device, transferFamily, 0, trans_q_hndl);
	else vkGetDeviceQueue(device, graphicsFamily, 1, trans_q_hndl);

	//TODO update this to support separate present queue family
	if (transferFamily != graphicsFamily) vkGetDeviceQueue(device, presentFamily, 1, pres_q_hndl);
	else vkGetDeviceQueue(device, presentFamily, 2, pres_q_hndl);

	VkSurfaceCapabilitiesKHR caps;
	vkGetPhysicalDeviceSurfaceCapabilitiesKHR(physDevice, surface, &caps);

	VkExtent2D cur_extent;
	if (caps.currentExtent.width != UINT32_MAX) {
		cur_extent = caps.currentExtent;
	}
	else {
		cur_extent.width = std::clamp(static_cast<uint32_t>(window->GetWidth()), caps.minImageExtent.width, caps.maxImageExtent.width);
		cur_extent.height = std::clamp(static_cast<uint32_t>(window->GetHeight()), caps.minImageExtent.height, caps.maxImageExtent.height);
	}

	uint32_t img_cnt = caps.minImageCount + 1;

	VkSwapchainCreateInfoKHR swapCreatInfo = {};
	swapCreatInfo.sType = VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR;
	swapCreatInfo.surface = surface;
	swapCreatInfo.minImageCount = img_cnt;
	swapCreatInfo.imageFormat = surface_fmt.format;
	swapCreatInfo.imageColorSpace = surface_fmt.colorSpace;
	swapCreatInfo.imageExtent = cur_extent;
	swapCreatInfo.imageArrayLayers = 1;
	swapCreatInfo.imageUsage = VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;

	//TODO update this to support separate present queue family
	swapCreatInfo.imageSharingMode = VK_SHARING_MODE_EXCLUSIVE;
	swapCreatInfo.queueFamilyIndexCount = 0;
	swapCreatInfo.pQueueFamilyIndices = nullptr;
	swapCreatInfo.preTransform = caps.currentTransform;
	swapCreatInfo.compositeAlpha = VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR;
	swapCreatInfo.presentMode = present_mode;
	swapCreatInfo.clipped = VK_TRUE;
	swapCreatInfo.oldSwapchain = VK_NULL_HANDLE;

	pin_ptr<VkSwapchainKHR> swapchain_ptr = &swapchain;
	result = vkCreateSwapchainKHR(device, &swapCreatInfo, nullptr, swapchain_ptr);
	if (result != VK_SUCCESS)
		throw gcnew System::Exception("Failed to create swapchain");

	pin_ptr<uint32_t> swapchain_img_cnt_ptr = &swapchain_img_cnt;
	vkGetSwapchainImagesKHR(device, swapchain, swapchain_img_cnt_ptr, nullptr);
	swapChainImages.resize(swapchain_img_cnt);
	swapChainViews.resize(swapchain_img_cnt);
	vkGetSwapchainImagesKHR(device, swapchain, swapchain_img_cnt_ptr, swapChainImages.data());

	surface_extent = cur_extent;
	for (size_t i = 0; i < swapChainImages.size(); i++) {
		VkImageViewCreateInfo imgViewCreatInfo = {};
		imgViewCreatInfo.sType = VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO;
		imgViewCreatInfo.image = swapChainImages[i];
		imgViewCreatInfo.viewType = VK_IMAGE_VIEW_TYPE_2D;
		imgViewCreatInfo.format = surface_fmt.format;
		imgViewCreatInfo.components.r = VK_COMPONENT_SWIZZLE_IDENTITY;
		imgViewCreatInfo.components.g = VK_COMPONENT_SWIZZLE_IDENTITY;
		imgViewCreatInfo.components.b = VK_COMPONENT_SWIZZLE_IDENTITY;
		imgViewCreatInfo.components.a = VK_COMPONENT_SWIZZLE_IDENTITY;
		imgViewCreatInfo.subresourceRange.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
		imgViewCreatInfo.subresourceRange.baseMipLevel = 0;
		imgViewCreatInfo.subresourceRange.baseArrayLayer = 0;
		imgViewCreatInfo.subresourceRange.levelCount = 1;
		imgViewCreatInfo.subresourceRange.layerCount = 1;
		result = vkCreateImageView(device, &imgViewCreatInfo, nullptr, &swapChainViews[i]);
		if (result != VK_SUCCESS)
			throw gcnew System::Exception("Failed to create image views.");
	}

	initialized = true;
}

int Kokoro::Graphics::GraphicsDevice::CreateBuffer(VkBufferCreateInfo* creatInfo, MemoryUsage memUsage, bool persistent_map, VkBuffer* buf, WVmaAllocation* alloc) {
	pin_ptr<uint32_t> queueFams_ptr = &queueFams[0];
	return allocator->CreateBuffer(creatInfo, memUsage, persistent_map, queueFams_ptr, queueFams->Length, buf, alloc);
}

void Kokoro::Graphics::GraphicsDevice::DestroyBuffer(VkBuffer buf, WVmaAllocation alloc) {
	allocator->DestroyBuffer(buf, alloc);
}

int Kokoro::Graphics::GraphicsDevice::CreateImage(VkImageCreateInfo* creatInfo, VkImage* img, WVmaAllocation* alloc) {
	pin_ptr<uint32_t> queueFams_ptr = &queueFams[0];
	return allocator->CreateImage(creatInfo, queueFams_ptr, queueFams->Length, img, alloc);
}

void Kokoro::Graphics::GraphicsDevice::DestroyImage(VkImage img, WVmaAllocation alloc) {
	allocator->DestroyImage(img, alloc);
}

VkDevice Kokoro::Graphics::GraphicsDevice::GetDevice() {
	return device;
}

uint32_t Kokoro::Graphics::GraphicsDevice::GetWidth() {
	return static_cast<uint32_t>(window->GetWidth());
}

uint32_t Kokoro::Graphics::GraphicsDevice::GetHeight() {
	return static_cast<uint32_t>(window->GetHeight());
}