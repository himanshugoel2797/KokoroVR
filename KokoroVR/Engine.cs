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
        static VRClient client;
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
            client = VRClient.Create(kind);
            Projection = new Matrix4[]
            {
                client.GetEyeProjection(VRHand.Left, 0.001f),
                client.GetEyeProjection(VRHand.Right, 0.001f),
            };
            View = new Matrix4[]
            {
                client.GetEyeView(VRHand.Left),
                client.GetEyeView(VRHand.Right)
            };
            Framebuffers = new Framebuffer[]
            {
                client.LeftFramebuffer,
                client.RightFramebuffer
            };
            iMeshGroup = new MeshGroup(MeshGroupVertexFormat.X32F_Y32F_Z32F, 256, 256);
            CurrentPlayer = new LocalPlayer(client);
        }

        public static void SetupControllers(string file, params VRActionSet[] actions)
        {
            client.InitializeControllers(file, actions);
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
            var clearCol = GraphicsDevice.ClearColor;
            var clearDepth = GraphicsDevice.ClearDepth;

            GraphicsDevice.ClearColor = new Vector4(0, 0.5f, 1.0f, 0.0f);
            GraphicsDevice.ClearDepth = InverseDepth.ClearDepth;
            for (int i = 0; i < 2; i++)
            {
                GraphicsDevice.Framebuffer = Framebuffers[i];
                GraphicsDevice.ClearDepthBuffer();
                GraphicsDevice.Clear();
            }
            GraphicsDevice.ClearColor = clearCol;
            GraphicsDevice.ClearDepth = clearDepth;
        }

        public static void Submit()
        {
            client.Submit(VRHand.Left);
            client.Submit(VRHand.Right);
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
