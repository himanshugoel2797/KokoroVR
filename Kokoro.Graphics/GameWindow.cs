using System.Threading;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;
using static VulkanSharp.Raw.Glfw;
using static VulkanSharp.Raw.Vk;
using KokoroVR2.Input;

namespace Kokoro.Graphics
{
    public unsafe class GameWindow
    {
        private IntPtr* windowHndl;
        private string text, fps;

        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                text = value;
                glfwSetWindowTitle(windowHndl, text + fps);
            }
        }
        public bool ShowFPS { get; set; } = true;
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

        public double MouseX
        {
            get
            {
                double x = 0;
                glfwGetCursorPos(windowHndl, &x, null);
                return x;
            }
        }

        public double MouseY
        {
            get
            {
                double y = 0;
                glfwGetCursorPos(windowHndl, null, &y);
                return y;
            }
        }

        public bool LeftDown
        {
            get
            {
                return glfwGetMouseButton(windowHndl, GlfwMouseButtonLeft) == GlfwPress;
            }
        }

        public bool RightDown
        {
            get
            {
                return glfwGetMouseButton(windowHndl, GlfwMouseButtonRight) == GlfwPress;
            }
        }

        public bool MiddleDown
        {
            get
            {
                return glfwGetMouseButton(windowHndl, GlfwMouseButtonMiddle) == GlfwPress;
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

                text = AppName;
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
            var prevTime = s.Elapsed.TotalMilliseconds;
            Thread.Sleep(5);
            var sTime = s.Elapsed.TotalMilliseconds;
            Render(sTime, sTime - prevTime);
            while (!IsExiting)
            {
                sTime = s.Elapsed.TotalMilliseconds;
                if (Width != prevWidth | Height != prevHeight)
                {
                    if(Resized != null)Resized(Width, Height);
                    prevWidth = Width;
                    prevHeight = Height;
                }
                PollEvents();
                Update(sTime, sTime - prevTime);
                Render(sTime, sTime - prevTime);
                if (fps > 0)
                    while ((s.ElapsedMilliseconds - prevTime) < 1000.0f / fps - 0.5f)
                        Thread.Sleep(1);
                if (ShowFPS)
                {
                    this.fps = $" | {s.Elapsed.TotalMilliseconds - sTime}ms";
                    glfwSetWindowTitle(windowHndl, text + this.fps);
                    //Console.WriteLine("Poll Time: " + (s.Elapsed.TotalMilliseconds - sTime) + "ms");
                }
                prevTime = sTime;
            }
        }

        public void SetMousePos(double x, double y)
        {
            glfwSetCursorPos(windowHndl, x, y);
        }

        public bool IsKeyDown(Key k)
        {
            return glfwGetKey(windowHndl, (int)k) == GlfwPress;
        }

        public bool IsKeyUp(Key k)
        {
            return glfwGetKey(windowHndl, (int)k) == GlfwRelease;
        }

        internal VkResult CreateSurface(IntPtr instanceHndl, IntPtr* surfacePtr)
        {
            return glfwCreateWindowSurface(instanceHndl, windowHndl, null, surfacePtr);
        }
    }
}
