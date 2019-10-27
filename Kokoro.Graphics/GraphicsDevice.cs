﻿using Kokoro.Math;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GameWindow = OpenTK.GameWindow;
using FrameEventArgs = OpenTK.FrameEventArgs;
using VSyncMode = OpenTK.VSyncMode;
using System.Collections.Concurrent;
using OpenTK.Graphics;
using System.IO;
using System.Drawing;
using Kokoro.Common;
using Kokoro.Common.StateMachine;

namespace Kokoro.Graphics
{
    public enum FaceWinding
    {
        Clockwise = 2304,
        CounterClockwise = 2305
    }

    public static class GraphicsDevice
    {
        static VertexArray curVarray;
        static ShaderProgram curProg;
        static Framebuffer curFramebuffer;
        static GameWindow game;

        static FaceWinding winding;
        public static FaceWinding Winding
        {
            get
            {
                return winding;
            }
            set
            {
                winding = value;
                GL.FrontFace((FrontFaceDirection)winding);
            }
        }

        public const int MaxIndirectDrawsUBO = 256;
        public const int MaxIndirectDrawsSSBO = 1024;

        public static Size WindowSize
        {
            get
            {
                return new Size(game.Width, game.Height);
            }
            set
            {
                game.Width = value.Width;
                game.Height = value.Height;
            }
        }

        static Vector4 clearColor;
        public static Vector4 ClearColor
        {
            get
            {
                return clearColor;
            }
            set
            {
                clearColor = value;
                GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
            }
        }

        static string gameName;
        public static string Name
        {
            get
            {
                return gameName;
            }
            set
            {
                gameName = value;
                if (Window != null) Window.Title = gameName;
            }
        }


        public static StateGroup GameLoop { get; set; }

        public static Action Load { get; set; }
        public static Action<double> Render
        {
            get
            {
                return GameLoop?.Render;
            }
            set
            {
                if (GameLoop != null)
                    GameLoop.Render = value;
            }
        }
        public static Action<double> Update
        {
            get
            {
                return GameLoop?.Update;
            }
            set
            {
                if (GameLoop != null)
                    GameLoop.Update = value;
            }
        }
        public static WeakAction Cleanup { get; set; }
        public static Action CleanupStrong { get; set; }
        public static OpenTK.Input.KeyboardDevice Keyboard { get { return game.Keyboard; } }
        public static OpenTK.Input.MouseDevice Mouse { get { return game.Mouse; } }
        public static GameWindow Window
        {
            get
            {
                return game;
            }
        }
        public static int PatchCount
        {
            set
            {
                GL.PatchParameter(PatchParameterInt.PatchVertices, value);
            }
        }

        static bool wframe = false;
        public static bool Wireframe
        {
            set
            {
                if (wframe != value)
                {
                    wframe = value;
                    GL.PolygonMode(MaterialFace.FrontAndBack, value ? PolygonMode.Line : PolygonMode.Fill);
                }
            }
            get
            {
                return wframe;
            }
        }

        static bool aEnabled = false;
        public static bool AlphaEnabled
        {
            get
            {
                return aEnabled;
            }
            set
            {
                if (aEnabled != value)
                {
                    aEnabled = value;
                    if (aEnabled)
                    {
                        GL.Enable(EnableCap.Blend);
                        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                    }
                    else
                    {
                        GL.Disable(EnableCap.Blend);
                    }
                }
            }
        }

        static CullFaceMode cullMode = CullFaceMode.None;
        public static CullFaceMode CullMode
        {
            get
            {
                return cullMode;
            }
            set
            {
                if (cullMode != value)
                {
                    cullMode = value;
                    if (cullMode == CullFaceMode.None) CullEnabled = false;
                    else CullEnabled = true;

                    if (cullMode != CullFaceMode.None)
                    {
                        GL.CullFace((OpenTK.Graphics.OpenGL4.CullFaceMode)cullMode);
                    }
                }
            }
        }

        static bool cullEnabled = false;
        public static bool CullEnabled
        {
            get
            {
                return cullEnabled;
            }
            set
            {
                if (cullEnabled != value)
                {
                    if (value)
                        GL.Enable(EnableCap.CullFace);
                    else
                        GL.Disable(EnableCap.CullFace);
                    cullEnabled = value;
                }
            }
        }

