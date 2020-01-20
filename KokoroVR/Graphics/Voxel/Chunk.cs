using Kokoro.Graphics;
using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KokoroVR.Graphics.Voxel
{
    public class Chunk
    {
        private const byte VisibleBit = 1;

        internal int id;
        internal ChunkStreamer streamer;
        internal bool dirty, update_pending, empty;
        internal byte DefaultEdgeVisibility = 0;

        internal byte[] data;
        internal byte[] vis;
        internal HashSet<Vector3>[] face_quads;
        internal byte[] faces;
        internal uint[] indices;
        internal Vector4[] bounds;
        internal byte[] norm_mask;

        public int VoxelCount { get; private set; }
        public ChunkObject Owner { get; internal set; }

        internal Chunk(ChunkStreamer streamer, int id)
        {
            data = new byte[ChunkConstants.Side * ChunkConstants.Side * ChunkConstants.Side];
            vis = new byte[ChunkConstants.Side * ChunkConstants.Side * ChunkConstants.Side];

            face_quads = new HashSet<Vector3>[6];
            for (int i = 0; i < face_quads.Length; i++) face_quads[i] = new HashSet<Vector3>();

            this.streamer = streamer;
            this.id = id;
            empty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetIndex(int x, int y, int z)
        {
            return (x & (ChunkConstants.Side - 1)) << (2 * ChunkConstants.SideLog) | (y & (ChunkConstants.Side - 1)) << ChunkConstants.SideLog | (z & (ChunkConstants.Side - 1));
        }

        private void FloodFillVisibility(byte[] vis, int x, int y, int z)
        {
            if (data[GetIndex(x, y, z)] != 0)
            {
                //Figure out which side of the face is visible based on the chunk face being sampled
                //TODO add neighbor checking here
                var vec = new Vector3(x, y, z);
                if (x == 0)
                {
                    face_quads[0].Add(vec);
                }

                if (x == ChunkConstants.Side - 1)
                {
                    face_quads[1].Add(vec);
                }

                if (y == 0)
                {
                    face_quads[2].Add(vec);
                }

                if (y == ChunkConstants.Side - 1)
                {
                    face_quads[3].Add(vec);
                }

                if (z == 0)
                {
                    face_quads[4].Add(vec);
                }

                if (z == ChunkConstants.Side - 1)
                {
                    face_quads[5].Add(vec);
                }

                return;
            }

            vis[GetIndex(x, y, z)] = VisibleBit;
            Queue<(int, int, int)> nodes = new Queue<(int, int, int)>();
            nodes.Enqueue((x, y, z));

            int x_b = x;
            int y_b = y;
            int z_b = z;

            while (nodes.Count > 0)
            {
                (x, y, z) = nodes.Dequeue();    //All nodes in this queue are known visible, so any solid neighbors can immediately be extracted for further processing

                if (x > 0 && vis[GetIndex(x - 1, y, z)] != VisibleBit)
                {
                    if (data[GetIndex(x - 1, y, z)] == 0)
                    {
                        vis[GetIndex(x - 1, y, z)] = VisibleBit;
                        nodes.Enqueue((x - 1, y, z));
                    }
                    else
                    {
                        //lft = 5
                        face_quads[1].Add(new Vector3(x - 1, y, z));
                    }
                }
                if (y > 0 && vis[GetIndex(x, y - 1, z)] != VisibleBit)
                {
                    if (data[GetIndex(x, y - 1, z)] == 0)
                    {
                        vis[GetIndex(x, y - 1, z)] = VisibleBit;
                        nodes.Enqueue((x, y - 1, z));
                    }
                    else
                    {
                        //btm = 1
                        face_quads[3].Add(new Vector3(x, y - 1, z));
                    }
                }
                if (z > 0 && vis[GetIndex(x, y, z - 1)] != VisibleBit)
                {
                    if (data[GetIndex(x, y, z - 1)] == 0)
                    {
                        vis[GetIndex(x, y, z - 1)] = VisibleBit;
                        nodes.Enqueue((x, y, z - 1));
                    }
                    else
                    {
                        //bck = 3
                        face_quads[5].Add(new Vector3(x, y, z - 1));
                    }
                }
                if (x < ChunkConstants.Side - 1 && vis[GetIndex(x + 1, y, z)] != VisibleBit)
                {
                    if (data[GetIndex(x + 1, y, z)] == 0)
                    {
                        vis[GetIndex(x + 1, y, z)] = VisibleBit;
                        nodes.Enqueue((x + 1, y, z));
                    }
                    else
                    {
                        //rgt = 4
                        face_quads[0].Add(new Vector3(x + 1, y, z));
                    }
                }
                if (y < ChunkConstants.Side - 1 && vis[GetIndex(x, y + 1, z)] != VisibleBit)
                {
                    if (data[GetIndex(x, y + 1, z)] == 0)
                    {
                        vis[GetIndex(x, y + 1, z)] = VisibleBit;
                        nodes.Enqueue((x, y + 1, z));
                    }
                    else
                    {
                        //top = 0
                        face_quads[2].Add(new Vector3(x, y + 1, z));
                    }
                }
                if (z < ChunkConstants.Side - 1 && vis[GetIndex(x, y, z + 1)] != VisibleBit)
                {
                    if (data[GetIndex(x, y, z + 1)] == 0)
                    {
                        vis[GetIndex(x, y, z + 1)] = VisibleBit;
                        nodes.Enqueue((x, y, z + 1));
                    }
                    else
                    {
                        //frt = 2
                        face_quads[4].Add(new Vector3(x, y, z + 1));
                    }
                }
            }
        }

        private void ComputeVisibility()
        {
            {
                int x = 0;
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        FloodFillVisibility(vis, x, y, z);
            }
            {
                int x = ChunkConstants.Side - 1;
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        FloodFillVisibility(vis, x, y, z);
            }
            {
                int x = 0;
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        FloodFillVisibility(vis, y, x, z);
            }
            {
                int x = ChunkConstants.Side - 1;
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        FloodFillVisibility(vis, y, x, z);
            }
            {
                int x = 0;
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        FloodFillVisibility(vis, z, y, x);
            }
            {
                int x = ChunkConstants.Side - 1;
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        FloodFillVisibility(vis, z, y, x);
            }
        }

        public void RebuildFullMesh(params Chunk[] neighbors)
        {
            VoxelCount = 0;

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var faces = new List<byte>();
            var indices = new List<uint>();
            var indexDict = new Dictionary<int, ushort>();
            ushort idx_cntr = 0;

            ComputeVisibility();

            //for each height value along each axis, extract faces
            for (int f_idx = 0; f_idx < 6; f_idx++)
            {
                if (face_quads[f_idx].Count == 0) continue;

                int h_idx = f_idx >> 1;
                int r_idx = (h_idx + 1) % 3;
                int c_idx = (h_idx + 2) % 3;

                var sorted_faces = face_quads[f_idx].OrderBy(a => a[h_idx] * (ChunkConstants.Side * ChunkConstants.Side) + a[r_idx] * ChunkConstants.Side + a[c_idx]).ToArray();
                //now everything has been sorted into runs of (c, r, h), so form runs along 'c'
                //TODO perform runs across both c and r axis
                var run_start = sorted_faces[0];
                var run_len = 1;
                var run_v = data[GetIndex((int)run_start.X, (int)run_start.Y, (int)run_start.Z)];
                for (int i = 1; i < sorted_faces.Length; i++)
                {
                    var v = data[GetIndex((int)sorted_faces[i].X, (int)sorted_faces[i].Y, (int)sorted_faces[i].Z)];
                    if (sorted_faces[i][r_idx] != run_start[r_idx] | sorted_faces[i][h_idx] != run_start[h_idx] | sorted_faces[i][c_idx] != run_start[c_idx] + run_len | v != run_v)
                    {
                        //Emit the current run as a face
                        //hold h_idx constant
                        var c_verts = new byte[4 * 3];
                        c_verts[h_idx] = (byte)(run_start[h_idx] + (f_idx & 1));
                        c_verts[3 + h_idx] = (byte)(run_start[h_idx] + (f_idx & 1));
                        c_verts[6 + h_idx] = (byte)(run_start[h_idx] + (f_idx & 1));
                        c_verts[9 + h_idx] = (byte)(run_start[h_idx] + (f_idx & 1));

                        //grid is r_idx and c_idx based
                        c_verts[c_idx] = (byte)run_start[c_idx];
                        c_verts[3 + c_idx] = (byte)run_start[c_idx];
                        c_verts[6 + c_idx] = (byte)(run_start[c_idx] + run_len);
                        c_verts[9 + c_idx] = (byte)(run_start[c_idx] + run_len);

                        c_verts[r_idx] = (byte)run_start[r_idx];
                        c_verts[3 + r_idx] = (byte)(run_start[r_idx] + 1);
                        c_verts[6 + r_idx] = (byte)(run_start[r_idx] + 1);
                        c_verts[9 + r_idx] = (byte)run_start[r_idx];

                        //get the indices for each vertex
                        var c_indices = new ushort[4];
                        for (int j = 0; j < 4; j++)
                        {
                            int v_idx = c_verts[j * 3 + 0] | c_verts[j * 3 + 1] << 8 | c_verts[j * 3 + 2] << 16;
                            if (indexDict.ContainsKey(v_idx))
                            {
                                c_indices[j] = indexDict[v_idx];
                            }
                            else
                            {
                                indexDict[v_idx] = idx_cntr;
                                faces.Add(c_verts[j * 3 + 0]);
                                faces.Add(c_verts[j * 3 + 1]);
                                faces.Add(c_verts[j * 3 + 2]);
                                faces.Add(0);
                                c_indices[j] = idx_cntr++;
                            }
                        }

                        //emit indices based on winding
                        if ((f_idx & 1) == 0)
                        {
                            //points along positive axes
                            indices.Add((uint)(run_v << 16 | c_indices[0]));
                            indices.Add((uint)(run_v << 16 | c_indices[3]));
                            indices.Add((uint)(run_v << 16 | c_indices[2]));
                            indices.Add((uint)(run_v << 16 | c_indices[0]));
                            indices.Add((uint)(run_v << 16 | c_indices[2]));
                            indices.Add((uint)(run_v << 16 | c_indices[1]));
                        }
                        else
                        {
                            //points along negative axes
                            indices.Add((uint)(run_v << 16 | c_indices[2]));
                            indices.Add((uint)(run_v << 16 | c_indices[3]));
                            indices.Add((uint)(run_v << 16 | c_indices[0]));
                            indices.Add((uint)(run_v << 16 | c_indices[1]));
                            indices.Add((uint)(run_v << 16 | c_indices[2]));
                            indices.Add((uint)(run_v << 16 | c_indices[0]));
                        }

                        run_start = sorted_faces[i];
                        run_len = 1;
                        run_v = v;
                    }
                    else
                    {
                        //continue this run
                        run_len++;
                    }
                }

                //Process final run
                {
                    //Emit the current run as a face
                    //hold h_idx constant
                    var c_verts = new byte[4 * 3];
                    c_verts[h_idx] = (byte)(run_start[h_idx] + (f_idx & 1));
                    c_verts[3 + h_idx] = (byte)(run_start[h_idx] + (f_idx & 1));
                    c_verts[6 + h_idx] = (byte)(run_start[h_idx] + (f_idx & 1));
                    c_verts[9 + h_idx] = (byte)(run_start[h_idx] + (f_idx & 1));

                    //grid is r_idx and c_idx based
                    c_verts[c_idx] = (byte)run_start[c_idx];
                    c_verts[3 + c_idx] = (byte)run_start[c_idx];
                    c_verts[6 + c_idx] = (byte)(run_start[c_idx] + run_len);
                    c_verts[9 + c_idx] = (byte)(run_start[c_idx] + run_len);

                    c_verts[r_idx] = (byte)run_start[r_idx];
                    c_verts[3 + r_idx] = (byte)(run_start[r_idx] + 1);
                    c_verts[6 + r_idx] = (byte)(run_start[r_idx] + 1);
                    c_verts[9 + r_idx] = (byte)run_start[r_idx];

                    //get the indices for each vertex
                    var c_indices = new ushort[4];
                    for (int j = 0; j < 4; j++)
                    {
                        int v_idx = c_verts[j * 3 + 0] | c_verts[j * 3 + 1] << 8 | c_verts[j * 3 + 2] << 16;
                        if (indexDict.ContainsKey(v_idx))
                        {
                            c_indices[j] = indexDict[v_idx];
                        }
                        else
                        {
                            indexDict[v_idx] = idx_cntr;
                            faces.Add(c_verts[j * 3 + 0]);
                            faces.Add(c_verts[j * 3 + 1]);
                            faces.Add(c_verts[j * 3 + 2]);
                            faces.Add(0);
                            c_indices[j] = idx_cntr++;
                        }
                    }

                    //emit indices based on winding
                    if ((f_idx & 1) == 0)
                    {
                        //points along positive axes
                        indices.Add((uint)(run_v << 16 | c_indices[0]));
                        indices.Add((uint)(run_v << 16 | c_indices[3]));
                        indices.Add((uint)(run_v << 16 | c_indices[2]));
                        indices.Add((uint)(run_v << 16 | c_indices[0]));
                        indices.Add((uint)(run_v << 16 | c_indices[2]));
                        indices.Add((uint)(run_v << 16 | c_indices[1]));
                    }
                    else
                    {
                        //points along negative axes
                        indices.Add((uint)(run_v << 16 | c_indices[2]));
                        indices.Add((uint)(run_v << 16 | c_indices[3]));
                        indices.Add((uint)(run_v << 16 | c_indices[0]));
                        indices.Add((uint)(run_v << 16 | c_indices[1]));
                        indices.Add((uint)(run_v << 16 | c_indices[2]));
                        indices.Add((uint)(run_v << 16 | c_indices[0]));
                    }
                }
            }

            //TODO redo system to compute visibility map once, apply neighbor face visibility too, compute to sets of runs along X or Y axes per layer
            //TODO test rendering triangles with compute culling instead of cluster culling

            this.faces = faces.ToArray();
            this.indices = indices.ToArray();

            stopwatch.Stop();
            Console.WriteLine($"[{id}] Meshing time: {stopwatch.Elapsed.TotalMilliseconds}ms");
            for (int i = 0; i < 6; i++) face_quads[i].Clear();
            Array.Clear(vis, 0, vis.Length);

            /*
            string str = "";
            string indi = "";
            for (int i = 0; i < faces.Count / 4; i++)
            {
                str += $"v {faces[i * 4]} {faces[i * 4 + 1]} {faces[i * 4 + 2]}\n";
            }

            for (int i = 0; i < indices.Count; i += 3)
            {
                indi += $"f {(indices[i] & 0xffff) + 1} {(indices[i + 1] & 0xffff) + 1} {(indices[i + 2] & 0xffff) + 1}\n";
            }
            File.WriteAllText($"tmp{id}.obj", str + indi);*/
            empty = (this.faces.Length == 0);

            dirty = false;
            update_pending = true;
        }

        public void EditLocalMesh(int x, int y, int z, byte cur)
        {
            //Just edit the current value
            var prev_data = data[GetIndex(x, y, z)];
            if (prev_data != cur) dirty = true;
            if (cur == 0 && prev_data != 0)
                VoxelCount--;
            else if (cur != 0 && prev_data == 0)
                VoxelCount++;

            data[GetIndex(x, y, z)] = cur;
        }

        public void Update()
        {
            // if (dirty)
            //RebuildFullMesh();
        }
    }
}