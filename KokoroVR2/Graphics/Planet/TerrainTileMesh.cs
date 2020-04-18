using System;
using System.Collections.Generic;

namespace KokoroVR2.Graphics.Planet
{
    public enum TerrainTileEdge
    {
        None = 0,
        Left = 1,
        Right = 2,
        Front = 4,
        Back = 8,
        All = Left | Right | Front | Back,
    }
    public class TerrainTileMesh
    {
        const int Side = 129;

        public static void Create(byte stepLen, TerrainTileEdge sideLoDMask, out byte[] verts, out ushort[] indices)
        {
            var vertL = new List<byte>();
            var indL = new List<ushort>();
            ushort indCntr = 0;
            var vertDict = new Dictionary<ushort, ushort>();

            //sideLoDMask is a bitfield which specifies for which sides to generate LoD transitions
            int lStep = stepLen * ((sideLoDMask & TerrainTileEdge.Left) != 0 ? 2 : 1);
            int rStep = stepLen * ((sideLoDMask & TerrainTileEdge.Right) != 0 ? 2 : 1);
            int fStep = stepLen * ((sideLoDMask & TerrainTileEdge.Front) != 0 ? 2 : 1);
            int bStep = stepLen * ((sideLoDMask & TerrainTileEdge.Back) != 0 ? 2 : 1);

            var vertSwitches = new bool[Side * Side];

            for (int y = 0; y < Side; y += lStep)
                vertSwitches[y * Side + 0] = true;

            for (int x = 0; x < Side; x += fStep)
                vertSwitches[0 * Side + x] = true;

            for (int x = 0; x < Side; x += bStep)
                vertSwitches[(Side - 1) * Side + x] = true;

            for (int x = stepLen; x < Side - stepLen; x += stepLen)
                for (int y = stepLen; y < Side - stepLen; y += stepLen)
                    vertSwitches[y * Side + x] = true;

            for (int y = 0; y < Side; y += rStep)
                vertSwitches[y * Side + (Side - 1)] = true;

            void emitF(int x0, int y0, int x1, int y1, int x2, int y2)
            {
                if (x0 > Side | y0 > Side | x1 > Side | y1 > Side | x2 > Side | y2 > Side)
                    return;

                ushort v0 = (ushort)(x0 << 8 | y0);
                ushort v1 = (ushort)(x1 << 8 | y1);
                ushort v2 = (ushort)(x2 << 8 | y2);
                if (!vertDict.ContainsKey(v0))
                {
                    indL.Add(indCntr);
                    vertDict[v0] = indCntr++;
                    vertL.Add((byte)x0);
                    vertL.Add((byte)y0);
                }
                else
                    indL.Add(vertDict[v0]);
                if (!vertDict.ContainsKey(v1))
                {
                    indL.Add(indCntr);
                    vertDict[v1] = indCntr++;
                    vertL.Add((byte)x1);
                    vertL.Add((byte)y1);
                }
                else
                    indL.Add(vertDict[v1]);
                if (!vertDict.ContainsKey(v2))
                {
                    indL.Add(indCntr);
                    vertDict[v2] = indCntr++;
                    vertL.Add((byte)x2);
                    vertL.Add((byte)y2);
                }
                else
                    indL.Add(vertDict[v2]);
            }

            for (int x = 0; x < Side; x += 2 * stepLen)
                for (int y = 0; y < Side; y += 2 * stepLen)
                {
                    //build predicted quad
                    int x0 = x;
                    int y0 = y;

                    int x1 = x + stepLen;
                    int y1 = y;

                    int x2 = x + 2 * stepLen;
                    int y2 = y;

                    int x3 = x;
                    int y3 = y + stepLen;

                    int x4 = x + stepLen;
                    int y4 = y + stepLen;

                    int x5 = x + 2 * stepLen;
                    int y5 = y + stepLen;

                    int x6 = x;
                    int y6 = y + 2 * stepLen;

                    int x7 = x + stepLen;
                    int y7 = y + 2 * stepLen;

                    int x8 = x + 2 * stepLen;
                    int y8 = y + 2 * stepLen;

                    bool v1 = y1 < Side && x1 < Side && vertSwitches[y1 * Side + x1];
                    bool v3 = y3 < Side && x3 < Side && vertSwitches[y3 * Side + x3];
                    bool v5 = y5 < Side && x5 < Side && vertSwitches[y5 * Side + x5];
                    bool v7 = y7 < Side && x7 < Side && vertSwitches[y7 * Side + x7];

                    if (!v3)
                        emitF(x0, y0, x4, y4, x6, y6);
                    else
                    {
                        emitF(x0, y0, x4, y4, x3, y3);
                        emitF(x3, y3, x4, y4, x6, y6);
                    }

                    if (!v7)
                        emitF(x4, y4, x8, y8, x6, y6);
                    else
                    {
                        emitF(x4, y4, x8, y8, x7, y7);
                        emitF(x7, y7, x6, y6, x4, y4);
                    }

                    if (!v5)
                        emitF(x2, y2, x8, y8, x4, y4);
                    else
                    {
                        emitF(x2, y2, x5, y5, x4, y4);
                        emitF(x4, y4, x5, y5, x8, y8);
                    }

                    if (!v1)
                        emitF(x0, y0, x2, y2, x4, y4);
                    else
                    {
                        emitF(x0, y0, x1, y1, x4, y4);
                        emitF(x1, y1, x2, y2, x4, y4);
                    }
                }

            verts = vertL.ToArray();
            indices = indL.ToArray();
        }
    }
}
