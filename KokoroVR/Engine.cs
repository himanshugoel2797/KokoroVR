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
        public static VRClient HMDClient { get; private set; }
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
        }

        public static void SetupControllers(string file, params VRActionSet[] actions)
        {
            HMDClient.InitializeControllers(file, actions);
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
            HMDClient.Clear();
        }

        public static void Submit()
        {
            HMDClient.Submit(VRHand.Left);
            HMDClient.Submit(VRHand.Right);
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
