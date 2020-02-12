using Kokoro.Common.StateMachine;
using Kokoro.Physics;
using KokoroVR.Graphics;
using KokoroVR.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valve.VR;

namespace KokoroVR
{
    /// <summary>
    /// Represents a 'scene' in VR. Has a physics instance, create interactables, engine manages tracking controls to simulate interactables.
    /// </summary>
    public class World : IState
    {
        private readonly List<Interactable> _interactableList;
        private readonly List<ControlInterpreter> _interpreterList;
        private readonly int maxLights;

        public Action Initializer { get; set; }
        public DeferredRenderer Renderer { get; protected set; }
        public LightManager LightManager { get; protected set; }
        public PhysicsWorld Physics { get; protected set; }
        public string Name { get; private set; }
        public StateManager WorldManager { get; internal set; }
        public IReadOnlyList<Interactable> Interactables { get { return _interactableList; } }
        public IReadOnlyList<ControlInterpreter> ControlInterpreters { get { return _interpreterList; } }

        public World(string name, int maxLights)
        {
            this.maxLights = maxLights;
            Name = name;
            Physics = new PhysicsWorld();
            _interactableList = new List<Interactable>();
            _interpreterList = new List<ControlInterpreter>();
        }

        public void AddRenderable(Interactable r)
        {
            _interactableList.Add(r);
        }

        public void RemoveRenderable(Interactable r)
        {
            _interactableList.Remove(r);
        }

        public void AddInterpreter(ControlInterpreter r)
        {
            _interpreterList.Add(r);
        }

        public void RemoveInterpreter(ControlInterpreter r)
        {
            _interpreterList.Remove(r);
        }

        public virtual void Render(double time)
        {
            Renderer.FrameStart();

            var v = Engine.View.Select(a => Engine.CurrentPlayer.Pose * a).ToArray();
            var p = Engine.Projection;

            for (int i = 0; i < Engine.Framebuffers.Length; i++)
            {
                foreach (var r in ControlInterpreters)
                    r.Render(time, Renderer.Framebuffers[i], (i == 0) ? VREye.Left : VREye.Right, VRHand.Get(i));

                foreach (var r in Interactables)
                    r.Render(time, Renderer.Framebuffers[i], (i == 0) ? VREye.Left : VREye.Right);
            }
            Renderer.Submit();
            Engine.Submit();

#if VR
            Kokoro.Graphics.Framebuffer.Default.Blit(Engine.Framebuffers[0], true, false, true);
#endif
            Kokoro.Graphics.GraphicsDevice.SwapBuffers();
        }

        public virtual void Update(double time)
        {
            //Update the controllers
            foreach (var r in ControlInterpreters)
                r.Update(time, this);

            foreach (var r in Interactables)
                r.Update(time, this);
            LightManager.Update();
            Physics.Update(time);
        }

        public virtual void Enter(StateManager man, IState prev)
        {
            WorldManager = man;

            if (LightManager == null)
                LightManager = new LightManager(maxLights, maxLights, maxLights);

            if (Renderer == null)
                Renderer = new DeferredRenderer(Engine.Framebuffers, LightManager);
            Engine.DeferredRenderer = Renderer;

            Initializer?.Invoke();
        }

        public virtual void Exit(IState next)
        {

        }
    }
}
