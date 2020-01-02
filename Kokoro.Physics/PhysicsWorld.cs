using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Physics
{
    public class PhysicsWorld
    {
        public BroadphaseSolver Broadphase { get; private set; }
        public NarrowphaseSolver Narrowphase { get; private set; }

        public PhysicsWorld()
        {
            Broadphase = new BroadphaseSolver(this);
            Narrowphase = new NarrowphaseSolver(this);
        }

        public void AddBody(PhysicsBody body)
        {
            Broadphase.AddBody(body);
        }

        public void RemoveBody(PhysicsBody body)
        {
            Broadphase.RemoveBody(body);
        }

        public void AddForce(ForceField field)
        {
            Broadphase.AddBody(field.Area);
        }

        public void RemoveForce(ForceField field)
        {
            Broadphase.RemoveBody(field.Area);
        }

        public void Update(double time)
        {
            //Maximize parallelism, divide narrow phase among threads
            var broad_collisions = Broadphase.Solve(time);
            //Solve narrow phase (subdivision intersections)
            var final_collisions = Narrowphase.Solve(time, broad_collisions);
            //Finally perform impulse resolution
        }
    }
}
