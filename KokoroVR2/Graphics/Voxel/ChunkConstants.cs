using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Text;

namespace KokoroVR2.Graphics.Voxel
{
    public static class ChunkConstants
    {
        public const int SideLog = 5;
        public const int Side = (1 << SideLog);
        public const int DictionaryLen = (1 << 16) - 1;
        public const int WavefrontSize = 64;
        public const int BlockSize = 1024 * 12;

        public static Vector3[] Normals = new Vector3[]
        {
            new Vector3(0, -1, 0),
            new Vector3(0, 1, 0),
            new Vector3(-1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 0, -1),
            new Vector3(0, 0, 1),
        };
    }
}
