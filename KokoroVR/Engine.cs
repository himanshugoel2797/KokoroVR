using Kokoro.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kokoro.Common.StateMachine;
using Kokoro.Math;
using Kokoro.Graphics.Profiling;
using KokoroVR.Input;
using KokoroVR.Graphics;
using System.Runtime.InteropServices;

namespace KokoroVR
{
    public static class Engine
    {
#if VR
        public static VRClient HMDClient { get; private set; }
#endif
        private static StateManager stateMachine;

        public static int EyeCount { get => GraphicsDevice.EyeCount; private set => GraphicsDevice.EyeCount = value; }
        public static Framebuffer[] Framebuffers { get; private set; }
        public static Matrix4[] Projection { get; private set; }
        public static Matrix4[] View { get; private set; }
        public static Matrix4[] PrevView { get; private set; }
        public static Frustum[] Frustums { get; private set; }
        public static LocalPlayer CurrentPlayer { get; private set; }
        public static DeferredRenderer DeferredRenderer { get; set; }
        public static UniformBuffer GlobalParameters { get; set; }
        public static bool LogMetrics { get { return GenericMetrics.MetricsEnabled; } set { GenericMetrics.MetricsEnabled = value; } }
        public static bool LogAMDMetrics { get { return PerfAPI.MetricsEnabled; } set { PerfAPI.MetricsEnabled = value; } }
        public static Action<int, int> WindowResized { get { return GraphicsDevice.Resized; } set { GraphicsDevice.Resized = value; } }
        public static Keyboard Keyboard { get; set; }

        static Engine()
        {
            stateMachine = new StateManager();
            GraphicsDevice.GameLoop.RegisterIState(stateMachine);
        }

