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
        private List<Interactable> _interactableList;
        private List<ControlInterpreter> _interpreterList;
        private int maxLights;

        public DeferredRenderer Renderer { get; protected set; }
        public Action Initializer { get; set; }
        public StaticMeshRenderer StaticMeshRenderer { get; protected set; }
        public DynamicMeshRenderer DynamicMeshRenderer { get; protected set; }
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
            Renderer.Clear();

            var v = Engine.View.Select(a => Engine.CurrentPlayer.Pose * a).ToArray();
            var p = Engine.Projection;

            StaticMeshRenderer.SetMatrices(p, v);
            for (int i = 0; i < Engine.Framebuffers.Length; i++)
            {
                foreach (var r in ControlInterpreters)
                    r.Render(time, Renderer.Framebuffers[i], StaticMeshRenderer, DynamicMeshRenderer, p[i], v[i], VRHand.Get(i));

                foreach (var r in Interactables)
                    r.Render(time, Renderer.Framebuffers[i], StaticMeshRenderer, DynamicMeshRenderer, p[i], v[i], (i == 0) ? VREye.Left : VREye.Right);
            }
            StaticMeshRenderer.Submit();
            Renderer.Submit(v, p, Engine.CurrentPlayer.Position);
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

        public void Enter(StateManager man, IState prev)
        {
            WorldManager = man;

            if (LightManager == null)
                LightManager = new LightManager(maxLights, maxLights, maxLights);

            if (Renderer == null)
                Renderer = new DeferredRenderer(Engine.Framebuffers, LightManager);

            if (StaticMeshRenderer == null)
                StaticMeshRenderer = new StaticMeshRenderer(50000, Renderer);

            if (DynamicMeshRenderer == null)
                DynamicMeshRenderer = new DynamicMeshRenderer();

            if (Initializer != null)
                Initializer();
        }

        public void Exit(IState next)
        {

        }
    }
}
