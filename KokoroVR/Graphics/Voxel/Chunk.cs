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
        //internal HashSet<Vector3>[] face_quads;
        internal byte[] faces;
        internal uint[] indices;
        internal Vector4[] boundSpheres;
        internal BoundingBox[] boundAABBs;
        internal byte[] norm_mask;

        public int VoxelCount { get; private set; }
        public ChunkObject Owner { get; internal set; }

        internal Chunk(ChunkStreamer streamer, int id)
        {
            data = new byte[ChunkConstants.Side * ChunkConstants.Side * ChunkConstants.Side];
            vis = new byte[ChunkConstants.Side * ChunkConstants.Side * ChunkConstants.Side];

            //face_quads = new HashSet<Vector3>[6];
            //for (int i = 0; i < face_quads.Length; i++) face_quads[i] = new HashSet<Vector3>();

            this.streamer = streamer;
            this.id = id;
            empty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetIndex(int x, int y, int z)
        {
            return (x & (ChunkConstants.Side - 1)) << (2 * ChunkConstants.SideLog) | (y & (ChunkConstants.Side - 1)) << ChunkConstants.SideLog | (z & (ChunkConstants.Side - 1));
        }

        private void FloodFillVisibility(byte[] vis, int x, int y, int z, Chunk[] neighbors)
        {
            if (data[GetIndex(x, y, z)] != 0)
            {
                //Figure out which side of the face is visible based on the chunk face being sampled
                //TODO add neighbor checking here
                //var vec = new Vector3(x, y, z);
                if (x == 0)
                {
                    if (!(neighbors[4] != null && neighbors[4].data[GetIndex(ChunkConstants.Side - 1, y, z)] != 0))
                        vis[GetIndex(x, y, z)] |= (1 << 1);
                    //face_quads[0].Add(vec);
                }

                if (x == ChunkConstants.Side - 1)
                {
                    if (!(neighbors[5] != null && neighbors[5].data[GetIndex(0, y, z)] != 0))
                        vis[GetIndex(x, y, z)] |= (1 << 2);
                    //face_quads[1].Add(vec);
                }

                if (y == 0)
                {
                    if (!(neighbors[1] != null && neighbors[1].data[GetIndex(x, ChunkConstants.Side - 1, z)] != 0))
                        vis[GetIndex(x, y, z)] |= (1 << 3);
                    //face_quads[2].Add(vec);
                }

                if (y == ChunkConstants.Side - 1)
                {
                    if (!(neighbors[0] != null && neighbors[0].data[GetIndex(x, 0, z)] != 0))
                        vis[GetIndex(x, y, z)] |= (1 << 4);
                    //face_quads[3].Add(vec);
                }

                if (z == 0)
                {
                    if (!(neighbors[3] != null && neighbors[3].data[GetIndex(x, y, ChunkConstants.Side - 1)] != 0))
                        vis[GetIndex(x, y, z)] |= (1 << 5);
                    //face_quads[4].Add(vec);
                }

                if (z == ChunkConstants.Side - 1)
                {
                    if (!(neighbors[2] != null && neighbors[2].data[GetIndex(x, y, 0)] != 0))
                        vis[GetIndex(x, y, z)] |= (1 << 6);
                    //face_quads[5].Add(vec);
                }

                return;
            }

            vis[GetIndex(x, y, z)] = VisibleBit;
            Queue<(int, int, int)> nodes = new Queue<(int, int, int)>();
            nodes.Enqueue((x, y, z));

            //int x_b = x;
            //int y_b = y;
            //int z_b = z;

            while (nodes.Count > 0)
            {
                (x, y, z) = nodes.Dequeue();    //All nodes in this queue are known visible, so any solid neighbors can immediately be extracted for further processing

                if (x > 0 && (vis[GetIndex(x - 1, y, z)] & VisibleBit) != VisibleBit)
                {
                    if (data[GetIndex(x - 1, y, z)] == 0)
                    {
                        vis[GetIndex(x - 1, y, z)] |= VisibleBit;
                        nodes.Enqueue((x - 1, y, z));
                    }
                    else
                    {
                        //lft = 5
                        vis[GetIndex(x - 1, y, z)] |= (1 << 2);
                        //face_quads[1].Add(new Vector3(x - 1, y, z));
                    }
                }
                if (y > 0 && (vis[GetIndex(x, y - 1, z)] & VisibleBit) != VisibleBit)
                {
                    if (data[GetIndex(x, y - 1, z)] == 0)
                    {
                        vis[GetIndex(x, y - 1, z)] |= VisibleBit;
                        nodes.Enqueue((x, y - 1, z));
                    }
                    else
                    {
                        //btm = 1
                        vis[GetIndex(x, y - 1, z)] |= (1 << 4);
                        //face_quads[3].Add(new Vector3(x, y - 1, z));
                    }
                }
                if (z > 0 && (vis[GetIndex(x, y, z - 1)] & VisibleBit) != VisibleBit)
                {
                    if (data[GetIndex(x, y, z - 1)] == 0)
                    {
                        vis[GetIndex(x, y, z - 1)] |= VisibleBit;
                        nodes.Enqueue((x, y, z - 1));
                    }
                    else
                    {
                        //bck = 3
                        vis[GetIndex(x, y, z - 1)] |= (1 << 6);
                        //face_quads[5].Add(new Vector3(x, y, z - 1));
                    }
                }
                if (x < ChunkConstants.Side - 1 && (vis[GetIndex(x + 1, y, z)] & VisibleBit) != VisibleBit)
                {
                    if (data[GetIndex(x + 1, y, z)] == 0)
                    {
                        vis[GetIndex(x + 1, y, z)] |= VisibleBit;
                        nodes.Enqueue((x + 1, y, z));
                    }
                    else
                    {
                        //rgt = 4
                        vis[GetIndex(x + 1, y, z)] |= (1 << 1);
                        //face_quads[0].Add(new Vector3(x + 1, y, z));
                    }
                }
                if (y < ChunkConstants.Side - 1 && (vis[GetIndex(x, y + 1, z)] & VisibleBit) != VisibleBit)
                {
                    if (data[GetIndex(x, y + 1, z)] == 0)
                    {
                        vis[GetIndex(x, y + 1, z)] |= VisibleBit;
                        nodes.Enqueue((x, y + 1, z));
                    }
                    else
                    {
                        //top = 0
                        vis[GetIndex(x, y + 1, z)] |= (1 << 3);
                        //face_quads[2].Add(new Vector3(x, y + 1, z));
                    }
                }
                if (z < ChunkConstants.Side - 1 && (vis[GetIndex(x, y, z + 1)] & VisibleBit) != VisibleBit)
                {
                    if (data[GetIndex(x, y, z + 1)] == 0)
                    {
                        vis[GetIndex(x, y, z + 1)] |= VisibleBit;
                        nodes.Enqueue((x, y, z + 1));
                    }
                    else
                    {
                        //frt = 2
                        vis[GetIndex(x, y, z + 1)] |= (1 << 5);
                        //face_quads[4].Add(new Vector3(x, y, z + 1));
                    }
                }
            }
        }

        private void ComputeVisibility(Chunk[] neighbors)
        {
            {
                int x = 0;
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        FloodFillVisibility(vis, x, y, z, neighbors);
            }
            {
                int x = ChunkConstants.Side - 1;
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        FloodFillVisibility(vis, x, y, z, neighbors);
            }
            {
                int x = 0;
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        FloodFillVisibility(vis, y, x, z, neighbors);
            }
            {
                int x = ChunkConstants.Side - 1;
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        FloodFillVisibility(vis, y, x, z, neighbors);
            }
            {
                int x = 0;
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        FloodFillVisibility(vis, z, y, x, neighbors);
            }
            {
                int x = ChunkConstants.Side - 1;
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        FloodFillVisibility(vis, z, y, x, neighbors);
            }
        }

        public void RebuildFullMesh(Vector3 offset, params Chunk[] neighbors)
        {
            VoxelCount = 0;

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var faces = new List<byte>();
            var indices = new List<uint>();
            var indexDict = new Dictionary<int, ushort>();
            var boundingAABB = new List<BoundingBox>();
            var boundingSphere = new List<Vector4>();
            ushort idx_cntr = 0, blk_idx_cnt = 0;
            byte[] min = new byte[3], max = new byte[3];

            for (int i = 0; i < 3; i++)
            {
                min[i] = byte.MaxValue;
                max[i] = byte.MinValue;
            }

            void computeBounds()
            {
                if (blk_idx_cnt != 0)
                {
                    for (int i = 0; i < 3; i++)
                        if (min[i] == max[i])
                        {
                            if (min[i] == byte.MinValue)    //Can't decrement min
                                max[i]++;
                            else if (max[i] == byte.MaxValue)   //Can't increment max
                                min[i]--;
                            else
                                max[i]++;   //Default to incrementing max
                        }

                    Vector3 minv = new Vector3(min[0], min[1], min[2]) + offset;
                    Vector3 maxv = new Vector3(max[0], max[1], max[2]) + offset;
                    Vector3 c = (minv + maxv) * 0.5f;
                    float rad = (maxv - c).Length;
                    boundingAABB.Add(new BoundingBox(minv, maxv));
                    boundingSphere.Add(new Vector4(c, rad));

                    blk_idx_cnt = 0;
                    for (int i = 0; i < 3; i++)
                    {
                        min[i] = byte.MaxValue;
                        max[i] = byte.MinValue;
                    }
                }
            }
            void addFace(byte[] tmp, byte cur)
            {
                for (int i = 0; i < 6; i++)
                {
                    var vec = tmp[i * 3] << 16 | tmp[i * 3 + 1] << 8 | tmp[i * 3 + 2];
                    min[0] = Math.Min(min[0], tmp[i * 3]);
                    min[1] = Math.Min(min[1], tmp[i * 3 + 1]);
                    min[2] = Math.Min(min[1], tmp[i * 3 + 2]);

                    max[0] = Math.Max(max[0], tmp[i * 3]);
                    max[1] = Math.Max(max[1], tmp[i * 3 + 1]);
                    max[2] = Math.Max(max[1], tmp[i * 3 + 2]);

                    blk_idx_cnt++;
                    if (!indexDict.ContainsKey(vec))
                    {
                        indexDict[vec] = idx_cntr;
                        faces.Add(tmp[i * 3 + 0]);
                        faces.Add(tmp[i * 3 + 1]);
                        faces.Add(tmp[i * 3 + 2]);
                        faces.Add(0);

                        indices.Add((uint)cur << 16 | (uint)(idx_cntr & 0xffff));
                        idx_cntr++;
                    }
                    else
                    {
                        indices.Add((uint)cur << 16 | indexDict[vec]);
                    }
                }
                if (blk_idx_cnt == ChunkConstants.BlockSize)
                {
                    computeBounds();
                }
            }

            ComputeVisibility(neighbors);

            //for each height value along each axis, extract faces
            //assign bit value to each face, then emit faces when doing localized iteration
            for (int j = 0; j < vis.Length; j++)
            {
                int xi, yi, zi;
                xi = (j & 0x1) | ((j & 0x8) >> 2) | ((j & 0x40) >> 4) | ((j & 0x200) >> 6) | ((j & 0x1000) >> 8);
                yi = (j & 0x2) >> 1 | ((j & 0x10) >> 3) | ((j & 0x80) >> 5) | ((j & 0x400) >> 7) | ((j & 0x2000) >> 9);
                zi = (j & 0x4) >> 2 | ((j & 0x20) >> 4) | ((j & 0x100) >> 6) | ((j & 0x800) >> 8) | ((j & 0x4000) >> 10);

                byte x = (byte)xi;
                byte y = (byte)yi;
                byte z = (byte)zi;

                byte v = vis[GetIndex(x, y, z)];
                byte cur = data[GetIndex(x, y, z)];
                if (cur == 0) continue;

                if ((v & (1 << 3)) != 0)
                {
                    //0
                    var tmp = new byte[]
                        {
                                    (byte)(x + 1), y, z,
                                    x, y, (byte)(z + 1),
                                    x, y, z,
                                    (byte)(x + 1), y, z,
                                    (byte)(x + 1), y, (byte)(z + 1),
                                    x, y, (byte)(z + 1),
                        };
                    addFace(tmp, cur);
                }

                if ((v & (1 << 4)) != 0)
                {
                    //1
                    var tmp = new byte[]
                    {
                                x, (byte)(y + 1), z,
                                x, (byte)(y + 1), (byte)(z + 1),
                                (byte)(x + 1), (byte)(y + 1), z,
                                x, (byte)(y + 1), (byte)(z + 1),
                                (byte)(x + 1), (byte)(y + 1), (byte)(z + 1),
                                (byte)(x + 1), (byte)(y + 1), z,
                    };
                    addFace(tmp, cur);
                }

                if ((v & (1 << 1)) != 0)
                {
                    //2
                    var tmp = new byte[]
                    {
                                x, y, z,
                                x, y, (byte)(z + 1),
                                x, (byte)(y + 1), z,
                                x, y, (byte)(z + 1),
                                x, (byte)(y + 1), (byte)(z + 1),
                                x, (byte)(y + 1), z,
                    };
                    addFace(tmp, cur);
                }

                if ((v & (1 << 2)) != 0)
                {
                    //3
                    var tmp = new byte[]
                    {
                                (byte)(x + 1), (byte)(y + 1), z,
                                (byte)(x + 1), y, (byte)(z + 1),
                                (byte)(x + 1), y, z,
                                (byte)(x + 1), (byte)(y + 1), z,
                                (byte)(x + 1), (byte)(y + 1), (byte)(z + 1),
                                (byte)(x + 1), y, (byte)(z + 1),
                    };
                    addFace(tmp, cur);
                }

                if ((v & (1 << 5)) != 0)
                {
                    //4
                    var tmp = new byte[]
                    {
                                x, y, z,
                                x, (byte)(y + 1), z,
                                (byte)(x + 1), y, z,
                                x, (byte)(y + 1), z,
                                (byte)(x + 1), (byte)(y + 1), z,
                                (byte)(x + 1), y, z,
                    };
                    addFace(tmp, cur);
                }

                if ((v & (1 << 6)) != 0)
                {
                    //5
                    var tmp = new byte[]
                    {
                                (byte)(x + 1), y, (byte)(z + 1),
                                x, (byte)(y + 1), (byte)(z + 1),
                                x, y, (byte)(z + 1),
                                (byte)(x + 1), y, (byte)(z + 1),
                                (byte)(x + 1), (byte)(y + 1), (byte)(z + 1),
                                x, (byte)(y + 1), (byte)(z + 1),
                    };
                    addFace(tmp, cur);
                }

                VoxelCount++;
            }
            computeBounds();
            //TODO figure out how to obtain index from vertex position without killing reuse
            //Consider making photon tracing act based on nearest vertex rather than nearest voxel

            this.faces = faces.ToArray();
            this.indices = indices.ToArray();
            this.boundSpheres = boundingSphere.ToArray();
            this.boundAABBs = boundingAABB.ToArray();

            stopwatch.Stop();
            Console.WriteLine($"[{id}] Vertex Count: {faces.Count}, Index Count: {indices.Count}, Meshing time: {stopwatch.Elapsed.TotalMilliseconds}ms");


            //for (int i = 0; i < 6; i++) face_quads[i].Clear();
            //Array.Clear(vis, 0, vis.Length);
            /*string str = "";
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