        public static void Initialize(ExperienceKind kind)
        {
#if VR
            HMDClient = VRClient.Create(kind);
            EyeCount = 2;
            Projection = new Matrix4[]
            {
                HMDClient.GetEyeProjection(VRHand.Left, 0.01f),
                HMDClient.GetEyeProjection(VRHand.Right, 0.01f),
            };
            View = new Matrix4[]
            {
                HMDClient.GetEyeView(VRHand.Left),
                HMDClient.GetEyeView(VRHand.Right)
            };
            PrevView = new Matrix4[]
            {
                HMDClient.GetEyeView(VRHand.Left),
                HMDClient.GetEyeView(VRHand.Right)
            };
            Frustums = new Frustum[]
            {
                new Frustum(View[0], Projection[0], CurrentPlayer.Position)
                new Frustum(View[1], Projection[1], CurrentPlayer.Position)
            };
            Framebuffers = new Framebuffer[]
            {
                HMDClient.LeftFramebuffer,
                HMDClient.RightFramebuffer
            };
            iMeshGroup = new MeshGroup(MeshGroupVertexFormat.X32F_Y32F_Z32F, 256, 256);
            CurrentPlayer = new LocalPlayer(HMDClient);
#else
            EyeCount = 1;
            Keyboard = new Keyboard();
            CurrentPlayer = new LocalPlayer
            {
                Position = Vector3.UnitX * -10 //new Vector3(0.577f, 0.577f, 0.577f) * 20;
            };
            Projection = new Matrix4[]
            {
                Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), 9f/9f, 1 - 0.001f)
            };
            View = new Matrix4[]
            {
                Matrix4.LookAt(CurrentPlayer.Position, Vector3.Zero, Vector3.UnitY)
            };
            PrevView = new Matrix4[]
            {
                Matrix4.LookAt(CurrentPlayer.Position, Vector3.Zero, Vector3.UnitY)
            };
            Frustums = new Frustum[]
            {
                new Frustum(Matrix4.LookAt(CurrentPlayer.Position, Vector3.Zero, Vector3.UnitY), Projection[0], CurrentPlayer.Position)
            };
            Framebuffers = new Framebuffer[]
            {
                Framebuffer.Default
            };
#endif
            GlobalParameters = new UniformBuffer(false);
        }

        public static void SetupControllers(string file, params VRActionSet[] actions)
        {
#if VR
            HMDClient.InitializeControllers(file, actions);
#endif
        }

        private static void InputUpdate(double time)
        {
            Mouse.Update();
            Keyboard.Update();
        }

        private static void ParamsUpdate(double time)
        {
            unsafe
            {
                float* p = (float*)GlobalParameters.Update();
                int off = 0;

                for (int i = 0; i < Engine.EyeCount; i++)
                {
                    var f = (float[])Projection[i];
                    for (int j = 0; j < f.Length; j++)
                        p[off++] = f[j];
                }

                for (int i = 0; i < Engine.EyeCount; i++)
                {
                    var f = (float[])View[i];
                    for (int j = 0; j < f.Length; j++)
                        p[off++] = f[j];
                }

                for (int i = 0; i < Engine.EyeCount; i++)
                {
                    var f = (float[])(View[i] * Projection[i]);
                    for (int j = 0; j < f.Length; j++)
                        p[off++] = f[j];
                }

                for (int i = 0; i < Engine.EyeCount; i++)
                {
                    var f = (float[])PrevView[i];
                    for (int j = 0; j < f.Length; j++)
                        p[off++] = f[j];
                }

                for (int i = 0; i < Engine.EyeCount; i++)
                {
                    var f = (float[])(PrevView[i] * Projection[i]);
                    for (int j = 0; j < f.Length; j++)
                        p[off++] = f[j];
                }

                for (int i = 0; i < Engine.EyeCount; i++)
                {
                    var f = (float[])(DeferredRenderer?.InfoBindings[i].GetTextureHandle());
                    for (int j = 0; j < f.Length; j++)
                        p[off++] = f[j];
                    f = (float[])(DeferredRenderer?.InfoBindings2[i].GetTextureHandle());
                    for (int j = 0; j < f.Length; j++)
                        p[off++] = f[j];

                }

                for (int i = 0; i < Engine.EyeCount; i++)
                {
                    var f = (float[])(DeferredRenderer?.DepthBindings[i].GetTextureHandle());
                    for (int j = 0; j < f.Length; j++)
                        p[off++] = f[j];
                    off += 2;
                }

                p[off++] = CurrentPlayer.PrevPosition.X;
                p[off++] = CurrentPlayer.PrevPosition.Y;
                p[off++] = CurrentPlayer.PrevPosition.Z;
                p[off++] = GraphicsDevice.WindowSize.Width;

                p[off++] = CurrentPlayer.PrevUp.X;
                p[off++] = CurrentPlayer.PrevUp.Y;
                p[off++] = CurrentPlayer.PrevUp.Z;
                p[off++] = GraphicsDevice.WindowSize.Height;

                p[off++] = CurrentPlayer.PrevDirection.X;
                p[off++] = CurrentPlayer.PrevDirection.Y;
                p[off++] = CurrentPlayer.PrevDirection.Z;
                p[off++] = 0;

                p[off++] = CurrentPlayer.Position.X;
                p[off++] = CurrentPlayer.Position.Y;
                p[off++] = CurrentPlayer.Position.Z;
                p[off++] = GraphicsDevice.WindowSize.Width;

                p[off++] = CurrentPlayer.Up.X;
                p[off++] = CurrentPlayer.Up.Y;
                p[off++] = CurrentPlayer.Up.Z;
                p[off++] = GraphicsDevice.WindowSize.Height;

                p[off++] = CurrentPlayer.Direction.X;
                p[off++] = CurrentPlayer.Direction.Y;
                p[off++] = CurrentPlayer.Direction.Z;
                p[off++] = 0;

                GlobalParameters.UpdateDone();
            }
        }

        public static void Start()
        {
            GraphicsDevice.Update = ParamsUpdate + GraphicsDevice.Update;
            GraphicsDevice.Update = CurrentPlayer.Update + GraphicsDevice.Update;
            GraphicsDevice.Update = InputUpdate + GraphicsDevice.Update;
            GraphicsDevice.ClearColor = new Vector4(0, 0.5f, 1.0f, 0.0f);
            GraphicsDevice.ClearDepth = InverseDepth.ClearDepth;
            GraphicsDevice.Run(0, 0);
        }

        public static void Clear()
        {
#if VR
            HMDClient.Clear();
#endif
        }

        public static void Submit()
        {
#if VR
            HMDClient.Submit(VRHand.Left);
            HMDClient.Submit(VRHand.Right);
#endif
        }

        public static void AddWorld(World w)
        {
            stateMachine.AddState(w.Name, w);
        }

        public static void RemoveWorld(string name)
        {
            stateMachine.RemoveState(name);
        }

        public static void SetActiveWorld(string name)
        {
            stateMachine.SetActiveState(name);
        }
    }
}
