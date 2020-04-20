using Kokoro.Graphics;
using Kokoro.Graphics.Framegraph;
using Kokoro.Math;
using KokoroVR2.Graphics;
using KokoroVR2.Input;
using System;

namespace KokoroVR2
{
    public static class Engine
    {
        public static string AppName { get => GraphicsContext.AppName; set => GraphicsContext.AppName = value; }
        public static bool EnableValidation { get => GraphicsContext.EnableValidation; set => GraphicsContext.EnableValidation = value; }
        public static bool RebuildShaders { get => GraphicsContext.RebuildShaders; set => GraphicsContext.RebuildShaders = value; }
        public static uint Width { get => GraphicsContext.Width; }
        public static uint Height { get => GraphicsContext.Height; }
        public static GameWindow Window { get => GraphicsContext.Window; }
        public static Matrix4 Projection { get => GraphicsContext.Projection; set => GraphicsContext.Projection = value; }
        public static Matrix4 View { get => GraphicsContext.View; set => GraphicsContext.View = value; }
        public static Matrix4 PrevView { get => GraphicsContext.PrevView; set => GraphicsContext.PrevView = value; }
        public static Frustum Frustum { get => GraphicsContext.Frustum; set => GraphicsContext.Frustum = value; }
        public static LocalPlayer CurrentPlayer { get; private set; }
        public static DeferredRenderer DeferredRenderer { get; set; }
        public static StreamableBuffer GlobalParameters { get => GraphicsContext.GlobalParameters; }
        public static FrameGraph RenderGraph { get => GraphicsContext.RenderGraph; set => GraphicsContext.RenderGraph = value; }
        public static Keyboard Keyboard { get; set; }
        public static event FrameHandler OnRender { add => GraphicsContext.OnRender += value; remove => GraphicsContext.OnRender -= value; }
        public static event FrameHandler OnUpdate { add => GraphicsContext.OnUpdate += value; remove => GraphicsContext.OnUpdate -= value; }
        public static event Action OnRebuildGraph { add => GraphicsContext.OnRebuildGraph += value; remove => GraphicsContext.OnRebuildGraph -= value; }

        public static void Initialize()
        {
            GraphicsContext.Initialize();

            Keyboard = new Keyboard();
            CurrentPlayer = new LocalPlayer
            {
                Position = Vector3.UnitX * -10 //new Vector3(0.577f, 0.577f, 0.577f) * 20;
            };
            Projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), (float)Width / Height, 1 - 0.001f);
            View = Matrix4.LookAt(CurrentPlayer.Position, Vector3.Zero, Vector3.UnitY);
            PrevView = Matrix4.LookAt(CurrentPlayer.Position, Vector3.Zero, Vector3.UnitY);
            Frustum = new Frustum(Matrix4.LookAt(CurrentPlayer.Position, Vector3.Zero, Vector3.UnitY), Projection, CurrentPlayer.Position);
            DeferredRenderer = new DeferredRenderer();
        }

        public static void Start(int fps)
        {
            GraphicsContext.OnUpdate += Window_Update;
            GraphicsContext.OnRender += Window_Render;
            GraphicsContext.Start(fps);
        }

        private static void Window_Render(double time_ms, double delta_ms)
        {

        }

        private static void Window_Update(double time_ms, double delta_ms)
        {
            Mouse.Update();
            Keyboard.Update();
            GraphicsContext.PrevCameraDirection = CurrentPlayer.Direction;
            GraphicsContext.PrevCameraPosition = CurrentPlayer.Position;
            GraphicsContext.PrevCameraUp = CurrentPlayer.Up;
            GraphicsContext.PrevView = View;
            CurrentPlayer.Update(delta_ms);
            GraphicsContext.CameraDirection = CurrentPlayer.Direction;
            GraphicsContext.CameraPosition = CurrentPlayer.Position;
            GraphicsContext.CameraUp = CurrentPlayer.Up;
            GraphicsContext.View = View;
        }
    }
}
