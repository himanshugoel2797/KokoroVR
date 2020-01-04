using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Graphics.Voxel
{
    public static class ChunkConstants
    {
        public const int Side = 32;
        public const int Off = 8;
        public const int DictionaryLen = 1 << (sizeof(ushort) * 8 - Off);
    }
}
