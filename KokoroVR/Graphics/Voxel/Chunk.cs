using Kokoro.Graphics;
using Kokoro.Math;
using System;
using System.Collections.Generic;
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
        private byte[] data;
        private object data_locker;

        internal int id;
        internal ChunkStreamer streamer;
        internal bool dirty, update_pending;
        internal List<byte> faces;

        struct Primitive
        {
            public byte[] data;
            public Vector3[] pos;
            public Vector3 center;

            public Primitive(byte[] d, int off)
            {
                data = new byte[4 * 3];
                Buffer.BlockCopy(d, off, data, 0, 4 * 3);

                pos = new Vector3[3];
                pos[0] = new Vector3(data[0 * 4 + 0] & 0x1f, data[0 * 4 + 1] & 0x1f, data[0 * 4 + 2] & 0x1f);
                pos[1] = new Vector3(data[1 * 4 + 0] & 0x1f, data[1 * 4 + 1] & 0x1f, data[1 * 4 + 2] & 0x1f);
                pos[2] = new Vector3(data[2 * 4 + 0] & 0x1f, data[2 * 4 + 1] & 0x1f, data[2 * 4 + 2] & 0x1f);

                center = (pos[0] + pos[1] + pos[2]) * 1.0f / 3.0f;
            }
        }

        public int VoxelCount { get; private set; }

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


            /*for (int x = 0; x <= ChunkConstants.Side - 1; x++)
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        if ((x == 0) | (y == 0) | (z == 0) | (x == ChunkConstants.Side - 1) | (y == ChunkConstants.Side - 1) | (z == ChunkConstants.Side - 1))
                            FloodFillVisibility(vis, x, y, z);*/
        }

        public void RebuildFullMesh()
        {
            VoxelCount = 0;

            var vismap = new byte[ChunkConstants.Side * ChunkConstants.Side * ChunkConstants.Side];
            ComputeVisibility(vismap);

            faces = new List<byte>();
            //0 - 0, -1, 0
            //1 - 0, 1, 0
            //2 - -1, 0, 0
            //3 - 1, 0, 0
            //4 - 0, 0, -1
            //5 - 0, 0, 1
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
                    top = y > 0 ? data[GetIndex(x, y - 1, z)] : (byte)0;
                    btm = y < ChunkConstants.Side - 1 ? data[GetIndex(x, y + 1, z)] : (byte)0;
                    frt = z > 0 ? data[GetIndex(x, y, z - 1)] : (byte)0;
                    bck = z < ChunkConstants.Side - 1 ? data[GetIndex(x, y, z + 1)] : (byte)0;
                    lft = x > 0 ? data[GetIndex(x - 1, y, z)] : (byte)0;
                    rgt = x < ChunkConstants.Side - 1 ? data[GetIndex(x + 1, y, z)] : (byte)0;

                    top_v = y > 0 ? vismap[GetIndex(x, y - 1, z)] : (byte)2;
                    btm_v = y < ChunkConstants.Side - 1 ? vismap[GetIndex(x, y + 1, z)] : (byte)2;
                    frt_v = z > 0 ? vismap[GetIndex(x, y, z - 1)] : (byte)2;
                    bck_v = z < ChunkConstants.Side - 1 ? vismap[GetIndex(x, y, z + 1)] : (byte)2;
                    lft_v = x > 0 ? vismap[GetIndex(x - 1, y, z)] : (byte)2;
                    rgt_v = x < ChunkConstants.Side - 1 ? vismap[GetIndex(x + 1, y, z)] : (byte)2;
                }
                //generate draws per face
                if (cur == 0)
                    continue;

                //emit vertices for each faces based on this data
                if (top_v == 2)
                {
                    //0
                    var tmp = new byte[]
                        {
                                    (byte)(x + 1), y, z, cur,
                                    x, y, (byte)(z + 1), cur,
                                    x, y, z, cur,
                                    (byte)(x + 1), y, z, cur,
                                    (byte)(x + 1), y, (byte)(z + 1), cur,
                                    x, y, (byte)(z + 1), cur,
                        };
                    int idx = 1;
                    int idy = 0;
                    int idz = 1;
                    for (int i = 0; i < 6; i++)
                    {
                        tmp[i * 4 + 0] |= (byte)((idx & 3) << 6);
                        tmp[i * 4 + 1] |= (byte)((idy & 3) << 6);
                        tmp[i * 4 + 2] |= (byte)((idz & 3) << 6);
                    }
                    faces.AddRange(tmp);
                }

                if (btm_v == 2)
                {
                    //1
                    var tmp = new byte[]
                    {
                                x, (byte)(y + 1), z, cur,
                                x, (byte)(y + 1), (byte)(z + 1), cur,
                                (byte)(x + 1), (byte)(y + 1), z, cur,
                                x, (byte)(y + 1), (byte)(z + 1), cur,
                                (byte)(x + 1), (byte)(y + 1), (byte)(z + 1), cur,
                                (byte)(x + 1), (byte)(y + 1), z, cur,
                    };
                    int idx = 1;
                    int idy = 2;
                    int idz = 1;
                    for (int i = 0; i < 6; i++)
                    {
                        tmp[i * 4 + 0] |= (byte)((idx & 3) << 6);
                        tmp[i * 4 + 1] |= (byte)((idy & 3) << 6);
                        tmp[i * 4 + 2] |= (byte)((idz & 3) << 6);
                    }
                    faces.AddRange(tmp);
                }

                if (lft_v == 2)
                {
                    //2
                    var tmp = new byte[]
                    {
                                x, y, z, cur,
                                x, y, (byte)(z + 1), cur,
                                x, (byte)(y + 1), z, cur,
                                x, y, (byte)(z + 1), cur,
                                x, (byte)(y + 1), (byte)(z + 1), cur,
                                x, (byte)(y + 1), z, cur,
                    };
                    int idx = 0;
                    int idy = 1;
                    int idz = 1;
                    for (int i = 0; i < 6; i++)
                    {
                        tmp[i * 4 + 0] |= (byte)((idx & 3) << 6);
                        tmp[i * 4 + 1] |= (byte)((idy & 3) << 6);
                        tmp[i * 4 + 2] |= (byte)((idz & 3) << 6);
                    }
                    faces.AddRange(tmp);
                }

                if (rgt_v == 2)
                {
                    //3
                    var tmp = new byte[]
                    {
                                (byte)(x + 1), (byte)(y + 1), z, cur,
                                (byte)(x + 1), y, (byte)(z + 1), cur,
                                (byte)(x + 1), y, z, cur,
                                (byte)(x + 1), (byte)(y + 1), z, cur,
                                (byte)(x + 1), (byte)(y + 1), (byte)(z + 1), cur,
                                (byte)(x + 1), y, (byte)(z + 1), cur,
                    };
                    int idx = 2;
                    int idy = 1;
                    int idz = 1;
                    for (int i = 0; i < 6; i++)
                    {
                        tmp[i * 4 + 0] |= (byte)((idx & 3) << 6);
                        tmp[i * 4 + 1] |= (byte)((idy & 3) << 6);
                        tmp[i * 4 + 2] |= (byte)((idz & 3) << 6);
                    }
                    faces.AddRange(tmp);
                }

                if (frt_v == 2)
                {
                    //4
                    var tmp = new byte[]
                    {
                                x, y, z, cur,
                                x, (byte)(y + 1), z, cur,
                                (byte)(x + 1), y, z, cur,
                                x, (byte)(y + 1), z, cur,
                                (byte)(x + 1), (byte)(y + 1), z, cur,
                                (byte)(x + 1), y, z, cur,
                    };
                    int idx = 1;
                    int idy = 1;
                    int idz = 0;
                    for (int i = 0; i < 6; i++)
                    {
                        tmp[i * 4 + 0] |= (byte)((idx & 3) << 6);
                        tmp[i * 4 + 1] |= (byte)((idy & 3) << 6);
                        tmp[i * 4 + 2] |= (byte)((idz & 3) << 6);
                    }
                    faces.AddRange(tmp);
                }

                if (bck_v == 2)
                {
                    //5
                    var tmp = new byte[]
                    {
                                (byte)(x + 1), y, (byte)(z + 1), cur,
                                x, (byte)(y + 1), (byte)(z + 1), cur,
                                x, y, (byte)(z + 1), cur,
                                (byte)(x + 1), y, (byte)(z + 1), cur,
                                (byte)(x + 1), (byte)(y + 1), (byte)(z + 1), cur,
                                x, (byte)(y + 1), (byte)(z + 1), cur,
                    };
                    int idx = 1;
                    int idy = 1;
                    int idz = 2;
                    for (int i = 0; i < 6; i++)
                    {
                        tmp[i * 4 + 0] |= (byte)((idx & 3) << 6);
                        tmp[i * 4 + 1] |= (byte)((idy & 3) << 6);
                        tmp[i * 4 + 2] |= (byte)((idz & 3) << 6);
                    }
                    faces.AddRange(tmp);
                }

                VoxelCount++;
            }

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
            if (dirty)
                RebuildFullMesh();
        }
    }
}