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
    public struct DeviceInfo
    {
        public IntPtr Device { get; internal set; }
        public IntPtr PhysicalDevice { get; internal set; }
        public uint GraphicsFamily { get; internal set; }
        public uint ComputeFamily { get; internal set; }
        public uint TransferFamily { get; internal set; }
        public uint PresentFamily { get; internal set; }
        public uint[] QueueFamilyIndices { get; internal set; }
        public GpuQueue GraphicsQueue { get; internal set; }
        public GpuQueue ComputeQueue { get; internal set; }
        public GpuQueue TransferQueue { get; internal set; }
        public GpuQueue PresentQueue { get; internal set; }

        internal IntPtr vmaAllocator;
        internal VkPhysicalDeviceProperties Properties;
    }

    public static unsafe class GraphicsDevice
    {
        private static IntPtr instanceHndl;
        private static IntPtr surfaceHndl;
        private static IntPtr swapChainHndl;
        private static VkDebugUtilsMessengerCreateInfoEXT debugCreatInfo;
        private static readonly string[] requiredDeviceExtns;
        private static VkPresentModeKHR present_mode;
        private static VkSurfaceFormatKHR surface_fmt;
        private static uint swapchain_img_cnt;
        private static Image[] swapchainImages;
        private static ImageView[] swapchainViews;
        private static Image[] depthImages;
        private static ImageView[] depthViews;
        private static GpuSemaphore[] frameFinishedSemaphore;
        private static GpuSemaphore[] imageAvailableSemaphore;
        private static Fence[] inflightFences;
        private static VkExtent2D surface_extent;
        private static IntPtr debugMessenger;
        public const int MaxIndirectDrawsUBO = 256; //TODO: check if needed
        public const int MaxIndirectDrawsSSBO = 1024;
        public const int EyeCount = 1;
        public static DeviceInfo[] DeviceInformation { get; private set; }
        public static GameWindow Window { get; private set; }
        public static bool EnableValidation { get; set; }
        public static string AppName { get; set; }
        public static Framebuffer[] DefaultFramebuffer { get; private set; }
        public static uint CurrentFrameIndex { get; private set; }
        public static uint MaxFrameCount { get; private set; }
        public static uint MaxFramesInFlight { get; private set; } = 3;
        public static uint CurrentFrameNumber { get; private set; }


        static GraphicsDevice()
        {
            requiredDeviceExtns = new string[]
            {
                VkKhrSwapchainExtensionName,
                VkExtSubgroupSizeControlExtensionName
            };
        }

        #region Debug Management
        private static bool DebugCallback(VkDebugUtilsMessageSeverityFlagsEXT severity, VkDebugUtilsMessageTypeFlagsEXT type, IntPtr callbackData, IntPtr userData)
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
                    debugCreatInfo = new VkDebugUtilsMessengerCreateInfoEXT()
                    {
                        sType = VkStructureType.StructureTypeDebugUtilsMessengerCreateInfoExt,
                        messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt | VkDebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt | VkDebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt | VkDebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityInfoBitExt,
                        messageType = VkDebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeGeneralBitExt | VkDebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypePerformanceBitExt | VkDebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeValidationBitExt,
                        pfnUserCallback = DebugCallback
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
                var orderedDevices = ratedDevices.OrderByDescending(a => a.Item1)
                                             .Select(a => a.Item2)
                                             .ToArray();
                DeviceInformation = new DeviceInfo[orderedDevices.Length];
                DeviceInformation[0] = new DeviceInfo()
                {
                    PhysicalDevice = orderedDevices[0]
                };
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
                            qFamIdx++;
                            continue;
                        }

                        if ((qFam.queueFlags & VkQueueFlags.QueueComputeBit) != 0 && computeFamily == ~0u)
                        {
                            computeFamily = qFamIdx;
                            if ((qFam.queueFlags & VkQueueFlags.QueueTransferBit) != 0) transferFamily = qFamIdx;
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

                    DeviceInformation[0].GraphicsFamily = graphicsFamily;
                    DeviceInformation[0].ComputeFamily = computeFamily;
                    DeviceInformation[0].TransferFamily = transferFamily;
                    DeviceInformation[0].PresentFamily = presentFamily;

                    VkPhysicalDeviceFeatures devFeats = new VkPhysicalDeviceFeatures()
                    {
                        multiDrawIndirect = true,
                        drawIndirectFirstInstance = true,
                        fullDrawIndexUint32 = true,
                        tessellationShader = true,
                        fragmentStoresAndAtomics = true,
                        vertexPipelineStoresAndAtomics = true,
                        robustBufferAccess = EnableValidation,
                        shaderInt16 = true,
                        samplerAnisotropy = true,
                    };

                    var devFeats11 = new VkPhysicalDeviceVulkan11Features()
                    {
                        sType = VkStructureType.StructureTypePhysicalDeviceVulkan11Features,
                        shaderDrawParameters = true,
                        storageBuffer16BitAccess = true,
                    };
                    var devFeats11_ptr = devFeats11.Pointer();

                    var devFeats12 = new VkPhysicalDeviceVulkan12Features()
                    {
                        sType = VkStructureType.StructureTypePhysicalDeviceVulkan12Features,
                        separateDepthStencilLayouts = true,
                        timelineSemaphore = true,
                        descriptorIndexing = true,
                        drawIndirectCount = true,
                        shaderFloat16 = true,
                        shaderInt8 = true,
                        uniformBufferStandardLayout = true,
                        storageBuffer8BitAccess = true,
                        shaderUniformBufferArrayNonUniformIndexing = true,
                        pNext = devFeats11_ptr
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
                    IntPtr deviceHndl = IntPtr.Zero;
                    res = vkCreateDevice(orderedDevices[0], devCreatInfo_ptr, null, &deviceHndl);
                    if (res != VkResult.Success)
                        throw new Exception("Failed to create logical device.");
                    DeviceInformation[0].Device = deviceHndl;

                    //TODO Setup memory allocator
                    var allocCreatInfo = new VmaAllocatorCreateInfo()
                    {
                        physicalDevice = DeviceInformation[0].PhysicalDevice,
                        device = DeviceInformation[0].Device
                    };
                    var allocCreatInfo_ptr = allocCreatInfo.Pointer();
                    fixed (IntPtr* vma_alloc_ptr = &DeviceInformation[0].vmaAllocator)
                        res = vmaCreateAllocator(allocCreatInfo_ptr, vma_alloc_ptr);
                    if (res != VkResult.Success)
                        throw new Exception("Failed to initialize allocator.");

                    IntPtr graph_q_hndl = IntPtr.Zero;
                    IntPtr trans_q_hndl = IntPtr.Zero;
                    IntPtr comp_q_hndl = IntPtr.Zero;
                    vkGetDeviceQueue(DeviceInformation[0].Device, graphicsFamily, 0, &graph_q_hndl);
                    vkGetDeviceQueue(DeviceInformation[0].Device, computeFamily, 0, &comp_q_hndl);
                    if (transferFamily != graphicsFamily)
                        vkGetDeviceQueue(DeviceInformation[0].Device, transferFamily, 0, &trans_q_hndl);
                    else vkGetDeviceQueue(DeviceInformation[0].Device, graphicsFamily, 1, &trans_q_hndl);

                    DeviceInformation[0].GraphicsQueue = new GpuQueue(CommandQueueKind.Graphics, graph_q_hndl, graphicsFamily, 0);
                    DeviceInformation[0].TransferQueue = new GpuQueue(CommandQueueKind.Transfer, trans_q_hndl, transferFamily, 0);
                    DeviceInformation[0].ComputeQueue = new GpuQueue(CommandQueueKind.Compute, comp_q_hndl, computeFamily, 0);

                    var queue_indices = new List<uint>();
                    if (!queue_indices.Contains(DeviceInformation[0].GraphicsFamily)) queue_indices.Add(DeviceInformation[0].GraphicsFamily);
                    if (!queue_indices.Contains(DeviceInformation[0].ComputeFamily)) queue_indices.Add(DeviceInformation[0].ComputeFamily);
                    if (!queue_indices.Contains(DeviceInformation[0].PresentFamily)) queue_indices.Add(DeviceInformation[0].PresentFamily);
                    if (!queue_indices.Contains(DeviceInformation[0].TransferFamily)) queue_indices.Add(DeviceInformation[0].TransferFamily);
                    DeviceInformation[0].QueueFamilyIndices = queue_indices.ToArray();

                    var physDeviceProps = new ManagedPtr<VkPhysicalDeviceProperties>();
                    vkGetPhysicalDeviceProperties(DeviceInformation[0].PhysicalDevice, physDeviceProps);
                    DeviceInformation[0].Properties = physDeviceProps.Value;

                    var caps_ptr = new ManagedPtr<VkSurfaceCapabilitiesKHR>();
                    vkGetPhysicalDeviceSurfaceCapabilitiesKHR(DeviceInformation[0].PhysicalDevice, surfaceHndl, caps_ptr);
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
                        res = vkCreateSwapchainKHR(DeviceInformation[0].Device, swapCreatInfo_ptr, null, swapchain_ptr);
                    if (res != VkResult.Success)
                        throw new Exception("Failed to create swapchain.");

                    fixed (uint* swapchain_img_cnt_ptr = &swapchain_img_cnt)
                    {
                        vkGetSwapchainImagesKHR(DeviceInformation[0].Device, swapChainHndl, swapchain_img_cnt_ptr, null);
                        var swapchainImages_l = new IntPtr[swapchain_img_cnt];
                        fixed (IntPtr* swapchain_imgs = swapchainImages_l)
                            vkGetSwapchainImagesKHR(DeviceInformation[0].Device, swapChainHndl, swapchain_img_cnt_ptr, swapchain_imgs);

                        MaxFramesInFlight = swapchain_img_cnt;
                        MaxFrameCount = swapchain_img_cnt;
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

                frameFinishedSemaphore = new GpuSemaphore[MaxFramesInFlight];
                imageAvailableSemaphore = new GpuSemaphore[MaxFramesInFlight];
                inflightFences = new Fence[MaxFramesInFlight];
                for (int i = 0; i < MaxFramesInFlight; i++)
                {
                    frameFinishedSemaphore[i] = new GpuSemaphore();
                    frameFinishedSemaphore[i].Build(0, false, 0);

                    imageAvailableSemaphore[i] = new GpuSemaphore();
                    imageAvailableSemaphore[i].Build(0, false, 0);

                    inflightFences[i] = new Fence
                    {
                        CreateSignaled = true
                    };
                    inflightFences[i].Build(0);
                }
            }
        }
        #endregion

        #region Device Management
        internal static IntPtr[] GetDevices()
        {
            return new IntPtr[] { DeviceInformation[0].Device };
        }

        internal static DeviceInfo GetDeviceInfo(int devID)
        {
            return DeviceInformation[devID];
        }

        internal static void WaitIdle(int devID)
        {
            vkDeviceWaitIdle(DeviceInformation[devID].Device);
        }
        #endregion

        #region Memory Management
        internal static VkResult CreateImage(int devID, ManagedPtr<VkImageCreateInfo> imgCreatInfo, ManagedPtr<VmaAllocationCreateInfo> allocCreatInfo, out IntPtr imgPtr, out IntPtr imgAllocPtr, ManagedPtr<VmaAllocationInfo> imgAllocInfo)
        {
            IntPtr imgPtr_l = IntPtr.Zero;
            IntPtr imgAllocPtr_l = IntPtr.Zero;
            var res = vmaCreateImage(DeviceInformation[devID].vmaAllocator, imgCreatInfo, allocCreatInfo, &imgPtr_l, &imgAllocPtr_l, imgAllocInfo);
            imgPtr = imgPtr_l;
            imgAllocPtr = imgAllocPtr_l;
            return res;
        }

        internal static void DestroyImage(int devID, IntPtr imgPtr, IntPtr allocPtr)
        {
            vmaDestroyImage(DeviceInformation[devID].vmaAllocator, imgPtr, allocPtr);
        }

        internal static VkResult CreateBuffer(int devID, ManagedPtr<VkBufferCreateInfo> imgCreatInfo, ManagedPtr<VmaAllocationCreateInfo> allocCreatInfo, out IntPtr imgPtr, out IntPtr imgAllocPtr, ManagedPtr<VmaAllocationInfo> imgAllocInfo)
        {
            IntPtr imgPtr_l = IntPtr.Zero;
            IntPtr imgAllocPtr_l = IntPtr.Zero;
            var res = vmaCreateBuffer(DeviceInformation[devID].vmaAllocator, imgCreatInfo, allocCreatInfo, &imgPtr_l, &imgAllocPtr_l, imgAllocInfo);
            imgPtr = imgPtr_l;
            imgAllocPtr = imgAllocPtr_l;
            return res;
        }

        internal static void DestroyBuffer(int devID, IntPtr imgPtr, IntPtr allocPtr)
        {
            vmaDestroyBuffer(DeviceInformation[devID].vmaAllocator, imgPtr, allocPtr);
        }
        #endregion

        #region Submit
        public static void SubmitGraphicsCommandBuffer(CommandBuffer buffer)
        {
            DeviceInformation[0].GraphicsQueue.SubmitCommandBuffer(buffer, new GpuSemaphore[] {
                imageAvailableSemaphore[CurrentFrameNumber]
            }, new GpuSemaphore[] {
                frameFinishedSemaphore[CurrentFrameNumber]
            },
            inflightFences[CurrentFrameNumber]);
        }
        #endregion

        #region Frame
        public static void AcquireFrame()
        {
            inflightFences[CurrentFrameNumber].Wait();
            inflightFences[CurrentFrameNumber].Reset();

            uint imgIdx = 0;
            vkAcquireNextImageKHR(DeviceInformation[0].Device, swapChainHndl, ulong.MaxValue, imageAvailableSemaphore[CurrentFrameNumber].semaphorePtr, IntPtr.Zero, &imgIdx);
            CurrentFrameIndex = imgIdx;
        }
        public static void PresentFrame()
        {
            var waitSemaphores = stackalloc IntPtr[] { frameFinishedSemaphore[CurrentFrameNumber].semaphorePtr };
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
            vkQueuePresentKHR(DeviceInformation[0].GraphicsQueue.Handle, presentInfo_ptr);
            CurrentFrameNumber = (CurrentFrameNumber + 1) % MaxFramesInFlight;
        }
        #endregion
    }
}
