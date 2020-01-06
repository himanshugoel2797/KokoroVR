using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Graphics.Voxel
{
    public class RunEncoder
    {
        public class Run
        {
            public byte Type;
            public byte Count;
            public List<byte>[] VisibleFaces;
        }

        public RunEncoder() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetIndex(int x, int y, int z)
        {
            return (x & (ChunkConstants.Side - 1)) << (2 * ChunkConstants.SideLog) | (y & (ChunkConstants.Side - 1)) << ChunkConstants.SideLog | (z & (ChunkConstants.Side - 1));
        }

        public Run[] Encode(byte[] data)
        {
            var rle = new List<Run>();
            var vis = new byte[ChunkConstants.Side * ChunkConstants.Side * ChunkConstants.Side];

            for (byte x = 0; x <= ChunkConstants.Side - 1; x++)
                for (byte y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (byte z = 0; z <= ChunkConstants.Side - 1; z++)
                    {
                        byte cur, top, btm, frt, bck, lft, rgt;
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

                        var bmp = 0;
                        bmp |= top == 0 ? (1 << 0) : 0;
                        bmp |= btm == 0 ? (1 << 1) : 0;
                        bmp |= frt == 0 ? (1 << 2) : 0;
                        bmp |= bck == 0 ? (1 << 3) : 0;
                        bmp |= lft == 0 ? (1 << 4) : 0;
                        bmp |= rgt == 0 ? (1 << 5) : 0;

                        vis[GetIndex(x, y, z)] = (byte)bmp;
                    }

            for (int x = 0; x <= ChunkConstants.Side - 1; x++)
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                {
                    var crun = new Run()
                    {
                        Count = 1,
                        Type = data[GetIndex(x, y, 0)],
                        VisibleFaces = new List<byte>[6],
                    };
                    for (int i = 0; i < 6; i++)
                    {
                        crun.VisibleFaces[i] = new List<byte>();
                        if ((vis[GetIndex(x, y, 0)] & (1 << i)) != 0)
                            crun.VisibleFaces[i].Add(0);
                    }
                    for (int z = 1; z <= ChunkConstants.Side - 1; z++)
                        if (crun.Type == data[GetIndex(x, y, z)])
                        {
                            crun.Count++;
                            for (int i = 0; i < 6; i++)
                                if ((vis[GetIndex(x, y, z)] & (1 << i)) != 0)
                                    crun.VisibleFaces[i].Add(0);
                        }
                        else
                        {
                            rle.Add(crun);
                            crun = new Run()
                            {
                                Count = 1,
                                Type = data[GetIndex(x, y, z)],
                                VisibleFaces = new List<byte>[6],
                            };
                            for (int i = 0; i < 6; i++)
                            {
                                crun.VisibleFaces[i] = new List<byte>();
                                if ((vis[GetIndex(x, y, z)] & (1 << i)) != 0)
                                    crun.VisibleFaces[i].Add((byte)z);
                            }
                        }
                    rle.Add(crun);
                }
            return rle.ToArray();
        }

        public void Decode(Run[] rle, byte[] data)
        {
            int pos = 0;
            for (int i = 0; i < rle.Length; i++)
                for (int j = 0; j < rle[i].Count; j++)
                    data[pos++] = rle[i].Type;
        }
    }
}
