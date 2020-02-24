using System;
using System.Collections.Generic;
using System.Text;

using VulkanSharp.Raw;
using static VulkanSharp.Raw.Vk;
using static VulkanSharp.Raw.Vma;
using static VulkanSharp.Raw.Glfw;
using System.Runtime.InteropServices;
using System.Linq;

namespace Kokoro.Graphics
{
    class VulkanDevice
    {
        public IntPtr Device;
        public IntPtr PhysicalDevice;
        public uint GraphicsFamily;
        public uint ComputeFamily;
        public uint TransferFamily;
        public uint PresentFamily;

        public IntPtr GraphicsQueue;
        public IntPtr ComputeQueue;
        public IntPtr TransferQueue;
        public IntPtr PresentQueue;
        public IntPtr vmaAllocator;

        public uint[] QueueFamilyIndices;

        public VkPhysicalDeviceProperties Properties;

        public VulkanDevice(IntPtr physDevice)
        {
            this.PhysicalDevice = physDevice;
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
        private static Image[] swapchainImages;
        private static ImageView[] swapchainViews;
        private static Image[] depthImages;
        private static ImageView[] depthViews;
        private static GpuSemaphore frameFinishedSemaphore;
        private static GpuSemaphore imageAvailableSemaphore;
        private static VkExtent2D surface_extent;
        private static IntPtr debugMessenger;
        public const int MaxIndirectDrawsUBO = 256; //TODO: check if needed
        public const int MaxIndirectDrawsSSBO = 1024;
        public const int EyeCount = 1;
        public static GameWindow Window { get; private set; }
        public static bool EnableValidation { get; set; }
        public static string AppName { get; set; }
        public static Framebuffer[] DefaultFramebuffer { get; private set; }
        public static uint CurrentFrameIndex { get; private set; }

        static GraphicsDevice()
        {
            requiredDeviceExtns = new string[]
            {
                VkKhrSwapchainExtensionName,
                VkExtSubgroupSizeControlExtensionName
            };
        }

        #region Debug Management
        private static bool debugCallback(VkDebugUtilsMessageSeverityFlagsEXT severity, VkDebugUtilsMessageTypeFlagsEXT type, IntPtr callbackData, IntPtr userData)
        {
            var cbkData = Marshal.PtrToStructure<VkDebugUtilsMessengerCallbackDataEXT>(callbackData);
            var consoleCol = Console.BackgroundColor;
            var fgconsoleCol = Console.ForegroundColor;
            switch (severity)
            {
                case VkDebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt:
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case VkDebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt:
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case VkDebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt:
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    Console.ForegroundColor = ConsoleColor.Black;
                    break;
                case VkDebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityInfoBitExt:
                    Console.BackgroundColor = ConsoleColor.Green;
                    Console.ForegroundColor = ConsoleColor.Black;
                    break;
            }
            var typeStr = type switch
            {
                VkDebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeGeneralBitExt => "General",
                VkDebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypePerformanceBitExt => "Perf",
                VkDebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeValidationBitExt => "Validation",
                _ => "Unknown"
            };

            Console.Write($"[{typeStr}]");
            Console.ForegroundColor = fgconsoleCol;
            Console.BackgroundColor = consoleCol;
            Console.WriteLine($" {cbkData.pMessage}");
            return false;
        }

        private static VkResult CreateDebugUtilsMessengerEXT(IntPtr instance, ManagedPtr<VkDebugUtilsMessengerCreateInfoEXT> pCreateInfo, ManagedPtrArray<VkAllocationCallbacks> pAllocator, IntPtr* pDebugMessenger)
        {
            var func = Marshal.GetDelegateForFunctionPointer<PFN_vkCreateDebugUtilsMessengerEXT>(vkGetInstanceProcAddr(instance, "vkCreateDebugUtilsMessengerEXT"));
            if (func != null)
            {
                return func(instance, pCreateInfo, pAllocator, pDebugMessenger);
            }
            else
            {
                return VkResult.ErrorExtensionNotPresent;
            }
        }

        private static void DestroyDebugUtilsMessengerEXT(IntPtr instance, IntPtr debugMessenger, ManagedPtrArray<VkAllocationCallbacks> pAllocator)
        {
            Marshal.GetDelegateForFunctionPointer<PFN_vkDestroyDebugUtilsMessengerEXT>(vkGetInstanceProcAddr(instance, "vkDestroyDebugUtilsMessengerEXT"))?.Invoke(instance, debugMessenger, pAllocator);
        }
        #endregion

        #region Initialization
        private static bool ExtensionsSupported(IntPtr physDevice)
        {
            uint extn_cnt = 0;
            vkEnumerateDeviceExtensionProperties(physDevice, null, &extn_cnt, null);
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
            vkGetPhysicalDeviceSurfaceFormatsKHR(physDevice, surfaceHndl, &fmt_cnt, null);
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

                    res = vkCreateInstance(instCreatInfo_ptr, null, instancePtr);
                    if (res != VkResult.Success)
                        throw new Exception("Failed to create instance.");
                }

                if (EnableValidation)
                {
                    //register debug message handler
                    var debugCreatInfo = new VkDebugUtilsMessengerCreateInfoEXT()
                    {
                        sType = VkStructureType.StructureTypeDebugUtilsMessengerCreateInfoExt,
                        messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt | VkDebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt | VkDebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt | VkDebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityInfoBitExt,
                        messageType = VkDebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeGeneralBitExt | VkDebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypePerformanceBitExt | VkDebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeValidationBitExt,
                        pfnUserCallback = debugCallback
                    };

                    var debugCreatInfo_ptr = debugCreatInfo.Pointer();
                    fixed (IntPtr* dbg_ptr = &debugMessenger)
                        res = CreateDebugUtilsMessengerEXT(instanceHndl, debugCreatInfo_ptr, null, dbg_ptr);
                    if (res != VkResult.Success)
                        throw new Exception("Failed to register debug callback.");
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
                    vkGetPhysicalDeviceQueueFamilyProperties(orderedDevices[0], &qfam_cnt, null);
                    var qFams_ptr = new ManagedPtrArray<VkQueueFamilyProperties>(qfam_cnt);
                    vkGetPhysicalDeviceQueueFamilyProperties(orderedDevices[0], &qfam_cnt, qFams_ptr);
                    var qFams = qFams_ptr.Value;

                    uint qFamIdx = 0;
                    foreach (var qFam in qFams)
                    {
                        bool presentSupport = false;
                        vkGetPhysicalDeviceSurfaceSupportKHR(orderedDevices[0], qFamIdx, surfaceHndl, &presentSupport);

                        if ((qFam.queueFlags & VkQueueFlags.QueueGraphicsBit) != 0 && graphicsFamily == ~0u)
                        {
                            graphicsFamily = qFamIdx;
                            if (presentSupport) presentFamily = qFamIdx;
                            if ((qFam.queueFlags & VkQueueFlags.QueueTransferBit) != 0)
                                transferFamily = qFamIdx;
                            qFamIdx++;
                            continue;
                        }

                        if ((qFam.queueFlags & VkQueueFlags.QueueComputeBit) != 0 && computeFamily == ~0u)
                        {
                            computeFamily = qFamIdx;
                            qFamIdx++;
                            continue;
                        }

                        if ((qFam.queueFlags & VkQueueFlags.QueueTransferBit) != 0)
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

                    orderedDeviceData[0].GraphicsFamily = graphicsFamily;
                    orderedDeviceData[0].ComputeFamily = computeFamily;
                    orderedDeviceData[0].TransferFamily = transferFamily;
                    orderedDeviceData[0].PresentFamily = presentFamily;

                    VkPhysicalDeviceFeatures devFeats = new VkPhysicalDeviceFeatures()
                    {
                        multiDrawIndirect = true,
                        tessellationShader = true,
                        fragmentStoresAndAtomics = true,
                        vertexPipelineStoresAndAtomics = true,
                        robustBufferAccess = EnableValidation,
                    };

                    var devFeats12 = new VkPhysicalDeviceVulkan12Features()
                    {
                        separateDepthStencilLayouts = true
                    };

                    var qCreatInfos_ptr = qCreatInfos.Pointer();
                    var devFeats_ptr = devFeats.Pointer();
                    var devFeats12_ptr = devFeats12.Pointer();
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
                        pNext = devFeats12_ptr
                    };
                    var devCreatInfo_ptr = devCreatInfo.Pointer();
                    fixed (IntPtr* device_ptr = &orderedDeviceData[0].Device)
                        res = vkCreateDevice(orderedDevices[0], devCreatInfo_ptr, null, device_ptr);
                    if (res != VkResult.Success)
                        throw new Exception("Failed to create logical device.");

                    //TODO Setup memory allocator
                    var allocCreatInfo = new VmaAllocatorCreateInfo()
                    {
                        physicalDevice = orderedDeviceData[0].PhysicalDevice,
                        device = orderedDeviceData[0].Device
                    };
                    var allocCreatInfo_ptr = allocCreatInfo.Pointer();
                    fixed (IntPtr* vma_alloc_ptr = &orderedDeviceData[0].vmaAllocator)
                        res = vmaCreateAllocator(allocCreatInfo_ptr, vma_alloc_ptr);
                    if (res != VkResult.Success)
                        throw new Exception("Failed to initialize allocator.");

                    fixed (IntPtr* graph_q_hndl = &orderedDeviceData[0].GraphicsQueue)
                        vkGetDeviceQueue(orderedDeviceData[0].Device, graphicsFamily, 0, graph_q_hndl);

                    fixed (IntPtr* comp_q_hndl = &orderedDeviceData[0].ComputeQueue)
                        vkGetDeviceQueue(orderedDeviceData[0].Device, computeFamily, 0, comp_q_hndl);
                    fixed (IntPtr* trans_q_hndl = &orderedDeviceData[0].TransferQueue)
                        if (transferFamily != graphicsFamily)
                            vkGetDeviceQueue(orderedDeviceData[0].Device, transferFamily, 0, trans_q_hndl);
                        else vkGetDeviceQueue(orderedDeviceData[0].Device, graphicsFamily, 1, trans_q_hndl);

                    var queue_indices = new List<uint>();
                    if (!queue_indices.Contains(orderedDeviceData[0].GraphicsFamily)) queue_indices.Add(orderedDeviceData[0].GraphicsFamily);
                    if (!queue_indices.Contains(orderedDeviceData[0].ComputeFamily)) queue_indices.Add(orderedDeviceData[0].ComputeFamily);
                    if (!queue_indices.Contains(orderedDeviceData[0].PresentFamily)) queue_indices.Add(orderedDeviceData[0].PresentFamily);
                    if (!queue_indices.Contains(orderedDeviceData[0].TransferFamily)) queue_indices.Add(orderedDeviceData[0].TransferFamily);
                    orderedDeviceData[0].QueueFamilyIndices = queue_indices.ToArray();

                    var physDeviceProps = new ManagedPtr<VkPhysicalDeviceProperties>();
                    vkGetPhysicalDeviceProperties(orderedDeviceData[0].PhysicalDevice, physDeviceProps);
                    orderedDeviceData[0].Properties = physDeviceProps.Value;

                    var caps_ptr = new ManagedPtr<VkSurfaceCapabilitiesKHR>();
                    vkGetPhysicalDeviceSurfaceCapabilitiesKHR(orderedDeviceData[0].PhysicalDevice, surfaceHndl, caps_ptr);
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
                        sType = VkStructureType.StructureTypeSwapchainCreateInfoKhr,
                        surface = surfaceHndl,
                        minImageCount = img_cnt,
                        imageFormat = surface_fmt.format,
                        imageColorSpace = surface_fmt.colorSpace,
                        imageExtent = cur_extent,
                        imageArrayLayers = 1,
                        imageUsage = VkImageUsageFlags.ImageUsageColorAttachmentBit,
                        imageSharingMode = VkSharingMode.SharingModeExclusive,
                        queueFamilyIndexCount = 0,
                        pQueueFamilyIndices = null,
                        preTransform = caps.currentTransform,
                        compositeAlpha = VkCompositeAlphaFlagsKHR.CompositeAlphaOpaqueBitKhr,
                        presentMode = present_mode,
                        clipped = true,
                        oldSwapchain = IntPtr.Zero
                    };
                    var swapCreatInfo_ptr = swapCreatInfo.Pointer();
                    fixed (IntPtr* swapchain_ptr = &swapChainHndl)
                        res = vkCreateSwapchainKHR(orderedDeviceData[0].Device, swapCreatInfo_ptr, null, swapchain_ptr);
                    if (res != VkResult.Success)
                        throw new Exception("Failed to create swapchain.");

                    fixed (uint* swapchain_img_cnt_ptr = &swapchain_img_cnt)
                    {
                        vkGetSwapchainImagesKHR(orderedDeviceData[0].Device, swapChainHndl, swapchain_img_cnt_ptr, null);
                        var swapchainImages_l = new IntPtr[swapchain_img_cnt];
                        fixed (IntPtr* swapchain_imgs = swapchainImages_l)
                            vkGetSwapchainImagesKHR(orderedDeviceData[0].Device, swapChainHndl, swapchain_img_cnt_ptr, swapchain_imgs);

                        swapchainImages = new Image[swapchain_img_cnt];
                        depthImages = new Image[swapchain_img_cnt];
                        for (int i = 0; i < swapchainImages.Length; i++)
                        {
                            swapchainImages[i] = new Image()
                            {
                                Dimensions = 2,
                                Width = cur_extent.width,
                                Height = cur_extent.height,
                                Depth = 1,
                                Format = (ImageFormat)surface_fmt.format,
                                Layers = 1,
                                Levels = 1,
                                MemoryUsage = MemoryUsage.GpuOnly,
                                Usage = ImageUsage.Sampled,
                                InitialLayout = ImageLayout.Undefined,
                                Cubemappable = false,
                            };
                            swapchainImages[i].Build(0, swapchainImages_l[i]);

                            depthImages[i] = new Image()
                            {
                                Dimensions = 2,
                                Width = cur_extent.width,
                                Height = cur_extent.height,
                                Depth = 1,
                                Format = ImageFormat.Depth32f,
                                Layers = 1,
                                Levels = 1,
                                MemoryUsage = MemoryUsage.GpuOnly,
                                Usage = ImageUsage.Depth,
                                InitialLayout = ImageLayout.Undefined,
                                Cubemappable = false,
                            };
                            depthImages[i].Build(0);
                        }


                        surface_extent = cur_extent;
                        swapchainViews = new ImageView[swapchain_img_cnt];
                        depthViews = new ImageView[swapchain_img_cnt];
                        DefaultFramebuffer = new Framebuffer[swapchain_img_cnt];
                        for (int i = 0; i < swapchainImages.Length; i++)
                        {
                            swapchainViews[i] = new ImageView()
                            {
                                BaseLayer = 0,
                                BaseLevel = 0,
                                Format = (ImageFormat)surface_fmt.format,
                                LayerCount = 1,
                                LevelCount = 1,
                                ViewType = ImageViewType.View2D
                            };
                            swapchainViews[i].Build(swapchainImages[i]);

                            depthViews[i] = new ImageView()
                            {
                                BaseLayer = 0,
                                BaseLevel = 0,
                                Format = ImageFormat.Depth32f,
                                LayerCount = 1,
                                LevelCount = 1,
                                ViewType = ImageViewType.View2D
                            };
                            depthViews[i].Build(depthImages[i]);

                            DefaultFramebuffer[i] = new Framebuffer(surface_extent.width, surface_extent.height);
                            DefaultFramebuffer[i][AttachmentKind.ColorAttachment0] = swapchainViews[i];
                            DefaultFramebuffer[i][AttachmentKind.DepthAttachment] = depthViews[i];
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

                frameFinishedSemaphore = new GpuSemaphore();
                frameFinishedSemaphore.Build(0);

                imageAvailableSemaphore = new GpuSemaphore();
                imageAvailableSemaphore.Build(0);

            }
        }
        #endregion

        #region Device Management
        internal static IntPtr[] GetDevices()
        {
            return new IntPtr[] { orderedDeviceData[0].Device };
        }

        internal static VulkanDevice GetDeviceInfo(int devID)
        {
            return orderedDeviceData[devID];
        }

        internal static void WaitIdle(int devID)
        {
            vkDeviceWaitIdle(orderedDeviceData[devID].Device);
        }
        #endregion

        #region Memory Management
        internal static VkResult CreateImage(int devID, ManagedPtr<VkImageCreateInfo> imgCreatInfo, ManagedPtr<VmaAllocationCreateInfo> allocCreatInfo, out IntPtr imgPtr, out IntPtr imgAllocPtr, ManagedPtr<VmaAllocationInfo> imgAllocInfo)
        {
            IntPtr imgPtr_l = IntPtr.Zero;
            IntPtr imgAllocPtr_l = IntPtr.Zero;
            var res = vmaCreateImage(orderedDeviceData[devID].vmaAllocator, imgCreatInfo, allocCreatInfo, &imgPtr_l, &imgAllocPtr_l, imgAllocInfo);
            imgPtr = imgPtr_l;
            imgAllocPtr = imgAllocPtr_l;
            return res;
        }

        internal static void DestroyImage(int devID, IntPtr imgPtr, IntPtr allocPtr)
        {
            vmaDestroyImage(orderedDeviceData[devID].vmaAllocator, imgPtr, allocPtr);
        }

        internal static VkResult CreateBuffer(int devID, ManagedPtr<VkBufferCreateInfo> imgCreatInfo, ManagedPtr<VmaAllocationCreateInfo> allocCreatInfo, out IntPtr imgPtr, out IntPtr imgAllocPtr, ManagedPtr<VmaAllocationInfo> imgAllocInfo)
        {
            IntPtr imgPtr_l = IntPtr.Zero;
            IntPtr imgAllocPtr_l = IntPtr.Zero;
            var res = vmaCreateBuffer(orderedDeviceData[devID].vmaAllocator, imgCreatInfo, allocCreatInfo, &imgPtr_l, &imgAllocPtr_l, imgAllocInfo);
            imgPtr = imgPtr_l;
            imgAllocPtr = imgAllocPtr_l;
            return res;
        }

        internal static void DestroyBuffer(int devID, IntPtr imgPtr, IntPtr allocPtr)
        {
            vmaDestroyBuffer(orderedDeviceData[devID].vmaAllocator, imgPtr, allocPtr);
        }
        #endregion


        #region Frame
        public static void AcquireFrame()
        {
            uint imgIdx = 0;
            vkAcquireNextImageKHR(orderedDeviceData[0].Device, swapChainHndl, ulong.MaxValue, imageAvailableSemaphore.semaphorePtr, IntPtr.Zero, &imgIdx);
            CurrentFrameIndex = imgIdx;
        }
        public static void PresentFrame()
        {
            var waitSemaphores = stackalloc IntPtr[] { frameFinishedSemaphore.semaphorePtr };
            var waitSwapchains = stackalloc IntPtr[] { swapChainHndl };
            var waitFrameIdx = stackalloc uint[] { CurrentFrameIndex };

            var presentInfo = new VkPresentInfoKHR()
            {
                sType = VkStructureType.StructureTypePresentInfoKhr,
                waitSemaphoreCount = 1,
                pWaitSemaphores = waitSemaphores,
                swapchainCount = 1,
                pSwapchains = waitSwapchains,
                pImageIndices = waitFrameIdx,
            };

            var presentInfo_ptr = presentInfo.Pointer();
            vkQueuePresentKHR(orderedDeviceData[0].GraphicsQueue, presentInfo_ptr);
        }
        #endregion

        #region Submit
        public static void SubmitCommandBuffer(CommandBuffer buffer)
        {
            var waitSemaphores = stackalloc IntPtr[] { imageAvailableSemaphore.semaphorePtr };
            var signalSemaphores = stackalloc IntPtr[] { frameFinishedSemaphore.semaphorePtr };
            var waitStages = stackalloc VkPipelineStageFlags[] { VkPipelineStageFlags.PipelineStageTopOfPipeBit };
            var cmdBuffers = stackalloc IntPtr[] { buffer.cmdBufferPtr };

            var submitInfo = new VkSubmitInfo()
            {
                sType = VkStructureType.StructureTypeSubmitInfo,
                waitSemaphoreCount = 1,
                pWaitSemaphores = waitSemaphores,
                pWaitDstStageMask = waitStages,
                commandBufferCount = 1,
                pCommandBuffers = cmdBuffers,
                signalSemaphoreCount = 1,
                pSignalSemaphores = signalSemaphores
            };
            if (vkQueueSubmit(buffer.cmdPool.queueFam, 1, submitInfo.Pointer(), IntPtr.Zero) != VkResult.Success)
                throw new Exception("Failed to submit command buffer.");
        }
        #endregion
    }
}
