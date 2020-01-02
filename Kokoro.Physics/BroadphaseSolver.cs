using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Physics
{
    public class BroadphaseSolver
    {
        private PhysicsWorld parentWorld;
        private List<PhysicsBody>[] _objs;

        const int X = 0, Y = 1, Z = 2;

        public BroadphaseSolver(PhysicsWorld world)
        {
            parentWorld = world;

            _objs = new List<PhysicsBody>[]
            {
                new List<PhysicsBody>(),
                new List<PhysicsBody>(),
                new List<PhysicsBody>(),
            };
        }

        public void AddBody(PhysicsBody body)
        {
            //Insert body into proper, sorted location
            for (int a = 0; a < 3; a++)
            {
                int i = 0;
                while (i < _objs[a].Count && _objs[a][i].CenterOfMass[a] <= body.CenterOfMass[a])
                    i++;
                _objs[a].Insert(i, body);
            }
        }

        public void RemoveBody(PhysicsBody body)
        {
            for (int a = 0; a < 3; a++)
                _objs[a].Remove(body);
        }

        public PhysicsBody[][] Solve(double time)
        {
            //Check all points within range of each body and make a list of all intersecting axes
            //Create a set for each 'group' of interactions
            HashSet<PhysicsBody> netSet = null;
            List<HashSet<PhysicsBody>> finalSets = null;
            for (int a = 0; a < 3; a++)
            {
                HashSet<PhysicsBody> collisionNetSets = new HashSet<PhysicsBody>();
                List<HashSet<PhysicsBody>> collisionSets = new List<HashSet<PhysicsBody>>();

                for (int i = 0; i < _objs[a].Count; i++)
                {
                    var obj_i = _objs[a][i];
                    for (int j = i + 1; j < _objs[a].Count; j++)
                    {
                        var obj_j = _objs[a][j];

                        float intersec_dist = (obj_i.CenterOfMass[a] + obj_i.BoundingRadius) - (obj_j.CenterOfMass[a] - obj_j.BoundingRadius);
                        if (intersec_dist >= 0)
                        {
                            //Intersection (>0) or contact (=0)
                            collisionNetSets.Add(obj_j);
                            collisionNetSets.Add(obj_i);

                            bool added = false;
                            for (int k = 0; k < collisionSets.Count; k++)
                                if (collisionSets[k].Contains(obj_i))
                                {
                                    added = true;
                                    collisionSets[k].Add(obj_j);
                                    break;
                                }
                                else if (collisionSets[k].Contains(obj_j))
                                {
                                    added = true;
                                    collisionSets[k].Add(obj_i);
                                    break;
                                }
                            if (!added)
                            {
                                var set = new HashSet<PhysicsBody>();
                                set.Add(obj_j);
                                set.Add(obj_i);
                                collisionSets.Add(set);
                            }
                        }
                    }
                }

                if (a == 0)
                {
                    finalSets = collisionSets;
                    netSet = collisionNetSets;
                }
                else
                    netSet = netSet.Intersect(collisionNetSets).ToHashSet();
            }

            var collisionGroups = new PhysicsBody[finalSets.Count][];
            for (int i = 0; i < finalSets.Count; i++)
                collisionGroups[i] = finalSets[i].Intersect(netSet).ToArray();

            return collisionGroups;
        }
    }
}
