using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Physics
{
    public abstract class PhysicsBody
    {
        private static uint ID_base = 1;
        internal uint ID;
        internal EntityType Kind;

        public Vector3 CenterOfMass;
        public float BoundingRadius;

        public PhysicsBody(Vector3 center = default, float boundingRadius = 0, EntityType kind = EntityType.FixedPlane)
        {
            CenterOfMass = center;
            BoundingRadius = boundingRadius;
            ID = ID_base++;
            Kind = kind;
        }

        //Verlet integrator
        //Point vs Triangle
        //Point vs Sphere
        //Point vs OBB
        //Triangle vs Triangle
        //Sphere vs Sphere
        //OBB vs OBB

        //Point vs Plane
        //Triangle vs Plane
        //Sphere vs Plane
        //OBB vs Plane

        //Sphere vs Sphere broadphase
        //OBB vs OBB narrowphase
        //Point resolution after filtering
    }
}
