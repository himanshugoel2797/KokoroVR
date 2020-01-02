using Kokoro.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kokoro.Common.StateMachine;
using Kokoro.Math;

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
        public static LocalPlayer CurrentPlayer { get; private set; }

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
            Framebuffers = new Framebuffer[]
            {
                HMDClient.LeftFramebuffer,
                HMDClient.RightFramebuffer
            };
            iMeshGroup = new MeshGroup(MeshGroupVertexFormat.X32F_Y32F_Z32F, 256, 256);
            CurrentPlayer = new LocalPlayer(HMDClient);
#else
            Projection = new Matrix4[]
            {
                Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), 16f/9f, 0.001f)
            };
            View = new Matrix4[]
            {
                Matrix4.LookAt(-Vector3.UnitX, Vector3.Zero, Vector3.UnitY)
            };
            Framebuffers = new Framebuffer[]
            {
                Framebuffer.Default
            };
            iMeshGroup = new MeshGroup(MeshGroupVertexFormat.X32F_Y32F_Z32F, 256, 256);
            CurrentPlayer = new LocalPlayer();
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
