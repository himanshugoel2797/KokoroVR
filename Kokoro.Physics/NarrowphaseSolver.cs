using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Physics
{
    public class NarrowphaseSolver
    {
        private PhysicsWorld world;

        public NarrowphaseSolver(PhysicsWorld world)
        {
            this.world = world;
        }

        public PhysicsBody[] Solve(double time, PhysicsBody[][] bodies)
        {
            int count = bodies.Length / PhysicsOptions.BatchSize;
            if (bodies.Length % PhysicsOptions.BatchSize != 0) count++;


            Parallel.For(0, count, (base_idx) =>
            {
                for (int batch_idx = base_idx * PhysicsOptions.BatchSize; batch_idx < PhysicsOptions.BatchSize && batch_idx < bodies.Length; batch_idx++)
                {
                    //Check for collisions and resolve them
                    var batch = bodies[batch_idx];
                    for (int i0 = 0; i0 < batch.Length; i0++)
                        for (int i1 = i0 + 1; i1 < batch.Length; i1++)
                        {
                            PhysicsBody o0 = null;
                            PhysicsBody o1 = null;
                            if (batch[i0].Kind < batch[i1].Kind)
                            {
                                o0 = batch[i0];
                                o1 = batch[i1];
                            }
                            else
                            {
                                o0 = batch[i1];
                                o1 = batch[i0];
                            }

                            switch (o0.Kind)
                            {
                                case EntityType.FixedPlane:
                                    switch (o1.Kind)
                                    {
                                        case EntityType.FixedPlane:
                                            //Don't check
                                            break;
                                        case EntityType.OrientedBoundingBox:

                                            break;
                                        case EntityType.Point:

                                            break;
                                        case EntityType.Sphere:
                                            //Distance from sphere to plane <= radius of sphere
                                            break;
                                        case EntityType.Triangle:

                                            break;
                                    }
                                    break;
                                case EntityType.OrientedBoundingBox:
                                    switch (o1.Kind)
                                    {
                                        case EntityType.OrientedBoundingBox:

                                            break;
                                        case EntityType.Point:

                                            break;
                                        case EntityType.Sphere:

                                            break;
                                        case EntityType.Triangle:

                                            break;
                                    }
                                    break;
                                case EntityType.Point:
                                    switch (o1.Kind)
                                    {
                                        case EntityType.Point:
                                            //Doesn't make sense
                                            break;
                                        case EntityType.Sphere:
                                            //Distance between center and point squared <= radius squared
                                            break;
                                        case EntityType.Triangle:

                                            break;
                                    }
                                    break;
                                case EntityType.Sphere:
                                    switch (o1.Kind)
                                    {
                                        case EntityType.Sphere:
                                            //Distance between centers squared <= sum of radius squared
                                            break;
                                        case EntityType.Triangle:

                                            break;
                                    }
                                    break;
                                case EntityType.Triangle:
                                    switch (o1.Kind)
                                    {
                                        case EntityType.Triangle:

                                            break;
                                    }
                                    break;
                            }
                        }
                }
            });
            return null;
        }
    }
}
