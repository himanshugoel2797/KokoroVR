using Kokoro.Common.StateMachine;
using Kokoro.Physics;
using KokoroVR.Graphics;
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
        private int maxLights;

        public DeferredRenderer Renderer { get; protected set; }
        public MeshRenderer MeshRenderer { get; protected set; }
        public LightManager LightManager { get; protected set; }
        public PhysicsWorld Physics { get; protected set; }
        public string Name { get; private set; }
        public StateManager WorldManager { get; internal set; }
        public IReadOnlyList<Interactable> Interactables { get { return _interactableList; } }

        public World(string name, int maxLights)
        {
            this.maxLights = maxLights;
            Name = name;
            Physics = new PhysicsWorld();
            _interactableList = new List<Interactable>();
        }

        public void AddRenderable(Interactable r)
        {
            _interactableList.Add(r);
        }

        public void RemoveRenderable(Interactable r)
        {
            _interactableList.Remove(r);
        }

        public virtual void Render(double time)
        {
            Renderer.Clear();

            var v = Engine.View.Select(a => a * Engine.CurrentPlayer.Pose).ToArray();
            var p = Engine.Projection;

            for (int i = 0; i < 2; i++)
                foreach (var r in Interactables)
                    r.Render(time, Renderer.Framebuffers[i], p[i], v[i], (i == 0) ? VREye.Left : VREye.Right);
            Renderer.Submit(v, p, Engine.CurrentPlayer.Position);
            Engine.Submit();

            Kokoro.Graphics.Framebuffer.Default.Blit(Renderer.Framebuffers[0], true, false, true);
            Kokoro.Graphics.GraphicsDevice.SwapBuffers();
        }

        public virtual void Update(double time)
        {
            //Update the controllers
            foreach (var r in Interactables)
                r.Update(time);
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

            if (MeshRenderer == null)
                MeshRenderer = new MeshRenderer();
        }

        public void Exit(IState next)
        {

        }
    }
}
