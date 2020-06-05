using Kokoro.Common;
using Kokoro.Graphics;
using Kokoro.Math;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace KokoroVR2.Graphics.Voxel
{
    public class VoxelCache : UniquelyNamedObject
    {
        //Multiple threads of 'chunk processors'
        //Build list of visible chunks
        //Check list of chunks to load
        //  - Load chunk in from disk if needed - caller is responsible for allocating memory for the chunk
        //Check list of chunks to build
        //  - Build the chunks
        //Check list of chunks to unload
        //  - Save and unload the chunks

        // - Compute shader sorts splats into bands based on eye distance
        // - Draw bands front to back


        internal BufferAllocator IndexBuffer;
        internal ConcurrentQueue<VoxelChunk> VisibleChunks;
        internal ConcurrentQueue<VoxelChunk> LoadChunks;
        internal ConcurrentQueue<VoxelChunk> BuildChunks;
        internal ConcurrentQueue<VoxelChunk> UnloadChunks;

        public VoxelCache(string cacheName) : base(cacheName)
        {
            IndexBuffer = new BufferAllocator(cacheName, VoxelConstants.IndexBlockSz, VoxelConstants.IndexBlockCnt, BufferUsage.Index, ImageFormat.R8G8B8A8UInt);


        }
    }
}
