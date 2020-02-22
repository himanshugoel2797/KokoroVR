using System;
using System.Collections.Generic;
using System.Text;

using static VulkanSharp.Raw.Vk;
using static VulkanSharp.Raw.Glfw;
using System.Runtime.InteropServices;
using System.Linq;

namespace Kokoro.Graphics
{
    class VulkanDevice
    {
        public IntPtr device;
        public IntPtr physDevice;
        public uint graphicsFamily;
        public uint computeFamily;
        public uint transferFamily;
        public uint presentFamily;

        public IntPtr graphicsQueue;
        public IntPtr computeQueue;
        public IntPtr transferQueue;
        public IntPtr presentQueue;

        public VulkanDevice(IntPtr physDevice)
        {
            this.physDevice = physDevice;
        }
    }

    public static unsafe class GraphicsDevice
    {
        private static IntPtr instanceHndl;
        private static IntPtr surfaceHndl;
        private static IntPtr swapChainHndl;
        private static string[] requiredDeviceExtns;
        private static VkPresentModeKHR present_mode;
        private static VkSurfaceFormatKHR surface_fmt;
        private static IntPtr[] orderedDevices;
        private static VulkanDevice[] orderedDeviceData;
        private static uint swapchain_img_cnt;
        private static IntPtr[] swapchainImages;
        private static IntPtr[] swapchainViews;
        private static VkExtent2D surface_extent;

        public static GameWindow Window { get; private set; }
        public static bool EnableValidation { get; set; }
        public static string AppName { get; set; }


        static GraphicsDevice()
        {
            requiredDeviceExtns = new string[]
            {
                VkKhrSwapchainExtensionName,
                VkExtSubgroupSizeControlExtensionName
            };
        }

        private static bool ExtensionsSupported(IntPtr physDevice)
        {
            uint extn_cnt = 0;
            vkEnumerateDeviceExtensionProperties(physDevice, null, &extn_cnt, IntPtr.Zero);
            var availExtns_ptr = new ManagedPtrArray<VkExtensionProperties>(extn_cnt);
            vkEnumerateDeviceExtensionProperties(physDevice, null, &extn_cnt, availExtns_ptr);
            var availExtns = availExtns_ptr.Value;

            int avail_extns = requiredDeviceExtns.Length;
            for (int i = 0; i < availExtns.Length; i++)
                fixed (byte* b_p = availExtns[i].extensionName)
                {
                    var b_p_str = Encoding.ASCII.GetString(b_p, VkExtensionProperties.extensionName_len);
                    b_p_str = b_p_str.Substring(0, b_p_str.IndexOf('\0'));
                    if (requiredDeviceExtns.Contains(b_p_str))
                        avail_extns--;
                }

            return avail_extns == 0;
        }