        static DepthFunc dFunc = DepthFunc.None;
        public static DepthFunc DepthTest
        {
            get
            {
                return dFunc;
            }
            set
            {
                if (dFunc != value)
                {
                    dFunc = value;
                    GL.DepthFunc((DepthFunction)value);
                }
            }
        }

        static float clearDepth = float.NaN;
        public static float ClearDepth
        {
            get
            {
                return clearDepth;
            }
            set
            {
                if (clearDepth != value)
                {
                    clearDepth = value;
                    GL.ClearDepth(value);
                }
            }
        }

        static BlendFactor alphaSrc = BlendFactor.One;
        public static BlendFactor AlphaSrc
        {
            get
            {
                return alphaSrc;
            }
            set
            {
                if (alphaSrc != value)
                {
                    alphaSrc = value;
                    GL.BlendFunc((BlendingFactorSrc)alphaSrc, (BlendingFactorDest)alphaDst);
                }
            }
        }

        static BlendFactor alphaDst = BlendFactor.One;
        public static BlendFactor AlphaDst
        {
            get
            {
                return alphaDst;
            }
            set
            {
                if (alphaDst != value)
                {
                    alphaDst = value;
                    GL.BlendFunc((BlendingFactorSrc)alphaSrc, (BlendingFactorDest)alphaDst);
                }
            }
        }

        static bool depthWrite = false;
        public static bool DepthWriteEnabled
        {
            get
            {
                return depthWrite;
            }
            set
            {
                if (depthWrite != value)
                {
                    depthWrite = value;
                    GL.DepthMask(depthWrite);
                }
            }
        }

        static bool colorWrite = false;
        public static bool ColorWriteEnabled
        {
            get
            {
                return colorWrite;
            }
            set
            {
                if (colorWrite != value)
                {
                    colorWrite = value;
                    GL.ColorMask(colorWrite, colorWrite, colorWrite, colorWrite);
                }
            }
        }

        static int workGroupSize = 0;
        public static int ComputeWorkGroupSize
        {
            get
            {
                if (workGroupSize == 0)
                    workGroupSize = GL.GetInteger((GetPName)All.MaxComputeWorkGroupCount);

                return workGroupSize;
            }
        }

        public static ShaderProgram ShaderProgram
        {
            get
            {
                return curProg;
            }
            set
            {
                curProg = value;
                if (curProg != null) GL.UseProgram(curProg.id);
            }
        }

        public static Framebuffer Framebuffer
        {
            get
            {
                return curFramebuffer;
            }

            set
            {
                curFramebuffer = value;
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, curFramebuffer.id);
            }
        }

        private static ConcurrentQueue<Tuple<int, GLObjectType>> DeletionQueue;

        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.InteropServices.DllImport("opengl32.dll", EntryPoint = "wglGetCurrentDC")]
        extern static IntPtr wglGetCurrentDC();

        static GraphicsDevice()
        {
            GraphicsContextFlags flags = GraphicsContextFlags.Default;
#if !DEBUG
            flags |= (GraphicsContextFlags)0x8; //Disable error checking
#else
            flags |= GraphicsContextFlags.Debug;
#endif
            game = new GameWindow(1280, 720, GraphicsMode.Default, "Game Window", OpenTK.GameWindowFlags.Default, OpenTK.DisplayDevice.Default, 0, 0, flags);

            game.Resize += Window_Resize;
            game.Load += Game_Load;
            game.RenderFrame += InitRender;
            game.UpdateFrame += Game_UpdateFrame;

            GameLoop = new StateGroup();
            Cleanup = new WeakAction();
            DeletionQueue = new ConcurrentQueue<Tuple<int, GLObjectType>>();

            curVarray = null;
            curProg = null;
        }

        internal static void QueueForDeletion(int o, GLObjectType t)
        {
            DeletionQueue.Enqueue(new Tuple<int, GLObjectType>(o, t));
        }

        public static void Run(double ups, double fps)
        {
            game.Title = gameName;
#if DEBUG
            if (renderer_name == "")
                renderer_name = GL.GetString(StringName.Renderer);

            if (gl_name == "")
                gl_name = GL.GetString(StringName.Version);

            game.Title = gameName + $" | {renderer_name} | { gl_name }";
            GL.Enable(EnableCap.DebugOutput);
            GL.DebugMessageInsert(DebugSourceExternal.DebugSourceApplication, DebugType.DebugTypePortability, 1, DebugSeverity.DebugSeverityNotification, 5, "test");
#endif
            game.Run(ups, fps);
        }

        static string gl_name = "";
        static string renderer_name = "";
