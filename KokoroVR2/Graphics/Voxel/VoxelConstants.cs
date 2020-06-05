using System;

namespace KokoroVR2.Graphics.Voxel
{
    public class VoxelConstants
    {
        public const int ChunkSide = 30;
        public const int ChunkSideWithNeighbors = 32;
        public const int ZLen = 64;

        public const int CPUCacheLen = 1 << 15;
        public const int CPUChunkSize = (ChunkSideWithNeighbors * ChunkSideWithNeighbors * sizeof(ulong)) + (ZLen * ChunkSide * ChunkSide);
        public const int IndexBufferLen = (1024 * 1024 * 1024);
        public const int IndexBlockSz = (16 * 16 * 32 * 4);
        public const int IndexBlockCnt = 32768;
    }
}