        private static uint RateDevice(IntPtr physDevice)
        {
            var devProps = new ManagedPtr<VkPhysicalDeviceProperties>();
            var devFeats = new ManagedPtr<VkPhysicalDeviceFeatures>();

            vkGetPhysicalDeviceProperties(physDevice, devProps);
            vkGetPhysicalDeviceFeatures(physDevice, devFeats);

            var devProps_ = devProps.Value;
            var devFeats_ = devFeats.Value;

            uint score = 0;
            if (devProps_.deviceType == VkPhysicalDeviceType.PhysicalDeviceTypeDiscreteGpu)
                score += 100;

            score += devProps_.limits.maxImageDimension2D;

            if (!devFeats_.multiDrawIndirect)
                return 0;

            if (!devFeats_.tessellationShader)
                return 0;

            if (!ExtensionsSupported(physDevice))
                return 0;

            var caps_ptr = new ManagedPtr<VkSurfaceCapabilitiesKHR>();
            vkGetPhysicalDeviceSurfaceCapabilitiesKHR(physDevice, surfaceHndl, caps_ptr);
            var caps = caps_ptr.Value;

            uint fmt_cnt = 0;
            vkGetPhysicalDeviceSurfaceFormatsKHR(physDevice, surfaceHndl, &fmt_cnt, IntPtr.Zero);
            var fmts_ptr = new ManagedPtrArray<VkSurfaceFormatKHR>(fmt_cnt);
            vkGetPhysicalDeviceSurfaceFormatsKHR(physDevice, surfaceHndl, &fmt_cnt, fmts_ptr);
            var fmts = fmts_ptr.Value;

            bool fmt_valid = false;
            VkSurfaceFormatKHR chosen_fmt = new VkSurfaceFormatKHR();
            foreach (var avail_fmt in fmts)
                if (avail_fmt.format == VkFormat.FormatB8g8r8a8Unorm && avail_fmt.colorSpace == VkColorSpaceKHR.ColorSpaceSrgbNonlinearKhr)
                {
                    chosen_fmt = avail_fmt;
                    fmt_valid = true;
                    break;
                }
            if (!fmt_valid && fmts.Length > 0)
            {
                chosen_fmt = fmts[0];
                fmt_valid = true;
            }

            uint present_mode_cnt = 0;
            vkGetPhysicalDeviceSurfacePresentModesKHR(physDevice, surfaceHndl, &present_mode_cnt, null);
            var present_modes = stackalloc VkPresentModeKHR[(int)present_mode_cnt];
            vkGetPhysicalDeviceSurfacePresentModesKHR(physDevice, surfaceHndl, &present_mode_cnt, present_modes);

            bool present_valid = false;
            VkPresentModeKHR chosen_present = VkPresentModeKHR.PresentModeBeginRangeKhr;
            for (int i = 0; i < present_mode_cnt; i++)
            {
                var avail_present = present_modes[i];
                if (avail_present == VkPresentModeKHR.PresentModeMailboxKhr)
                {
                    present_valid = true;
                    chosen_present = VkPresentModeKHR.PresentModeMailboxKhr;
                }
                else if (avail_present == VkPresentModeKHR.PresentModeFifoRelaxedKhr)
                {
                    present_valid = true;
                    chosen_present = VkPresentModeKHR.PresentModeFifoRelaxedKhr;
                }
            }
            if (!present_valid)
            {
                present_valid = true;
                chosen_present = VkPresentModeKHR.PresentModeFifoKhr;
            }

            if (!fmt_valid)
                return 0;
            if (!present_valid)
                return 0;

            present_mode = chosen_present;
            surface_fmt = chosen_fmt;

            return score;
        }