#if DEBUG
        static double renderCnt = 0;
        static double updateCnt = 0;
        static DateTime startTime;
#endif
        public static void SwapBuffers()
        {
#if DEBUG
            if (renderer_name == "")
                renderer_name = GL.GetString(StringName.Renderer);

            if (renderCnt == 0) startTime = DateTime.Now;
            renderCnt++;
            if ((DateTime.Now - startTime) > TimeSpan.FromMilliseconds(500))
            {
                game.Title = gameName + $" | {renderer_name} | {gl_name} | FPS : {(renderCnt * 2):F2}, UPS : {(updateCnt * 2):F2}";
                renderCnt = 0;
                updateCnt = 0;
            }
#endif
            game.SwapBuffers();
        }

        private static void DeleteObject(int o, GLObjectType t)
        {
            switch (t)
            {
                case GLObjectType.Buffer:
                    GL.DeleteBuffer(o);
                    break;
                case GLObjectType.Fence:
                    GL.DeleteSync((IntPtr)o);
                    break;
                case GLObjectType.Framebuffer:
                    GL.DeleteFramebuffer(o);
                    break;
                case GLObjectType.Sampler:
                    GL.DeleteSampler(o);
                    break;
                case GLObjectType.Shader:
                    GL.DeleteShader(o);
                    break;
                case GLObjectType.ShaderProgram:
                    GL.DeleteProgram(o);
                    break;
                case GLObjectType.Texture:
                    GL.DeleteTexture(o);
                    break;
                case GLObjectType.VertexArray:
                    GL.DeleteVertexArray(o);
                    break;
            }
        }

        public static void DeleteSomeObjects()
        {
            if (DeletionQueue.Count < 10)
                return;

            for (int i = 0; i < 20 && i < DeletionQueue.Count; i++)
            {
                if (DeletionQueue.TryDequeue(out var a))
                    DeleteObject(a.Item1, a.Item2);
            }
        }

        public static void Exit()
        {
            for (int i = 0; i < DeletionQueue.Count; i++)
            {
                if (DeletionQueue.TryDequeue(out var a))
                    DeleteObject(a.Item1, a.Item2);
            }
            game.Exit();
        }

        private static void Game_UpdateFrame(object sender, FrameEventArgs e)
        {
#if DEBUG
            updateCnt++;
            int len = GL.GetInteger((GetPName)All.MaxDebugMessageLength);
            StringBuilder str = new StringBuilder(len);
            if (GL.GetDebugMessageLog(1, len, out var source, out var type, out var id, out var severity, out var strlen, str) > 0)
            {
                var consoleCol = Console.ForegroundColor;
                switch (severity)
                {
                    case DebugSeverity.DebugSeverityHigh:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case DebugSeverity.DebugSeverityLow:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    case DebugSeverity.DebugSeverityMedium:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case DebugSeverity.DebugSeverityNotification:
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                }
                Console.WriteLine($"[{type}][{source}] {str}");
                Console.ForegroundColor = consoleCol;
            }
#endif

            Update?.Invoke(e.Time);
        }

        private static void InitRender(object sender, FrameEventArgs e)
        {

            game.VSync = VSyncMode.Off;
            game.TargetRenderFrequency = 0;
            game.TargetUpdateFrequency = 0;

            //Verify opengl functionality, check for required extensions
            int major_v = GL.GetInteger(GetPName.MajorVersion);
            int minor_v = GL.GetInteger(GetPName.MinorVersion);
            if (major_v < 4 | (major_v == 4 && minor_v < 5))
            {
                throw new Exception($"Unsupported OpenGL version ({major_v}.{minor_v}), minimum OpenGL 4.5 required.");
            }

            Game_RenderFrame(sender, e);
            game.RenderFrame -= InitRender;
            game.RenderFrame += Game_RenderFrame;
        }

        private static void Game_RenderFrame(object sender, FrameEventArgs e)
        {
            Render?.Invoke(e.Time);
        }

        private static void Game_Load(object sender, EventArgs e)
        {
            curFramebuffer = Framebuffer.Default;
            GL.Enable(EnableCap.TextureCubeMapSeamless);
            GL.Enable(EnableCap.DepthTest);
            GL.ClipControl(ClipOrigin.LowerLeft, ClipDepthMode.ZeroToOne);

            Load?.Invoke();
        }

        private static void Window_Resize(object sender, EventArgs e)
        {
            GPUStateMachine.SetViewport(0, 0, 0, game.ClientSize.Width, game.ClientSize.Height);
            GL.ClipControl(ClipOrigin.LowerLeft, ClipDepthMode.ZeroToOne);
            //Input.LowLevel.InputLL.SetWinXY(game.Location.X, game.Location.Y, game.ClientSize.Width, game.ClientSize.Height);
            Framebuffer.RecreateDefaultFramebuffer();
            //EngineManager.ResolutionChangedHandler(game.ClientSize.Width, game.ClientSize.Height);
        }

        public static void SetViewport(int idx, float x, float y, float width, float height)
        {
            GL.ViewportIndexed(idx, x, y, width, height);
        }

        public static void SetVertexArray(VertexArray varray)
        {
            curVarray = varray;
            GL.BindVertexArray(varray.id);
        }

        #region Shader Buffers
        public static void SetShaderStorageBufferBinding(ShaderStorageBuffer buf, int index)
        {
            if (buf == null) return;
            GPUStateMachine.BindBuffer(OpenTK.Graphics.OpenGL4.BufferTarget.ShaderStorageBuffer, buf.buf.id, index, (IntPtr)(buf.GetReadyOffset()), (IntPtr)buf.size);
        }

        public static void SetUniformBufferBinding(UniformBuffer buf, int index)
        {
            if (buf == null) return;

            int rd_rung = buf.curRung - 1;
            if (rd_rung < 0) rd_rung = 3;

            GPUStateMachine.BindBuffer(OpenTK.Graphics.OpenGL4.BufferTarget.UniformBuffer, buf.buf.id, index, (IntPtr)(buf.GetReadyOffset()), (IntPtr)buf.Size);
        }

        #endregion

        #region Indirect call buffers
        public static void SetMultiDrawParameterBuffer(GPUBuffer buf)
        {
            GL.BindBuffer(OpenTK.Graphics.OpenGL4.BufferTarget.DrawIndirectBuffer, buf.id);
        }

        public static void SetMultiDrawParameterBuffer(ShaderStorageBuffer buf)
        {
            SetMultiDrawParameterBuffer(buf.buf);
        }

        public static void SetParameterBuffer(GPUBuffer buf)
        {
            GL.BindBuffer((OpenTK.Graphics.OpenGL4.BufferTarget)ArbIndirectParameters.ParameterBufferArb, buf.id);
        }

        public static void SetParameterBuffer(ShaderStorageBuffer buf)
        {
            SetParameterBuffer(buf.buf);
        }
        #endregion

        #region Compute Jobs
        public static void DispatchSyncComputeJob(ShaderProgram prog, int x, int y, int z)
        {
            GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);
            var tmp = ShaderProgram;
            ShaderProgram = prog;
            GL.DispatchCompute(x, y, z);
            ShaderProgram = tmp;
        }
        #endregion

        #region Draw calls
        public static void Draw(Engine.Graphics.PrimitiveType type, int first, int count, bool indexed)
        {
            if (count == 0) return;

            curProg.Set(nameof(WindowSize), new Vector2(WindowSize.Width, WindowSize.Height));

            if (indexed) GL.DrawElements((OpenTK.Graphics.OpenGL4.PrimitiveType)type, count, DrawElementsType.UnsignedShort, IntPtr.Zero);
            else GL.DrawArrays((OpenTK.Graphics.OpenGL4.PrimitiveType)type, first, count);
        }

        public static void MultiDraw(Engine.Graphics.PrimitiveType type, bool indexed, params MultiDrawParameters[] dParams)
        {
            if (dParams.Length == 0) return;

            int[] first = new int[dParams.Length];
            int[] count = new int[dParams.Length];
            int[] baseVertex = new int[dParams.Length];
            int drawCount = dParams.Length;

            if (indexed)
                GL.MultiDrawElementsBaseVertex((OpenTK.Graphics.OpenGL4.PrimitiveType)type, count, DrawElementsType.UnsignedShort, IntPtr.Zero, drawCount, baseVertex);
            else
                GL.MultiDrawArrays((OpenTK.Graphics.OpenGL4.PrimitiveType)type, first, count, drawCount);
        }

        public static void MultiDrawIndirect(Engine.Graphics.PrimitiveType type, uint byteOffset, int count, bool indexed)
        {
            if (count == 0) return;

            if (indexed)
                GL.MultiDrawElementsIndirect((OpenTK.Graphics.OpenGL4.PrimitiveType)type, DrawElementsType.UnsignedShort, (IntPtr)byteOffset, count, 0);
            else
                GL.MultiDrawArraysIndirect((OpenTK.Graphics.OpenGL4.PrimitiveType)type, (IntPtr)byteOffset, count, 0);
        }

        public static void MultiDrawIndirectCount(Engine.Graphics.PrimitiveType type, uint byteOffset, uint countOffset, int maxCount, bool indexed)
        {
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
            if (indexed)
                GL.Arb.MultiDrawElementsIndirectCount((All)type, (All)DrawElementsType.UnsignedShort, (IntPtr)byteOffset, (IntPtr)countOffset, maxCount, 0);
            else
                GL.Arb.MultiDrawArraysIndirectCount((All)type, (IntPtr)byteOffset, (IntPtr)countOffset, maxCount, 5 * sizeof(int) /*Each entry is formatted as index data, so work accordingly*/);
        }
        #endregion

        #region Depth Range
        private static double _far = 0, _near = 0;
        public static void SetDepthRange(double near, double far)
        {
            _far = far;
            _near = near;
            //GL.NV.DepthRange(near, far);
        }

        public static void GetDepthRange(out double near, out double far)
        {
            near = _near;
            far = _far;
        }
        #endregion

        public static void Clear()
        {
            // render graphics
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        public static void ClearDepthBuffer()
        {
            GL.Clear(ClearBufferMask.DepthBufferBit);
        }

        public static void SaveTexture(Texture t, string file)
        {
#if DEBUG
            GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);
            Bitmap bmp = new Bitmap(t.Width, t.Height);
            System.Drawing.Imaging.BitmapData bmpData;

            bmpData = bmp.LockBits(new Rectangle(0, 0, t.Width, t.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            switch (t.ptype)
            {
                case Graphics.PixelType.HalfFloat:
                    GL.GetTextureImage(t.id, 0, (OpenTK.Graphics.OpenGL4.PixelFormat)Graphics.PixelFormat.Bgra, (OpenTK.Graphics.OpenGL4.PixelType)Graphics.PixelType.UnsignedInt8888Reversed, bmpData.Stride * bmpData.Height, bmpData.Scan0);
                    break;
                case Graphics.PixelType.UnsignedInt:
                    {
                        GL.GetTextureImage(t.id, 0, (OpenTK.Graphics.OpenGL4.PixelFormat)t.format, (OpenTK.Graphics.OpenGL4.PixelType)Graphics.PixelType.UnsignedInt, bmpData.Stride * bmpData.Height, bmpData.Scan0);
                        unsafe
                        {
                            uint* data = (uint*)bmpData.Scan0;
                            var f = File.OpenWrite("pixels.txt");
                            var fs = new StreamWriter(f);
                            for (int y = 0; y < bmp.Height; y++)
                            {
                                for (int x = 0; x < bmp.Width; x++)
                                    fs.Write($" {*(data++)}");
                                fs.WriteLine();
                            }
                            fs.Close();
                        }
                    }
                    break;
            }
            bmp.UnlockBits(bmpData);

            bmp.RotateFlip(RotateFlipType.Rotate180FlipX);
            bmp.Save(file);
            bmp.Dispose();
#endif
        }

    }
}
