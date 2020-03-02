using Kokoro.Graphics;
using Kokoro.Math;
using KokoroVR2.Graphics;
using KokoroVR2.Input;
using System;

namespace KokoroVR2
{
    public static class Engine
    {
        public static string AppName { get => GraphicsDevice.AppName; set => GraphicsDevice.AppName = value; }
        public static bool EnableValidation { get => GraphicsDevice.EnableValidation; set => GraphicsDevice.EnableValidation = value; }
        public static bool RebuildShaders { get => GraphicsDevice.RebuildShaders; set => GraphicsDevice.RebuildShaders = value; }
        public static Framegraph Graph { get; private set; }
        public static uint Width { get => GraphicsDevice.Width; }
        public static uint Height { get => GraphicsDevice.Height; }
        public static GameWindow Window { get => GraphicsDevice.Window; }
        public static Matrix4 Projection { get; set; }
        public static Matrix4 View { get; set; }
        public static Matrix4 PrevView { get; set; }
        public static Frustum Frustum { get; set; }
        public static LocalPlayer CurrentPlayer { get; private set; }
        public static DeferredRenderer DeferredRenderer { get; set; }
        public static GpuBuffer GlobalParameters { get => Graph.GlobalParameters; }
        public static GpuBuffer GlobalParametersStaging { get; private set; }
        public static Keyboard Keyboard { get; set; }
        public static bool RebuildGraph { get; set; }

        public delegate void FrameHandler(double time_ms, double delta_ms);
        public static event FrameHandler OnRebuildGraph;
        public static event FrameHandler OnUpdate;

        public static void Initialize()
        {
            GraphicsDevice.EngineName = $"KokoroVR2";
            GraphicsDevice.Init();
            Graph = new Framegraph(0);
            RebuildGraph = true;

            GlobalParametersStaging = new GpuBuffer()
            {
                Name = Graph.GlobalParametersName + "_Local",
                Mapped = true,
                MemoryUsage = MemoryUsage.CpuOnly,
                Size = Graph.GlobalParametersLength,
                Usage = BufferUsage.TransferSrc
            };
            GlobalParametersStaging.Build(0);

            Keyboard = new Keyboard();
            CurrentPlayer = new LocalPlayer
            {
                Position = Vector3.UnitX * -10 //new Vector3(0.577f, 0.577f, 0.577f) * 20;
            };
            Projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), (float)Width / Height, 1 - 0.001f);
            View = Matrix4.LookAt(CurrentPlayer.Position, Vector3.Zero, Vector3.UnitY);
            PrevView = Matrix4.LookAt(CurrentPlayer.Position, Vector3.Zero, Vector3.UnitY);
            Frustum = new Frustum(Matrix4.LookAt(CurrentPlayer.Position, Vector3.Zero, Vector3.UnitY), Projection, CurrentPlayer.Position);
        }

        private static void UpdateParams()
        {
            unsafe
            {
                float* p = (float*)GlobalParametersStaging.GetAddress();
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

                p[off++] = CurrentPlayer.PrevPosition.X;
                p[off++] = CurrentPlayer.PrevPosition.Y;
                p[off++] = CurrentPlayer.PrevPosition.Z;
                p[off++] = Width;

                p[off++] = CurrentPlayer.PrevUp.X;
                p[off++] = CurrentPlayer.PrevUp.Y;
                p[off++] = CurrentPlayer.PrevUp.Z;
                p[off++] = Height;

                p[off++] = CurrentPlayer.PrevDirection.X;
                p[off++] = CurrentPlayer.PrevDirection.Y;
                p[off++] = CurrentPlayer.PrevDirection.Z;
                p[off++] = 0;

                p[off++] = CurrentPlayer.Position.X;
                p[off++] = CurrentPlayer.Position.Y;
                p[off++] = CurrentPlayer.Position.Z;
                p[off++] = 0;

                p[off++] = CurrentPlayer.Up.X;
                p[off++] = CurrentPlayer.Up.Y;
                p[off++] = CurrentPlayer.Up.Z;
                p[off++] = 0;

                p[off++] = CurrentPlayer.Direction.X;
                p[off++] = CurrentPlayer.Direction.Y;
                p[off++] = CurrentPlayer.Direction.Z;
                p[off++] = 0;
            }
        }

        public static void Start(int fps)
        {
            GraphicsDevice.Window.Update += Window_Update;
            GraphicsDevice.Window.Render += Window_Render;
            GraphicsDevice.Window.Run(fps);
        }

        private static void Window_Render(double time_ms, double delta_ms)
        {
            if (RebuildGraph)
            {
                Graph.RegisterPass(new BufferUploadPass()
                {
                    Name = Graph.GlobalParametersName,
                    DestBuffer = Graph.GlobalParameters,
                    DeviceOffset = 0,
                    LocalOffset = 0,
                    Size = Graph.GlobalParametersLength,
                    SourceBuffer = GlobalParametersStaging
                });

                OnRebuildGraph?.Invoke(time_ms, delta_ms);
                Graph.Compile();
                RebuildGraph = false;
                Graph.Execute(true);
            }
            else
                Graph.Execute(false);
        }

        private static void Window_Update(double time_ms, double delta_ms)
        {
            UpdateParams();
            OnUpdate?.Invoke(time_ms, delta_ms);
        }
    }
}
