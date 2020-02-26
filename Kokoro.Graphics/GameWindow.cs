using System.Threading;
using System.Diagnostics;
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

        private int prevWidth, prevHeight;

        public delegate void FrameHandler(double time_ms, double delta_ms);
        public delegate void ResizeHandler(int nWidth, int nHeight);
        public event FrameHandler Render;
        public event FrameHandler Update;
        public event ResizeHandler Resized;

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

        public bool IsExiting
        {
            get
            {
                unsafe
                {
                    return glfwWindowShouldClose(windowHndl) != 0;
                }
            }
        }

        public void PollEvents()
        {
            unsafe
            {
                glfwPollEvents();
            }
        }

        public void Run(int fps)
        {
            prevWidth = Width;
            prevHeight = Height;

            Stopwatch s = Stopwatch.StartNew();
            var sTime = s.Elapsed.TotalMilliseconds;
            Thread.Sleep(5);
            while (!IsExiting)
            {
                if (Width != prevWidth | Height != prevHeight)
                {
                    Resized(Width, Height);
                    prevWidth = Width;
                    prevHeight = Height;
                }
                PollEvents();
                Update(s.ElapsedMilliseconds, s.ElapsedMilliseconds - sTime);
                Render(s.ElapsedMilliseconds, s.ElapsedMilliseconds - sTime);
                if (fps > 0)
                    while ((s.ElapsedMilliseconds - sTime) < 1000.0f / fps - 0.5f)
                        Thread.Sleep(1);
                sTime = s.ElapsedMilliseconds;
            }
        }

        internal VkResult CreateSurface(IntPtr instanceHndl, IntPtr* surfacePtr)
        {
            return glfwCreateWindowSurface(instanceHndl, windowHndl, null, surfacePtr);
        }
    }
}
