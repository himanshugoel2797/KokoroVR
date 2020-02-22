using System;
using System.Collections.Generic;
using System.Text;
using static VulkanSharp.Raw.Glfw;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public unsafe class GameWindow
    {
        private IntPtr* windowHndl;
        public int Width
        {
            get
            {
                int w = 0;
                glfwGetWindowSize(windowHndl, &w, null);
                return w;
            }
        }
        public int Height
        {
            get
            {
                int h = 0;
                glfwGetWindowSize(windowHndl, null, &h);
                return h;
            }
        }

        public GameWindow(string AppName)
        {
            unsafe
            {
                glfwInit();
                glfwWindowHint(GlfwClientApi, GlfwNoApi);
                glfwWindowHint(GlfwResizable, GlfwFalse);

                windowHndl = glfwCreateWindow(1024, 1024, AppName, null, null);
            }
        }

        internal VkResult CreateSurface(IntPtr instanceHndl, IntPtr* surfacePtr)
        {
            return glfwCreateWindowSurface(instanceHndl, windowHndl, null, surfacePtr);
        }

    }
}
