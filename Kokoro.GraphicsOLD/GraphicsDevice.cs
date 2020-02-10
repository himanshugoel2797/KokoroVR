using Kokoro.Math;
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
using Kokoro.Graphics.Profiling;
using Kokoro.Input.LowLevel;

namespace Kokoro.Graphics
{
    public enum FaceWinding
    {
        Clockwise = 2304,
        CounterClockwise = 2305
    }

    public static class GraphicsDevice
    {
        static ShaderProgram curProg;
        static Framebuffer curFramebuffer;
        static FaceWinding winding;

        public static GPUPerfAPI.NET.Context Context { get; private set; }

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
                return new Size(Window.Width, Window.Height);
            }
            set
            {
                Window.Width = value.Width;
                Window.Height = value.Height;
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

        public static Action<int, int> Resized { get; set; }
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
        //public static OpenTK.Input.KeyboardDevice Keyboard { get { return Window.Keyboard; } }
        //public static OpenTK.Input.MouseDevice Mouse { get { return Window.Mouse; } }
        public static GameWindow Window { get; private set; }
        public static int PatchCount
        {
            set
            {
                GL.PatchParameter(PatchParameterInt.PatchVertices, value);
            }
        }

        public static int EyeCount { get; set; }

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
                    GL.BlendFunc((BlendingFactor)alphaSrc, (BlendingFactor)alphaDst);
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
                    GL.BlendFunc((BlendingFactor)alphaSrc, (BlendingFactor)alphaDst);
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
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, (int)curFramebuffer);
            }
        }

        private static readonly ConcurrentQueue<Tuple<int, GLObjectType>> DeletionQueue;

        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.InteropServices.DllImport("opengl32.dll", EntryPoint = "wglGetCurrentContext")]
        extern static IntPtr WglGetCurrentContext();

        static GraphicsDevice()
        {
            GraphicsContextFlags flags = GraphicsContextFlags.Default;
#if !DEBUG
            flags |= GraphicsContextFlags.NoError; //Disable error checking
#else
            flags |= GraphicsContextFlags.Debug;
#endif
            Window = new GameWindow(1280, 720, GraphicsMode.Default, "Game Window", OpenTK.GameWindowFlags.Default, OpenTK.DisplayDevice.Default, 0, 0, flags | GraphicsContextFlags.ForwardCompatible);

            Window.Resize += Window_Resize;
            Window.Load += Game_Load;
            Window.RenderFrame += InitRender;
            Window.UpdateFrame += Game_UpdateFrame;

            GameLoop = new StateGroup();
            Cleanup = new WeakAction();
            DeletionQueue = new ConcurrentQueue<Tuple<int, GLObjectType>>();

            curProg = null;
        }

        internal static void QueueForDeletion(int o, GLObjectType t)
        {
            DeletionQueue.Enqueue(new Tuple<int, GLObjectType>(o, t));
        }

        public static void SetVertexArray(VertexArray varray)
        {
            GL.BindVertexArray(varray.id);
        }

        public static void SetRenderState(RenderState state)
        {
            GraphicsDevice.CullMode = state.CullMode;
            GraphicsDevice.ClearColor = state.ClearColor;
            GraphicsDevice.DepthTest = state.DepthTest;
            GraphicsDevice.DepthWriteEnabled = state.DepthWrite;
            GraphicsDevice.ColorWriteEnabled = state.ColorWrite;
            GraphicsDevice.ClearDepth = state.ClearDepth;
            GraphicsDevice.AlphaSrc = state.Src;
            GraphicsDevice.AlphaDst = state.Dst;
            GraphicsDevice.Framebuffer = state.Framebuffer;
            GraphicsDevice.SetDepthRange(state.NearPlane, state.FarPlane);


            if (state.IndexBuffer != null) SetVertexArray(state.IndexBuffer.varray);
            for (int i = 0; i < state.Viewports.Length; i++)
                GraphicsDevice.SetViewport(i, state.Viewports[i].X, state.Viewports[i].Y, state.Viewports[i].Z, state.Viewports[i].W);

            if (state.ShaderStorageBufferBindings != null)
            {
                StorageBuffer[] pendingBindings = new StorageBuffer[state.ShaderStorageBufferBindings.Length];
                Array.Copy(state.ShaderStorageBufferBindings, pendingBindings, pendingBindings.Length);
                int pendingCnt = pendingBindings.Length;
                while (pendingCnt > 0)
                {
                    for (int i = 0; i < pendingBindings.Length; i++)
                    {
                        if (pendingBindings[i] != null && pendingBindings[i].IsReady)
                        {
                            GraphicsDevice.SetShaderStorageBufferBinding(pendingBindings[i], i);
                            pendingBindings[i] = null;
                            pendingCnt--;
                        }
                    }
                }
            }

            if (state.UniformBufferBindings != null)
            {
                UniformBuffer[] pendingBindings = new UniformBuffer[state.UniformBufferBindings.Length];
                Array.Copy(state.UniformBufferBindings, pendingBindings, pendingBindings.Length);
                int pendingCnt = pendingBindings.Length;
                while (pendingCnt > 0)
                {
                    for (int i = 0; i < pendingBindings.Length; i++)
                    {
                        if (pendingBindings[i] != null && pendingBindings[i].IsReady)
                        {
                            GraphicsDevice.SetUniformBufferBinding(pendingBindings[i], i);
                            pendingBindings[i] = null;
                            pendingCnt--;
                        }
                    }
                }
            }

            if (state.ShaderProgram != null)
                GraphicsDevice.ShaderProgram = state.ShaderProgram;
        }

        public static void Run(double ups, double fps)
        {
            if (PerfAPI.MetricsEnabled) GPUPerfAPI.NET.Context.Initialize();
            Window.Title = gameName;
#if DEBUG
            if (string.IsNullOrWhiteSpace(renderer_name))
                renderer_name = GL.GetString(StringName.Renderer);

            if (string.IsNullOrWhiteSpace(gl_name))
                gl_name = GL.GetString(StringName.Version);

            GL.GetInteger(GetPName.MaxCombinedImageUniforms, out var img_val);
            GL.GetInteger((GetPName)All.SparseBufferPageSizeArb, out var psz);
            GL.GetInteger((GetIndexedPName)All.MaxComputeWorkGroupCount, 0, out var x_wg_max); //Simplify buffer interface, get rid of individual uniforms, mandatory texture views, mandatory pbo streaming 
            GL.GetInteger((GetIndexedPName)All.MaxComputeWorkGroupCount, 1, out var y_wg_max);
            GL.GetInteger((GetIndexedPName)All.MaxComputeWorkGroupCount, 2, out var z_wg_max);

            GL.GetInteger((GetIndexedPName)All.MaxComputeWorkGroupSize, 0, out var x_wg_sz);
            GL.GetInteger((GetIndexedPName)All.MaxComputeWorkGroupSize, 1, out var y_wg_sz);
            GL.GetInteger((GetIndexedPName)All.MaxComputeWorkGroupSize, 2, out var z_wg_sz);

            GL.GetInteger((GetPName)All.MaxComputeWorkGroupInvocations, out var wg_inv);

            Window.Title = gameName + $" | {renderer_name} | { gl_name }";
            GL.Enable(EnableCap.DebugOutput);
            GL.DebugMessageInsert(DebugSourceExternal.DebugSourceApplication, DebugType.DebugTypePortability, 1, DebugSeverity.DebugSeverityNotification, 5, "test");
#endif
            GL.Enable(EnableCap.TextureCubeMapSeamless);
            Window.Run(ups, fps);
        }

        static string gl_name = "";
        static string renderer_name = "";
        //#if DEBUG
        static double renderCnt = 0;
        static double updateCnt = 0;
        static DateTime startTime;
        //#endif
        public static void SwapBuffers()
        {
            Window.SwapBuffers();
            //#if DEBUG
            if (string.IsNullOrWhiteSpace(renderer_name))
                renderer_name = GL.GetString(StringName.Renderer);

            if (renderCnt == 0) startTime = DateTime.Now;
            renderCnt++;
            if ((DateTime.Now - startTime) > TimeSpan.FromMilliseconds(500))
            {
                Window.Title = gameName + $" | {renderer_name} | {gl_name} | FPS : {(renderCnt * 2):F2}, UPS : {(updateCnt * 2):F2}";
                renderCnt = 0;
                updateCnt = 0;
            }
            //#endif
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
            Window.Exit();
        }

        private static void Game_UpdateFrame(object sender, FrameEventArgs e)
        {
#if DEBUG
            if (Context == null && PerfAPI.MetricsEnabled)
            {
                Context = new GPUPerfAPI.NET.Context(GraphicsContext.CurrentContextHandle.Handle);
            }
            PerfAPI.BeginFrame();
#endif
            DeleteSomeObjects();
#if DEBUG

            GenericMetrics.UpdateLog();

            updateCnt++;
            int len = GL.GetInteger((GetPName)All.MaxDebugMessageLength);
            if (GL.GetDebugMessageLog(1, len, out var source, out var type, out var id, out var severity, out var strlen, out var str) > 0)
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
            InputLL.IsFocused(Window.Focused);
            Update?.Invoke(e.Time);

        }

        private static void InitRender(object sender, FrameEventArgs e)
        {

            Window.VSync = VSyncMode.Off;
            Window.TargetRenderFrequency = 0;
            Window.TargetUpdateFrequency = 0;

            //Verify opengl functionality, check for required extensions
            int major_v = GL.GetInteger(GetPName.MajorVersion);
            int minor_v = GL.GetInteger(GetPName.MinorVersion);
            if (major_v < 4 | (major_v == 4 && minor_v < 5))
            {
                throw new Exception($"Unsupported OpenGL version ({major_v}.{minor_v}), minimum OpenGL 4.5 required.");
            }

            Game_RenderFrame(sender, e);
            Window.RenderFrame -= InitRender;
            Window.RenderFrame += Game_RenderFrame;
        }

        private static void Game_RenderFrame(object sender, FrameEventArgs e)
        {
            Render?.Invoke(e.Time);
#if DEBUG
            PerfAPI.EndFrame();
            GenericMetrics.EndFrame();
#endif
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
            GL.ViewportIndexed(0, 0, 0, Window.ClientSize.Width, Window.ClientSize.Height);
            GL.ClipControl(ClipOrigin.LowerLeft, ClipDepthMode.ZeroToOne);
            //Input.LowLevel.InputLL.SetWinXY(game.Location.X, game.Location.Y, game.ClientSize.Width, game.ClientSize.Height);
            Framebuffer.RecreateDefaultFramebuffer();
            Resized?.Invoke(Window.ClientSize.Width, Window.ClientSize.Height);
        }

        public static void SetViewport(int idx, float x, float y, float width, float height)
        {
            GL.ViewportIndexed(idx, x, y, width, height);
        }

        #region Shader Buffers
        public static void SetShaderStorageBufferBinding(StorageBuffer buf, int index)
        {
            if (buf == null) return;
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, index, (int)buf);
        }

        public static void SetUniformBufferBinding(UniformBuffer buf, int index)
        {
            if (buf == null) return;
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, index, (int)buf);
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
    }
}
