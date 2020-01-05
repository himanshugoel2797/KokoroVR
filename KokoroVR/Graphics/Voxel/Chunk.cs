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
        internal List<byte>[] faces;

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
            return x * (ChunkConstants.Side * ChunkConstants.Side) + y * ChunkConstants.Side + z;
        }

        public void RebuildFullMesh()
        {
            VoxelCount = 0;

            faces = new List<byte>[6];
            for (int i = 0; i < faces.Length; i++) faces[i] = new List<byte>();

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
                            faces[0].AddRange(new byte[]
                                {
                                    x, y, z, cur,
                                    x, y, (byte)(z + 1), cur,
                                    (byte)(x + 1), y, (byte)(z + 1), cur,
                                    (byte)(x + 1), y, z, cur,
                                });

                        if (btm == 0)
                            faces[1].AddRange(new byte[]
                            {
                                x, (byte)(y + 1), z, cur,
                                x, (byte)(y + 1), (byte)(z + 1), cur,
                                (byte)(x + 1), (byte)(y + 1), (byte)(z + 1), cur,
                                (byte)(x + 1), (byte)(y + 1), z, cur,
                            });

                        if (lft == 0)
                            faces[2].AddRange(new byte[]
                            {
                                x, y, z, cur,
                                x, y, (byte)(z + 1), cur,
                                x, (byte)(y + 1), (byte)(z + 1), cur,
                                x, (byte)(y + 1), z, cur,
                            });

                        if (rgt == 0)
                            faces[3].AddRange(new byte[]
                            {
                                (byte)(x + 1), y, z, cur,
                                (byte)(x + 1), y, (byte)(z + 1), cur,
                                (byte)(x + 1), (byte)(y + 1), (byte)(z + 1), cur,
                                (byte)(x + 1), (byte)(y + 1), z, cur,
                            });

                        if (frt == 0)
                            faces[4].AddRange(new byte[]
                            {
                                x, y, z, cur,
                                x, (byte)(y + 1), z, cur,
                                (byte)(x + 1), (byte)(y + 1), z, cur,
                                (byte)(x + 1), y, z, cur,
                            });

                        if (bck == 0)
                            faces[5].AddRange(new byte[]
                            {
                                x, y, (byte)(z + 1), cur,
                                x, (byte)(y + 1), (byte)(z + 1), cur,
                                (byte)(x + 1), (byte)(y + 1), (byte)(z + 1), cur,
                                (byte)(x + 1), y, (byte)(z + 1), cur,
                            });

                        VoxelCount++;
                    }

            //Normals:
            //0 - 0, -1, 0
            //1 - 0, 1, 0
            //2 - -1, 0, 0
            //3 - 1, 0, 0
            //4 - 0, 0, -1
            //5 - 0, 0, 1
            var norm_set = new Vector3[]
            {
                Vector3.UnitY * -1,
                Vector3.UnitY * 1,
                Vector3.UnitX * -1,
                Vector3.UnitX * 1,
                Vector3.UnitZ * -1,
                Vector3.UnitZ * 1,
            };

            //generate a graph from each side, including material ids
            //drop vertices that have 4 connections
            //join the vertices that have 3 connections
            //generate triangles from these polygons
            var opts = new MeshOptimizer[6];
            for (int i = 0; i < opts.Length; i++)
            {
                if (faces[i].Count > 0)
                {
                    opts[i] = new MeshOptimizer(norm_set[i]);
                    opts[i].ReduceQuads(faces[i].ToArray());
                }
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