        public static void Init()
        {
            Window = new GameWindow(AppName);
            fixed (IntPtr* instancePtr = &instanceHndl)
            fixed (IntPtr* surfacePtr = &surfaceHndl)
            {
                VkResult res;
                var instLayers = new List<string>();
                var instExtns = new List<string>();
                var devExtns = new List<string>();
                uint glfwExtnCnt = 0;
                var glfwExtns = glfwGetRequiredInstanceExtensions(&glfwExtnCnt);
                for (int i = 0; i < glfwExtnCnt; i++)
                    instExtns.Add(Marshal.PtrToStringAnsi(glfwExtns[i]));
                devExtns.AddRange(requiredDeviceExtns);
                if (EnableValidation)
                {
                    instLayers.Add("VK_LAYER_KHRONOS_validation");
                    instExtns.Add(VkExtDebugUtilsExtensionName);
                }

                var layers = stackalloc IntPtr[instLayers.Count];
                for (int i = 0; i < instLayers.Count; i++)
                    layers[i] = Marshal.StringToHGlobalAnsi(instLayers[i]);

                var extns = stackalloc IntPtr[instExtns.Count];
                for (int i = 0; i < instExtns.Count; i++)
                    extns[i] = Marshal.StringToHGlobalAnsi(instExtns[i]);

                var devExtns_ptr = stackalloc IntPtr[instExtns.Count];
                for (int i = 0; i < devExtns.Count; i++)
                    devExtns_ptr[i] = Marshal.StringToHGlobalAnsi(devExtns[i]);
                {
                    var appInfo =
                        new VkApplicationInfo()
                        {
                            sType = VkStructureType.StructureTypeApplicationInfo,
                            pApplicationName = AppName,
                            pEngineName = "KokoroVR",
                            apiVersion = VkApiVersion12,
                            applicationVersion = 1,
                            engineVersion = 1,
                            pNext = IntPtr.Zero
                        };
                    var appInfo_ptr = appInfo.Pointer();

                    var instCreatInfo = new VkInstanceCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeInstanceCreateInfo,
                        pApplicationInfo = appInfo_ptr,
                    };

                    instCreatInfo.ppEnabledLayerNames = layers;
                    instCreatInfo.enabledLayerCount = (uint)instLayers.Count;
                    instCreatInfo.ppEnabledExtensionNames = extns;
                    instCreatInfo.enabledExtensionCount = (uint)instExtns.Count;

                    var instCreatInfo_ptr = instCreatInfo.Pointer();

                    res = vkCreateInstance(instCreatInfo_ptr, IntPtr.Zero, instancePtr);
                    if (res != VkResult.Success)
                        throw new Exception("Failed to create instance.");
                }

                if (EnableValidation)
                {
                    //TODO register debug message handler
                }

                res = Window.CreateSurface(instanceHndl, surfacePtr);
                if (res != VkResult.Success)
                    throw new Exception("Failed to create surface.");

                uint devCount = 0;
                vkEnumeratePhysicalDevices(instanceHndl, &devCount, null);
                if (devCount == 0)
                    throw new Exception("Failed to find Vulkan compatible devices.");

                var devices = new IntPtr[devCount];
                fixed (IntPtr* devicesPtr = devices)
                    vkEnumeratePhysicalDevices(instanceHndl, &devCount, devicesPtr);

                var ratedDevices = new List<(uint, IntPtr)>();
                for (int i = 0; i < devices.Length; i++)
                    //rate each device
                    ratedDevices.Add((RateDevice(devices[i]), devices[i]));
                orderedDevices = ratedDevices.OrderByDescending(a => a.Item1)
                                             .Select(a => a.Item2)
                                             .ToArray();
                orderedDeviceData = new VulkanDevice[orderedDevices.Length];
                orderedDeviceData[0] = new VulkanDevice(orderedDevices[0]);
                //TODO for now just choose the first device

                {
                    //allocate queues for primary device
                    uint graphicsFamily = ~0u;
                    uint computeFamily = ~0u;
                    uint transferFamily = ~0u;
                    uint presentFamily = ~0u;

                    uint qfam_cnt = 0;
                    vkGetPhysicalDeviceQueueFamilyProperties(orderedDevices[0], &qfam_cnt, IntPtr.Zero);
                    var qFams_ptr = new ManagedPtrArray<VkQueueFamilyProperties>(qfam_cnt);
                    vkGetPhysicalDeviceQueueFamilyProperties(orderedDevices[0], &qfam_cnt, qFams_ptr);
                    var qFams = qFams_ptr.Value;

                    uint qFamIdx = 0;
                    foreach (var qFam in qFams)
                    {
                        bool presentSupport = false;
                        vkGetPhysicalDeviceSurfaceSupportKHR(orderedDevices[0], qFamIdx, surfaceHndl, &presentSupport);

                        if ((qFam.queueFlags & (uint)VkQueueFlagBits.QueueGraphicsBit) != 0 && graphicsFamily == ~0u)
                        {
                            graphicsFamily = qFamIdx;
                            if (presentSupport) presentFamily = qFamIdx;
                            if ((qFam.queueFlags & (uint)VkQueueFlagBits.QueueTransferBit) != 0)
                                transferFamily = qFamIdx;
                            qFamIdx++;
                            continue;
                        }

                        if ((qFam.queueFlags & (uint)VkQueueFlagBits.QueueComputeBit) != 0 && computeFamily == ~0u)
                        {
                            computeFamily = qFamIdx;
                            qFamIdx++;
                            continue;
                        }

                        if ((qFam.queueFlags & (uint)VkQueueFlagBits.QueueTransferBit) != 0)
                            transferFamily = qFamIdx;

                        if (graphicsFamily != ~0u && computeFamily != ~0u && transferFamily != ~0u && presentFamily != ~0u)
                            break;
                        qFamIdx++;
                    }

                    if (presentFamily == ~0u)
                        throw new Exception("Separate present queue support hasn't been implemented.");

                    var max_q_priority = stackalloc float[] { 1.0f };
                    var dual_graph_q_priority = stackalloc float[] { 1.0f };
                    var triple_graph_q_priority = stackalloc float[] { 1.0f, 1.0f, 1.0f };

                    VkDeviceQueueCreateInfo graphics_qCreatInfo = new VkDeviceQueueCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeDeviceQueueCreateInfo,
                        queueFamilyIndex = graphicsFamily,
                        queueCount = 1,
                        pQueuePriorities = max_q_priority
                    };

                    VkDeviceQueueCreateInfo compute_qCreatInfo = new VkDeviceQueueCreateInfo
                    {
                        sType = VkStructureType.StructureTypeDeviceQueueCreateInfo,
                        queueFamilyIndex = computeFamily,
                        queueCount = 1,
                        pQueuePriorities = max_q_priority
                    };

                    VkDeviceQueueCreateInfo transfer_qCreatInfo = new VkDeviceQueueCreateInfo();
                    if (transferFamily != graphicsFamily)
                    {
                        transfer_qCreatInfo.sType = VkStructureType.StructureTypeDeviceQueueCreateInfo;
                        transfer_qCreatInfo.queueFamilyIndex = transferFamily;
                        transfer_qCreatInfo.queueCount = 1;
                        transfer_qCreatInfo.pQueuePriorities = max_q_priority;
                    }
                    else
                    {
                        graphics_qCreatInfo.queueCount = 2;
                        graphics_qCreatInfo.pQueuePriorities = dual_graph_q_priority;
                    }


                    var qCreatInfos = new VkDeviceQueueCreateInfo[3];
                    qCreatInfos[0] = graphics_qCreatInfo;
                    qCreatInfos[1] = compute_qCreatInfo;
                    if (transferFamily != graphicsFamily) qCreatInfos[2] = transfer_qCreatInfo;

                    orderedDeviceData[0].graphicsFamily = graphicsFamily;
                    orderedDeviceData[0].computeFamily = computeFamily;
                    orderedDeviceData[0].transferFamily = transferFamily;
                    orderedDeviceData[0].presentFamily = presentFamily;

                    VkPhysicalDeviceFeatures devFeats = new VkPhysicalDeviceFeatures()
                    {
                        multiDrawIndirect = true,
                        tessellationShader = true,
                        fragmentStoresAndAtomics = true,
                        vertexPipelineStoresAndAtomics = true,
                        robustBufferAccess = EnableValidation,
                    };

                    var qCreatInfos_ptr = qCreatInfos.Pointer();
                    var devFeats_ptr = devFeats.Pointer();
                    var devCreatInfo = new VkDeviceCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeDeviceCreateInfo,
                        queueCreateInfoCount = (uint)(transferFamily != graphicsFamily ? 3 : 2),
                        enabledExtensionCount = (uint)devExtns.Count,
                        ppEnabledExtensionNames = devExtns_ptr,
                        enabledLayerCount = (uint)instLayers.Count,
                        ppEnabledLayerNames = layers,
                        pEnabledFeatures = devFeats_ptr,
                        pQueueCreateInfos = qCreatInfos_ptr,
                    };
                    var devCreatInfo_ptr = devCreatInfo.Pointer();
                    fixed (IntPtr* device_ptr = &orderedDeviceData[0].device)
                        res = vkCreateDevice(orderedDevices[0], devCreatInfo_ptr, IntPtr.Zero, device_ptr);
                    if (res != VkResult.Success)
                        throw new Exception("Failed to create logical device.");

