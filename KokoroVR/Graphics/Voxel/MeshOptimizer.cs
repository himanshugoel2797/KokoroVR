using Kokoro.Math;
using Kokoro.Math.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Graphics.Voxel
{
    class MeshOptimizer : Graph<(byte, byte, byte, byte)>
    {
        private Vector3 n;
        public MeshOptimizer(Vector3 normal)
        {
            n = normal;
        }

        private bool clockwise(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 c0 = b - a;
            Console.WriteLine(b);
            Console.WriteLine(c0);
            Vector3 c1 = c - a;
            Vector3 norm = Vector3.Cross(c0, c1);

            return n == norm;
        }

        private (byte, byte, byte, byte)[] ExtractPoly()
        {
            var points = new List<(byte, byte, byte, byte)>();
            var keys = Nodes.Keys.OrderByDescending(a => Nodes[a].Count).ToArray();

            var key = keys[0];
            do
            {
                //Determine which windings lead forward
                var fwd_winding_idxs = new List<int>();
                var bck_winding_idxs = new List<int>();
                for (int i = 0; i < Windings[key].Count; i++)
                    if (Windings[key][i] == 1)
                        fwd_winding_idxs.Add(i);
                    else
                        bck_winding_idxs.Add(i);

                if (fwd_winding_idxs.Count == 1)
                {
                    //Choose only possible path
                    var n_key = Nodes[key][fwd_winding_idxs[0]];
                    points.Add(key);
                    RemoveConnection(key, n_key);
                    key = n_key;
                }
                else
                {
                    //arbitrarily choose the first backward leading point as our current path if no previous point is available
                    var bck = points.Count == 0 ? Nodes[key][bck_winding_idxs[0]] : points.Last();
                    var fwd0 = Nodes[key][fwd_winding_idxs[0]];
                    var fwd1 = Nodes[key][fwd_winding_idxs[1]];

                    //determine which one leads clockwise
                    Vector3 k_v = new Vector3(key.Item1, key.Item2, key.Item3);
                    Vector3 bck_v = new Vector3(bck.Item1, bck.Item2, bck.Item3);
                    Vector3 fwd0_v = new Vector3(fwd0.Item1, fwd0.Item2, fwd0.Item3);
                    Vector3 fwd1_v = new Vector3(fwd1.Item1, fwd1.Item2, fwd1.Item3);

                    if (clockwise(k_v, bck_v, fwd0_v))
                    {
                        //Choose fwd0
                        var n_key = Nodes[key][fwd_winding_idxs[0]];
                        points.Add(key);
                        RemoveConnection(key, n_key);
                        key = n_key;
                    }
                    else
                    {
                        //Choose fwd1
                        var n_key = Nodes[key][fwd_winding_idxs[1]];
                        points.Add(key);
                        RemoveConnection(key, n_key);
                        key = n_key;
                    }
                }
            } while (!points.Contains(key));

            return points.ToArray();
        }

        public void ReduceQuads(byte[] quads)
        {
            for (int i = 0; i < quads.Length / 4; i += 4)
            {
                var v0 = (quads[i * 4 + 0], quads[i * 4 + 1], quads[i * 4 + 2], quads[i * 4 + 3]);
                var v1 = (quads[(i + 1) * 4 + 0], quads[(i + 1) * 4 + 1], quads[(i + 1) * 4 + 2], quads[(i + 1) * 4 + 3]);
                var v2 = (quads[(i + 2) * 4 + 0], quads[(i + 2) * 4 + 1], quads[(i + 2) * 4 + 2], quads[(i + 2) * 4 + 3]);
                var v3 = (quads[(i + 3) * 4 + 0], quads[(i + 3) * 4 + 1], quads[(i + 3) * 4 + 2], quads[(i + 3) * 4 + 3]);

                AddConnection(v0, v1);
                AddConnection(v1, v2);
                AddConnection(v2, v3);
                AddConnection(v3, v0);
            }

            var keys = Nodes.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                if (Nodes.ContainsKey(key))
                    for (int j = 0; j < Nodes[key].Count; j++)
                        if (Windings[key][j] == 0)
                        {
                            RemoveConnection(key, Nodes[key][j]);
                            j--;
                            if (!Nodes.ContainsKey(key))
                                break;
                        }
            }

            keys = Nodes.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                if (Nodes.ContainsKey(key) && Nodes[key].Count == 2)
                {
                    if (Windings[key][0] == 1 && Windings[key][1] == -1)
                    {
                        //Forward from 1 to 0
                        var incoming_node = Nodes[key][1];
                        var outgoing_node = Nodes[key][0];

                        Vector3 incoming_v = new Vector3(incoming_node.Item1, incoming_node.Item2, incoming_node.Item3);
                        Vector3 outgoing_v = new Vector3(outgoing_node.Item1, outgoing_node.Item2, outgoing_node.Item3);
                        Vector3 key_v = new Vector3(key.Item1, key.Item2, key.Item3);

                        Vector3 c0_v = incoming_v - key_v;
                        Vector3 c1_v = outgoing_v - key_v;

                        //Only merge if all three are colinear
                        //Due to vertices only being at either 90 degree or 180 degree angles, if the dot product is not 0, the vectors are colinear
                        if (Vector3.Dot(c0_v, c1_v) != 0)
                        {
                            int rep_idx = Nodes[incoming_node].IndexOf(key);
                            Nodes[incoming_node][rep_idx] = outgoing_node;

                            rep_idx = Nodes[outgoing_node].IndexOf(key);
                            Nodes[outgoing_node][rep_idx] = incoming_node;

                            Nodes.Remove(key);
                            Windings.Remove(key);
                        }
                    }
                    else if (Windings[key][1] == 1 && Windings[key][0] == -1)
                    {
                        //Forward from 1 to 0
                        var incoming_node = Nodes[key][0];
                        var outgoing_node = Nodes[key][1];

                        Vector3 incoming_v = new Vector3(incoming_node.Item1, incoming_node.Item2, incoming_node.Item3);
                        Vector3 outgoing_v = new Vector3(outgoing_node.Item1, outgoing_node.Item2, outgoing_node.Item3);
                        Vector3 key_v = new Vector3(key.Item1, key.Item2, key.Item3);

                        Vector3 c0_v = incoming_v - key_v;
                        Vector3 c1_v = outgoing_v - key_v;

                        //Only merge if all three are colinear
                        //Due to vertices only being at either 90 degree or 180 degree angles, if the dot product is not 0, the vectors are colinear
                        if (Vector3.Dot(c0_v, c1_v) != 0)
                        {
                            int rep_idx = Nodes[incoming_node].IndexOf(key);
                            Nodes[incoming_node][rep_idx] = outgoing_node;

                            rep_idx = Nodes[outgoing_node].IndexOf(key);
                            Nodes[outgoing_node][rep_idx] = incoming_node;

                            Nodes.Remove(key);
                            Windings.Remove(key);
                        }
                    }
                }
            }

            //Now extract the remaining polygons and triangulate
            var net_polys = new List<(byte, byte, byte, byte)[]>();
            var net_holes = new List<(byte, byte, byte, byte)[]>();
            while (Nodes.Count > 0)
            {
                var cur_loop = ExtractPoly();

                var bck = cur_loop[0];
                var key = cur_loop[1];
                var fwd0 = cur_loop[2];
                Vector3 k_v = new Vector3(key.Item1, key.Item2, key.Item3);
                Vector3 bck_v = new Vector3(bck.Item1, bck.Item2, bck.Item3);
                Vector3 fwd0_v = new Vector3(fwd0.Item1, fwd0.Item2, fwd0.Item3);

                //All clockwise loops are faces
                if (clockwise(k_v, bck_v, fwd0_v))
                    net_polys.Add(cur_loop);
                else
                    //All counterclockwise loops are holes
                    net_holes.Add(cur_loop);
            }

            //Compute planes and bounds for each loop
            //Use the plane and bounds to assign holes to loops
            //Generate lines along every 2 hole vertices
            //Insert vertices in outer loop where these lines intersect with edges
            //Generate two triangles for every 4 vertices in the outer loop
        }
    }
}
