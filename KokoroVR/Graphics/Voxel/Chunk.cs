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
            RebuildFullMesh();  //Initialize the data block
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetIndex(int x, int y, int z)
        {
            return (x & (ChunkConstants.Side - 1)) << (2 * ChunkConstants.SideLog) | (y & (ChunkConstants.Side - 1)) << ChunkConstants.SideLog | (z & (ChunkConstants.Side - 1));
        }

        public void RebuildFullMesh()
        {
            VoxelCount = 0;

            faces = new List<byte>();
            //0 - 0, -1, 0
            //1 - 0, 1, 0
            //2 - -1, 0, 0
            //3 - 1, 0, 0
            //4 - 0, 0, -1
            //5 - 0, 0, 1
            for (byte x = 0; x <= ChunkConstants.Side - 1; x++)
                for (byte y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (byte z = 0; z <= ChunkConstants.Side - 1; z++)
                    {
                        byte cur, top, btm, frt, bck, lft, rgt;
                        lock (data_locker)
                        {
                            cur = data[GetIndex(x, y, z)];
                            top = y > 0 ? data[GetIndex(x, y - 1, z)] : (byte)0;
                            btm = y < ChunkConstants.Side - 1 ? data[GetIndex(x, y + 1, z)] : (byte)0;
                            frt = z > 0 ? data[GetIndex(x, y, z - 1)] : (byte)0;
                            bck = z < ChunkConstants.Side - 1 ? data[GetIndex(x, y, z + 1)] : (byte)0;
                            lft = x > 0 ? data[GetIndex(x - 1, y, z)] : (byte)0;
                            rgt = x < ChunkConstants.Side - 1 ? data[GetIndex(x + 1, y, z)] : (byte)0;
                        }
                        //generate draws per face
                        if (cur == 0)
                            continue;

                        //emit vertices for each faces based on this data
                        if (top == 0)
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

                        if (btm == 0)
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

                        if (lft == 0)
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

                        if (rgt == 0)
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

                        if (frt == 0)
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

                        if (bck == 0)
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


            if (faces.Count > 0)
            {
                //reorder triangles to be clustered together
                var face_arr = faces.ToArray();
                var face_sorted = new List<byte>();
                var prims = new List<Primitive>();
                for (int i = 0; i < faces.Count; i += 4 * 3)
                    prims.Add(new Primitive(face_arr, i));

                do
                {
                    //Choose the first primitive
                    var ref_prim = prims[0];
                    prims.RemoveAt(0);
                    prims = prims.OrderBy(a => (ref_prim.center - a.center).LengthSquared).ToList();
                    var ordered = prims.ToArray();

                    face_sorted.AddRange(ref_prim.data);
                    for(int i = 0; i < Math.Min(ChunkConstants.VoxeletTris - 1, ordered.Length); i++)
                    {
                        face_sorted.AddRange(ordered[i].data);
                        prims.Remove(ordered[i]);
                    }
                } while (prims.Count > 0);
                faces = face_sorted;
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