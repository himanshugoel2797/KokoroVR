using System;
using System.Runtime.InteropServices;
using Kokoro.Math;

namespace KokoroVR2.Graphics.Voxel
{
    public unsafe class VoxelData : IDisposable
    {
        private IntPtr memArea;
        public ulong* VisibilityMasks; //Use bit manipulation instructions to speed up meshing
        public byte* MaterialData;
        public uint* IndexCache;
        //public Func<VoxelData, int, int, int, byte> GetMaterialData; //Use block compression for materials
        public Vector3 Offset;

        public VoxelData(Vector3 offset)
        {
            var len = (VoxelConstants.ChunkSideWithNeighbors * VoxelConstants.ChunkSideWithNeighbors * sizeof(ulong))
                + (6 * VoxelConstants.ChunkSideWithNeighbors * VoxelConstants.ChunkSide * VoxelConstants.ChunkSide * sizeof(uint)) +
                (VoxelConstants.ChunkSideWithNeighbors * 2 * VoxelConstants.ChunkSideWithNeighbors * VoxelConstants.ChunkSideWithNeighbors);
            memArea = Marshal.AllocHGlobal(len);

            VisibilityMasks = (ulong*)memArea;
            IndexCache = (uint*)memArea + (VoxelConstants.ChunkSideWithNeighbors * VoxelConstants.ChunkSideWithNeighbors * 2);
            MaterialData = (byte*)memArea + (VoxelConstants.ChunkSideWithNeighbors * VoxelConstants.ChunkSideWithNeighbors * sizeof(ulong))
            + (6 * VoxelConstants.ChunkSideWithNeighbors * VoxelConstants.ChunkSide * VoxelConstants.ChunkSide * sizeof(uint));

            //VisibilityMasks = new uint[VoxelConstants.ChunkSideWithNeighbors * VoxelConstants.ChunkSideWithNeighbors];
            //IndexCache = new uint[6 * VoxelConstants.ChunkSide / 2 * VoxelConstants.ChunkSide * VoxelConstants.ChunkSide];
            //MaterialData = new byte[VoxelConstants.ChunkSideWithNeighbors * VoxelConstants.ChunkSideWithNeighbors * VoxelConstants.ChunkSideWithNeighbors];
            Offset = offset;
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

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                Marshal.FreeHGlobal(memArea);

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~VoxelData()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
