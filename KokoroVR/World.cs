using Kokoro.Common.StateMachine;
using Kokoro.Physics;
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
        private List<Interactable> _renderableList;

        public PhysicsWorld Physics { get; protected set; }
        public string Name { get; private set; }
        public StateManager WorldManager { get; internal set; }
        public IReadOnlyList<Interactable> Renderables { get { return _renderableList; } }

        public World(string name)
        {
            Name = name;
            Physics = new PhysicsWorld();
            _renderableList = new List<Interactable>();
        }

        public void AddRenderable(Interactable r)
        {
            _renderableList.Add(r);
        }

        public void RemoveRenderable(Interactable r)
        {
            _renderableList.Remove(r);
        }

        public virtual void Render(double time)
        {
            Engine.Clear();
            for (int i = 0; i < 2; i++)
                foreach (var r in Renderables)
                    r.Render(time, Engine.Framebuffers[i], (i == 0) ? VREye.Left : VREye.Right);
            Engine.Submit();

            Kokoro.Graphics.Framebuffer.Default.Blit(Engine.Framebuffers[0], true, false, true);
            Kokoro.Graphics.GraphicsDevice.SwapBuffers();
        }

        public virtual void Update(double time)
        {
            //Update the controllers
            foreach (var r in Renderables)
                r.Update(time);
            Physics.Update(time);
        }

        public void Enter(StateManager man, IState prev)
        {
            WorldManager = man;
        }

        public void Exit(IState next)
        {

        }
    }
}