                    //TODO Setup memory allocator

                    fixed (IntPtr* graph_q_hndl = &orderedDeviceData[0].graphicsQueue)
                        vkGetDeviceQueue(orderedDeviceData[0].device, graphicsFamily, 0, graph_q_hndl);

                    fixed (IntPtr* comp_q_hndl = &orderedDeviceData[0].computeQueue)
                        vkGetDeviceQueue(orderedDeviceData[0].device, computeFamily, 0, comp_q_hndl);
                    fixed (IntPtr* trans_q_hndl = &orderedDeviceData[0].transferQueue)
                        if (transferFamily != graphicsFamily)
                            vkGetDeviceQueue(orderedDeviceData[0].device, transferFamily, 0, trans_q_hndl);
                        else vkGetDeviceQueue(orderedDeviceData[0].device, graphicsFamily, 1, trans_q_hndl);

                    var caps_ptr = new ManagedPtr<VkSurfaceCapabilitiesKHR>();
                    vkGetPhysicalDeviceSurfaceCapabilitiesKHR(orderedDeviceData[0].physDevice, surfaceHndl, caps_ptr);
                    var caps = caps_ptr.Value;

                    VkExtent2D cur_extent = new VkExtent2D();
                    if (caps.currentExtent.width != uint.MaxValue)
                    {
                        cur_extent = caps.currentExtent;
                    }
                    else
                    {
                        cur_extent.width = Math.Clamp((uint)Window.Width, caps.minImageExtent.width, caps.maxImageExtent.width);
                        cur_extent.height = Math.Clamp((uint)Window.Height, caps.minImageExtent.height, caps.maxImageExtent.height);
                    }

