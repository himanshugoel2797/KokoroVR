using Kokoro.Graphics;
using Kokoro.Math;
using System;

namespace Kokoro.Graphics
{
    public delegate void FrameHandler(double time_ms, double delta_ms);
    public static class GraphicsContext
    {
        public static string AppName { get => GraphicsDevice.AppName; set => GraphicsDevice.AppName = value; }
        public static bool EnableValidation { get => GraphicsDevice.EnableValidation; set => GraphicsDevice.EnableValidation = value; }
        public static bool RebuildShaders { get => GraphicsDevice.RebuildShaders; set => GraphicsDevice.RebuildShaders = value; }
        public static bool RenderGraphNeedsRebuild { get; set; }
        public static uint Width { get => GraphicsDevice.Width; }
        public static uint Height { get => GraphicsDevice.Height; }
        public static GameWindow Window { get => GraphicsDevice.Window; }
        public static Matrix4 Projection { get; set; }
        public static Matrix4 View { get; set; }
        public static Matrix4 PrevView { get; set; }
        public static Frustum Frustum { get; set; }
        public static Vector3 CameraPosition { get; set; }
        public static Vector3 CameraDirection { get; set; }
        public static Vector3 CameraUp { get; set; }
        public static Vector3 PrevCameraPosition { get; set; }
        public static Vector3 PrevCameraDirection { get; set; }
        public static Vector3 PrevCameraUp { get; set; }
        public static StreamableBuffer GlobalParameters { get; private set; }
        public static Framegraph.FrameGraph RenderGraph { get; set; }
        public static event FrameHandler OnRender;
        public static event FrameHandler OnUpdate;
        public static event Action OnRebuildGraph;

        public static void Initialize()
        {
            GraphicsDevice.EngineName = $"KokoroVR2";
            GraphicsDevice.Init();

            GlobalParameters = new StreamableBuffer("GlobalParameters", 4096, BufferUsage.Uniform);

            CameraPosition = PrevCameraPosition = -Vector3.UnitZ;
            CameraDirection = PrevCameraDirection = Vector3.UnitZ;
            CameraUp = PrevCameraUp = Vector3.UnitY;

            Projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), (float)Width / Height, 1 - 0.001f);
            View = Matrix4.LookAt(-Vector3.UnitZ, Vector3.Zero, Vector3.UnitY);
            PrevView = Matrix4.LookAt(-Vector3.UnitZ, Vector3.Zero, Vector3.UnitY);
            Frustum = new Frustum(View, Projection, -Vector3.UnitZ);

            RenderGraphNeedsRebuild = true;

            OnRebuildGraph += RebuildGlobalGraph;
            GraphicsDevice.Window.Update += Window_Update;
            GraphicsDevice.Window.Render += Window_Render;
        }

        private static void RebuildGlobalGraph()
        {
            GlobalParameters.RebuildGraph();
        }

        private static void UpdateParams()
        {
            unsafe
            {
                float* p = (float*)GlobalParameters.BeginBufferUpdate();
                int off = 0;

                var f = (float[])Projection;
                for (int j = 0; j < f.Length; j++)
                    p[off++] = f[j];

                f = (float[])View;
                for (int j = 0; j < f.Length; j++)
                    p[off++] = f[j];

                f = (float[])(View * Projection);
                for (int j = 0; j < f.Length; j++)
                    p[off++] = f[j];

                f = (float[])Matrix4.Invert(View * Projection);
                for (int j = 0; j < f.Length; j++)
                    p[off++] = f[j];

                f = (float[])PrevView;
                for (int j = 0; j < f.Length; j++)
                    p[off++] = f[j];

                f = (float[])(PrevView * Projection);
                for (int j = 0; j < f.Length; j++)
                    p[off++] = f[j];

                f = (float[])Matrix4.Invert(PrevView * Projection);
                for (int j = 0; j < f.Length; j++)
                    p[off++] = f[j];

                p[off++] = PrevCameraPosition.X;
                p[off++] = PrevCameraPosition.Y;
                p[off++] = PrevCameraPosition.Z;
                p[off++] = Width;

                p[off++] = PrevCameraUp.X;
                p[off++] = PrevCameraUp.Y;
                p[off++] = PrevCameraUp.Z;
                p[off++] = Height;

                p[off++] = PrevCameraDirection.X;
                p[off++] = PrevCameraDirection.Y;
                p[off++] = PrevCameraDirection.Z;
                p[off++] = 0;

                p[off++] = CameraPosition.X;
                p[off++] = CameraPosition.Y;
                p[off++] = CameraPosition.Z;
                p[off++] = 0;

                p[off++] = CameraUp.X;
                p[off++] = CameraUp.Y;
                p[off++] = CameraUp.Z;
                p[off++] = 0;

                p[off++] = CameraDirection.X;
                p[off++] = CameraDirection.Y;
                p[off++] = CameraDirection.Z;
                p[off++] = 0;

                GlobalParameters.EndBufferUpdate();
                //GlobalParameters.Update();
            }
        }

        public static void Start(int fps)
        {
            GraphicsDevice.Window.Run(fps);
        }

        private static void Window_Render(double time_ms, double delta_ms)
        {
            if (RenderGraphNeedsRebuild)
            {
                RenderGraphNeedsRebuild = false;
                OnRebuildGraph?.Invoke();
            }
            OnRender?.Invoke(time_ms, delta_ms);
        }

        private static void Window_Update(double time_ms, double delta_ms)
        {
            UpdateParams();
            OnUpdate?.Invoke(time_ms, delta_ms);
        }
    }
}
