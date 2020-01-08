using Kokoro.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kokoro.Common.StateMachine;
using Kokoro.Math;
using Kokoro.Graphics.Profiling;

namespace KokoroVR
{
    public static class Engine
    {
#if VR
        public static VRClient HMDClient { get; private set; }
#endif
        internal static MeshGroup iMeshGroup;
        private static StateManager stateMachine;

        public static Framebuffer[] Framebuffers { get; private set; }
        public static Matrix4[] Projection { get; private set; }
        public static Matrix4[] View { get; private set; }
        public static Frustum[] Frustums { get; private set; }
        public static LocalPlayer CurrentPlayer { get; private set; }
        public static bool LogMetrics { get { return GenericMetrics.MetricsEnabled; } set { GenericMetrics.MetricsEnabled = value; } }
        public static Action<int, int> WindowResized { get { return GraphicsDevice.Resized; } set { GraphicsDevice.Resized = value; } }

        static Engine()
        {
            stateMachine = new StateManager();
            GraphicsDevice.GameLoop.RegisterIState(stateMachine);
        }

        public static void Initialize(ExperienceKind kind)
        {
#if VR
            HMDClient = VRClient.Create(kind);
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
            CurrentPlayer = new LocalPlayer();
            CurrentPlayer.Position = Vector3.UnitX * -300; //new Vector3(0.577f, 0.577f, 0.577f) * 20;
            Projection = new Matrix4[]
            {
                Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), 16f/9f, 0.001f)
            };
            View = new Matrix4[]
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
            iMeshGroup = new MeshGroup(MeshGroupVertexFormat.X32F_Y32F_Z32F, 256, 256);
#endif
        }

        public static void SetupControllers(string file, params VRActionSet[] actions)
        {
#if VR
            HMDClient.InitializeControllers(file, actions);
#endif
        }

        public static void Start()
        {
            GraphicsDevice.Update = CurrentPlayer.Update + GraphicsDevice.Update;
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
