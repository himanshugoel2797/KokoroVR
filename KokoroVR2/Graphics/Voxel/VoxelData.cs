using System;
using Kokoro.Math;

namespace KokoroVR2.Graphics.Voxel
{
    public class VoxelData
    {
        public uint[] VisibilityMasks; //Use bit manipulation instructions to speed up meshing
        public Func<VoxelData, int, int, int, byte> GetMaterialData; //Use block compression for materials
        public Vector3 Offset;

        public VoxelData()
        {
            VisibilityMasks = new uint[VoxelConstants.ChunkSideWithNeighbors * VoxelConstants.ChunkSideWithNeighbors];
        }

        public static int GetVisibilityIndex(int x, int y, int z)
        {
            return y * VoxelConstants.ChunkSideWithNeighbors + x;
        }

        public static int GetVisibilityBit(int x, int y, int z)
        {
            return z;
        }

        public static int GetMaterialIndex(int x, int y, int z)
        {
            return z * VoxelConstants.ChunkSideWithNeighbors * VoxelConstants.ChunkSideWithNeighbors + y * VoxelConstants.ChunkSideWithNeighbors + x;
        }
    }
}