                    uint img_cnt = caps.minImageCount + 1;

                    VkSwapchainCreateInfoKHR swapCreatInfo = new VkSwapchainCreateInfoKHR()
                    {
                        sType = VkStructureType.StructureTypeImageSwapchainCreateInfoKhr,
                        surface = surfaceHndl,
                        minImageCount = img_cnt,
                        imageFormat = surface_fmt.format,
                        imageColorSpace = surface_fmt.colorSpace,
                        imageExtent = cur_extent,
                        imageArrayLayers = 1,
                        imageUsage = (uint)VkImageUsageFlagBits.ImageUsageColorAttachmentBit,
                        imageSharingMode = VkSharingMode.SharingModeExclusive,
                        queueFamilyIndexCount = 0,
                        pQueueFamilyIndices = null,
                        preTransform = caps.currentTransform,
                        compositeAlpha = VkCompositeAlphaFlagBitsKHR.CompositeAlphaOpaqueBitKhr,
                        presentMode = present_mode,
                        clipped = true,
                        oldSwapchain = IntPtr.Zero
                    };
                    var swapCreatInfo_ptr = swapCreatInfo.Pointer();
                    fixed (IntPtr* swapchain_ptr = &swapChainHndl)
                        res = vkCreateSwapchainKHR(orderedDeviceData[0].device, swapCreatInfo_ptr, IntPtr.Zero, swapchain_ptr);
                    if (res != VkResult.Success)
                        throw new Exception("Failed to create swapchain.");

                    fixed (uint* swapchain_img_cnt_ptr = &swapchain_img_cnt)
                    {
                        vkGetSwapchainImagesKHR(orderedDeviceData[0].device, swapChainHndl, swapchain_img_cnt_ptr, null);
                        swapchainImages = new IntPtr[swapchain_img_cnt];
                        fixed (IntPtr* swapchain_imgs = swapchainImages)
                            vkGetSwapchainImagesKHR(orderedDeviceData[0].device, swapChainHndl, swapchain_img_cnt_ptr, swapchain_imgs);

                        surface_extent = cur_extent;
                        swapchainViews = new IntPtr[swapchain_img_cnt];
                        for (int i = 0; i < swapchainImages.Length; i++)
                        {
                            VkImageViewCreateInfo imgViewCreatInfo = new VkImageViewCreateInfo();
                            imgViewCreatInfo.sType = VkStructureType.StructureTypeImageViewCreateInfo;
                            imgViewCreatInfo.image = swapchainImages[i];
                            imgViewCreatInfo.viewType = VkImageViewType.ImageViewType2d;
                            imgViewCreatInfo.format = surface_fmt.format;
                            imgViewCreatInfo.components.r = VkComponentSwizzle.ComponentSwizzleIdentity;
                            imgViewCreatInfo.components.g = VkComponentSwizzle.ComponentSwizzleIdentity;
                            imgViewCreatInfo.components.b = VkComponentSwizzle.ComponentSwizzleIdentity;
                            imgViewCreatInfo.components.a = VkComponentSwizzle.ComponentSwizzleIdentity;
                            imgViewCreatInfo.subresourceRange.aspectMask = (uint)VkImageAspectFlagBits.ImageAspectColorBit;
                            imgViewCreatInfo.subresourceRange.baseMipLevel = 0;
                            imgViewCreatInfo.subresourceRange.baseArrayLayer = 0;
                            imgViewCreatInfo.subresourceRange.levelCount = 1;
                            imgViewCreatInfo.subresourceRange.layerCount = 1;

                            var imgViewCreatInfo_ptr = imgViewCreatInfo.Pointer();
                            fixed (IntPtr* swapchainView_ptr = &swapchainViews[i])
                                res = vkCreateImageView(orderedDeviceData[0].device, imgViewCreatInfo_ptr, IntPtr.Zero, swapchainView_ptr);
                            if (res != VkResult.Success)
                                throw new Exception("Failed to create image views.");
                        }
                    }

                    //TODO allocate compute and trasnfer queues for all secondary devices
                }
                for (int i = 0; i < instLayers.Count; i++)
                    Marshal.FreeHGlobal(layers[i]);
                for (int i = 0; i < instExtns.Count; i++)
                    Marshal.FreeHGlobal(extns[i]);
                for (int i = 0; i < devExtns.Count; i++)
                    Marshal.FreeHGlobal(devExtns_ptr[i]);
            }
        }
    }
}
