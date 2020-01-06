using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Graphics.Voxel
{
    public static class ChunkConstants
    {
        public const int SideLog = 5;
        public const int Side = (1 << SideLog);
        public const int VoxletSize = 8;
        public const int VoxeletTris = VoxletSize * 36;
        public const int Off = 8;
        public const int DictionaryLen = 1 << (sizeof(ushort) * 8 - Off);
    }
}
