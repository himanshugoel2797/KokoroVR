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
    public enum FaceIndex
    {
        Top = 0,
        Bottom,
        Left,
        Right,
        Front,
        Back,
    }

    public class Chunk
    {
        private object data_locker;

        internal int id;
        internal ChunkStreamer streamer;
        internal bool dirty, update_pending, empty;

        internal byte[] data;
        internal byte[] faces;
        internal uint[] indices;
        internal Vector4[] bounds;
        internal byte[] norm_mask;

        public int VoxelCount { get; private set; }
        public ChunkObject Owner { get; internal set; }

        internal Chunk(ChunkStreamer streamer, int id)
        {
            data_locker = new object();
            data = new byte[ChunkConstants.Side * ChunkConstants.Side * ChunkConstants.Side];
            this.streamer = streamer;
            this.id = id;
            //RebuildFullMesh();  //Initialize the data block
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetIndex(int x, int y, int z)
        {
            return (x & (ChunkConstants.Side - 1)) << (2 * ChunkConstants.SideLog) | (y & (ChunkConstants.Side - 1)) << ChunkConstants.SideLog | (z & (ChunkConstants.Side - 1));
        }

        private void FloodFillVisibility(byte[] vis, int x, int y, int z)
        {
            if (data[GetIndex(x, y, z)] != 0) return;

            vis[GetIndex(x, y, z)] = 2;
            Queue<(int, int, int)> nodes = new Queue<(int, int, int)>();
            nodes.Enqueue((x, y, z));

            int x_b = x;
            int y_b = y;
            int z_b = z;

            while (nodes.Count > 0)
            {
                (x, y, z) = nodes.Dequeue();

                if (x > 0)
                {
                    if (vis[GetIndex(x - 1, y, z)] != 2 && data[GetIndex(x - 1, y, z)] == 0)
                    {
                        vis[GetIndex(x - 1, y, z)] = 2;
                        nodes.Enqueue((x - 1, y, z));
                    }
                }
                if (y > 0)
                {
                    if (vis[GetIndex(x, y - 1, z)] != 2 && data[GetIndex(x, y - 1, z)] == 0)
                    {
                        vis[GetIndex(x, y - 1, z)] = 2;
                        nodes.Enqueue((x, y - 1, z));
                    }
                }
                if (z > 0)
                {
                    if (vis[GetIndex(x, y, z - 1)] != 2 && data[GetIndex(x, y, z - 1)] == 0)
                    {
                        vis[GetIndex(x, y, z - 1)] = 2;
                        nodes.Enqueue((x, y, z - 1));
                    }
                }
                if (x < ChunkConstants.Side - 1)
                {
                    if (vis[GetIndex(x + 1, y, z)] != 2 && data[GetIndex(x + 1, y, z)] == 0)
                    {
                        vis[GetIndex(x + 1, y, z)] = 2;
                        nodes.Enqueue((x + 1, y, z));
                    }
                }
                if (y < ChunkConstants.Side - 1)
                {
                    if (vis[GetIndex(x, y + 1, z)] != 2 && data[GetIndex(x, y + 1, z)] == 0)
                    {
                        vis[GetIndex(x, y + 1, z)] = 2;
                        nodes.Enqueue((x, y + 1, z));
                    }
                }
                if (z < ChunkConstants.Side - 1)
                {
                    if (vis[GetIndex(x, y, z + 1)] != 2 && data[GetIndex(x, y, z + 1)] == 0)
                    {
                        vis[GetIndex(x, y, z + 1)] = 2;
                        nodes.Enqueue((x, y, z + 1));
                    }
                }
            }
        }

        private void ComputeVisibility(byte[] vis)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmitFace(byte cur, int norm, int normal_idx, byte[] tmp, Dictionary<uint, uint> indexDict, List<byte> faces, List<uint> indices, List<Vector4> cluster_bnds, List<byte> cluster_norms, ref byte minx, ref byte miny, ref byte minz, ref byte maxx, ref byte maxy, ref byte maxz, ref byte cur_norm_mask)
        {
            cur_norm_mask |= (byte)(1 << normal_idx);
            for (int i = 0; i < 6; i++)
            {
                minx = Math.Min(minx, tmp[i * 3 + 0]);
                miny = Math.Min(miny, tmp[i * 3 + 1]);
                minz = Math.Min(minz, tmp[i * 3 + 2]);

                maxx = Math.Max(maxx, tmp[i * 3 + 0]);
                maxy = Math.Max(maxy, tmp[i * 3 + 1]);
                maxz = Math.Max(maxz, tmp[i * 3 + 2]);

                var vec = (uint)tmp[i * 3 + 2] << 16 | (uint)tmp[i * 3 + 1] << 8 | tmp[i * 3];
                if (!indexDict.ContainsKey(vec))
                {
                    indices.Add((uint)norm << 24 | (uint)cur << 16 | (uint)(indexDict.Count & 0xffff));
                    indexDict[vec] = (ushort)indexDict.Count;
                    faces.Add(tmp[i * 3 + 0]);
                    faces.Add(tmp[i * 3 + 1]);
                    faces.Add(tmp[i * 3 + 2]);
                    faces.Add(0);
                }
                else
                {
                    indices.Add((uint)norm << 24 | (uint)cur << 16 | indexDict[vec]);
                }
            }

            if (indices.Count % ChunkConstants.BlockSize == 0)
            {
                //Compute bounds and set norm mask
                Vector3 c = new Vector3(maxx + minx, maxy + miny, maxz + minz) * 0.5f;
                float radsq = (new Vector3(maxx, maxy, maxz) - c).Length;

                cluster_bnds.Add(new Vector4(c, radsq));
                cluster_norms.Add(cur_norm_mask);

                cur_norm_mask = 0;
                maxx = maxy = maxz = byte.MinValue;
                minx = miny = minz = byte.MaxValue;
            }
        }

        public void RebuildFullMesh(int x_off, int y_off, int z_off)
        {
            VoxelCount = 0;

            var vismap = new byte[ChunkConstants.Side * ChunkConstants.Side * ChunkConstants.Side];
            ComputeVisibility(vismap);

            var faces = new List<byte>();
            var indices = new List<uint>();
            var cluster_bnds = new List<Vector4>();
            var cluster_norms = new List<byte>();

            //TODO implement neighbor visibility sampling instead of assuming visibility
            var indexDict = new Dictionary<uint, uint>();
            byte cur_norm_mask = 0;
            byte minx = byte.MaxValue, miny = byte.MaxValue, minz = byte.MaxValue, maxx = byte.MinValue, maxy = byte.MinValue, maxz = byte.MinValue;
            for (int j = 0; j < data.Length; j++)
            {
                int xi, yi, zi;
                //Use interleaved indexing to maintain locality for faces, eliminating a need for the clustering pass
                xi = (j & 0x1) | ((j & 0x8) >> 2) | ((j & 0x40) >> 4) | ((j & 0x200) >> 6) | ((j & 0x1000) >> 8);
                yi = (j & 0x2) >> 1 | ((j & 0x10) >> 3) | ((j & 0x80) >> 5) | ((j & 0x400) >> 7) | ((j & 0x2000) >> 9);
                zi = (j & 0x4) >> 2 | ((j & 0x20) >> 4) | ((j & 0x100) >> 6) | ((j & 0x800) >> 8) | ((j & 0x4000) >> 10);

                byte x = (byte)xi;
                byte y = (byte)yi;
                byte z = (byte)zi;

                byte cur, top, btm, frt, bck, lft, rgt, top_v, btm_v, frt_v, bck_v, lft_v, rgt_v;
                lock (data_locker)
                {
                    cur = data[GetIndex(x, y, z)];

                    if (cur == 0)
                        continue;

                    var top_c = Owner.GetChunk(x_off + x, y_off + y - 1, z_off + z);
                    var btm_c = Owner.GetChunk(x_off + x, y_off + y + 1, z_off + z);
                    var frt_c = Owner.GetChunk(x_off + x, y_off + y, z_off + z - 1);
                    var bck_c = Owner.GetChunk(x_off + x, y_off + y, z_off + z + 1);
                    var lft_c = Owner.GetChunk(x_off + x - 1, y_off + y, z_off + z);
                    var rgt_c = Owner.GetChunk(x_off + x + 1, y_off + y, z_off + z);

                    top = y > 0 ? data[GetIndex(x, y - 1, z)] : top_c == null ? (byte)0 : top_c.data[GetIndex(x, ChunkConstants.Side - 1, z)];
                    btm = y < ChunkConstants.Side - 1 ? data[GetIndex(x, y + 1, z)] : btm_c == null ? (byte)0 : btm_c.data[GetIndex(x, 0, z)];
                    frt = z > 0 ? data[GetIndex(x, y, z - 1)] : frt_c == null ? (byte)0 : frt_c.data[GetIndex(x, y, ChunkConstants.Side - 1)];
                    bck = z < ChunkConstants.Side - 1 ? data[GetIndex(x, y, z + 1)] : bck_c == null ? (byte)0 : bck_c.data[GetIndex(x, y, 0)];
                    lft = x > 0 ? data[GetIndex(x - 1, y, z)] : lft_c == null ? (byte)0 : lft_c.data[GetIndex(ChunkConstants.Side - 1, y, z)];
                    rgt = x < ChunkConstants.Side - 1 ? data[GetIndex(x + 1, y, z)] : rgt_c == null ? (byte)0 : rgt_c.data[GetIndex(0, y, z)];

                    top_v = y > 0 ? vismap[GetIndex(x, y - 1, z)] : top_c == null ? (byte)2 : top_c.data[GetIndex(x, ChunkConstants.Side - 1, z)] == 0 ? (byte)2 : (byte)0;
                    btm_v = y < ChunkConstants.Side - 1 ? vismap[GetIndex(x, y + 1, z)] : btm_c == null ? (byte)2 : btm_c.data[GetIndex(x, 0, z)] == 0 ? (byte)2 : (byte)0;
                    frt_v = z > 0 ? vismap[GetIndex(x, y, z - 1)] : frt_c == null ? (byte)2 : frt_c.data[GetIndex(x, y, ChunkConstants.Side - 1)] == 0 ? (byte)2 : (byte)0;
                    bck_v = z < ChunkConstants.Side - 1 ? vismap[GetIndex(x, y, z + 1)] : bck_c == null ? (byte)2 : bck_c.data[GetIndex(x, y, 0)] == 0 ? (byte)2 : (byte)0;
                    lft_v = x > 0 ? vismap[GetIndex(x - 1, y, z)] : lft_c == null ? (byte)2 : lft_c.data[GetIndex(ChunkConstants.Side - 1, y, z)] == 0 ? (byte)2 : (byte)0;
                    rgt_v = x < ChunkConstants.Side - 1 ? vismap[GetIndex(x + 1, y, z)] : rgt_c == null ? (byte)2 : rgt_c.data[GetIndex(0, y, z)] == 0 ? (byte)2 : (byte)0;
                }

                //generate draws per face
                //emit vertices for each faces based on this data
                if (top_v == 2)
                {
                    //Emit a patch for lighting
                    //Group patches based on planes
                    //Compute and store visibility angles which exit the chunk (16x16 angles)
                    //Build a list of locally visible voxels - propogate the lighting on these
                    //raycast exiting angles onto other chunks, store intersection positions - lighting can be injected into chunks based on these locations
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
                    EmitFace(cur, (1 & 3) << 4 | (0 & 3) << 2 | (1 & 3), 0, tmp, indexDict, faces, indices, cluster_bnds, cluster_norms, ref minx, ref miny, ref minz, ref maxx, ref maxy, ref maxz, ref cur_norm_mask);
                }

                if (btm_v == 2)
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
                    EmitFace(cur, (1 & 3) << 4 | (2 & 3) << 2 | (1 & 3), 1, tmp, indexDict, faces, indices, cluster_bnds, cluster_norms, ref minx, ref miny, ref minz, ref maxx, ref maxy, ref maxz, ref cur_norm_mask);
                }

                if (lft_v == 2)
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
                    EmitFace(cur, (1 & 3) << 4 | (1 & 3) << 2 | (0 & 3), 2, tmp, indexDict, faces, indices, cluster_bnds, cluster_norms, ref minx, ref miny, ref minz, ref maxx, ref maxy, ref maxz, ref cur_norm_mask);
                }

                if (rgt_v == 2)
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
                    EmitFace(cur, (1 & 3) << 4 | (1 & 3) << 2 | (2 & 3), 3, tmp, indexDict, faces, indices, cluster_bnds, cluster_norms, ref minx, ref miny, ref minz, ref maxx, ref maxy, ref maxz, ref cur_norm_mask);
                }

                if (frt_v == 2)
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
                    EmitFace(cur, (0 & 3) << 4 | (1 & 3) << 2 | (1 & 3), 4, tmp, indexDict, faces, indices, cluster_bnds, cluster_norms, ref minx, ref miny, ref minz, ref maxx, ref maxy, ref maxz, ref cur_norm_mask);
                }

                if (bck_v == 2)
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
                    EmitFace(cur, (2 & 3) << 4 | (1 & 3) << 2 | (1 & 3), 5, tmp, indexDict, faces, indices, cluster_bnds, cluster_norms, ref minx, ref miny, ref minz, ref maxx, ref maxy, ref maxz, ref cur_norm_mask);
                }
                VoxelCount++;
            }

            if (indices.Count % ChunkConstants.BlockSize != 0)
            {
                //Compute bounds and set norm mask
                Vector3 c = new Vector3(maxx + minx, maxy + miny, maxz + minz) * 0.5f;
                float radsq = (new Vector3(maxx, maxy, maxz) - c).Length;

                cluster_bnds.Add(new Vector4(c, radsq));
                cluster_norms.Add(cur_norm_mask);
            }

            this.faces = faces.ToArray();
            this.indices = indices.ToArray();
            this.bounds = cluster_bnds.ToArray();
            this.norm_mask = cluster_norms.ToArray();

            if (this.faces.Length == 0)
                empty = true;

            dirty = false;
            update_pending = true;
        }

        public void EditLocalMesh(int x, int y, int z, byte cur)
        {
            //Just edit the current value
            lock (data_locker)
            {
                var prev_data = data[GetIndex(x, y, z)];
                if (prev_data != cur) dirty = true;
                if (cur == 0 && prev_data != 0)
                    VoxelCount--;
                else if (cur != 0 && prev_data == 0)
                    VoxelCount++;

                data[GetIndex(x, y, z)] = cur;
            }
        }

        public void Update()
        {
            // if (dirty)
            //RebuildFullMesh();
        }
    }
}