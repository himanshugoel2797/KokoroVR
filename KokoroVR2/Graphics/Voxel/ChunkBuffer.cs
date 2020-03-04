using Kokoro.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KokoroVR2.Graphics.Voxel
{
    public class ChunkBuffer
    {
        //Allocate various buffer sizes: 16KiB, 32KiB, 64KiB, 128KiB, 256KiB, 512KiB, 1024KiB,
        //for now: 4Kx1024, 16Kx1024, 32Kx16384, 64Kx2048, 196Kx2048, 256Kx128, 1Mx64
        const int Count4K = 2048;
        const int Count16K = 16384;
        const int Count32K = 8192;
        const int Count64K = 2048;
        const int Count196K = 2048;
        const int Count256K = 128;
        const int Count1M = 64;

        private readonly uint[] Sizes = new uint[] { 4 * 1024, 16 * 1024, 32 * 1024, 64 * 1024, 196 * 1024, 256 * 1024, 1024 * 1024 };
        private readonly uint[] Counts = new uint[] { Count4K, Count16K, Count32K, Count64K, Count196K, Count256K, Count1M };
        private BufferAllocator[] BufferAllocators;

        public GpuBufferView[] Views { get => BufferAllocators.Select(a => a.BufferTex).ToArray(); }
        public string[] Names { get => BufferAllocators.Select(a => a.Name).ToArray(); }

        public ChunkBuffer()
        {
            //Setup all buffer allocators
            BufferAllocators = new BufferAllocator[Sizes.Length];
            for (int i = 0; i < Sizes.Length; i++)
            {
                BufferAllocators[i] = new BufferAllocator("ChunkBuffer" + i, Engine.Graph, Sizes[i], Counts[i], BufferUsage.StorageTexel, ImageFormat.R8G8B8A8UInt);
            }
        }

        public void Allocate(byte[] verts, out int bufferIdx, out uint baseVertex)
        {
            var vCount = verts.Length;
            for (int i = 0; i < Sizes.Length; i++)
                if (Sizes[i] > vCount)
                {
                    bufferIdx = i;
                    var indices = BufferAllocators[i].Allocate(vCount);
                    if (indices.Length > 1) throw new ArgumentOutOfRangeException("Expected only one allocated block!");
                    baseVertex = (uint)indices[0] * Sizes[i] / 4;
                    return;
                }
            throw new Exception("Allocation failed! Need a larger block size.");
        }

        public void GenerateRenderGraph()
        {
            for (int i = 0; i < BufferAllocators.Length; i++)
                BufferAllocators[i].GenerateRenderGraph();
        }

        public void Update()
        {
            for (int i = 0; i < BufferAllocators.Length; i++)
                BufferAllocators[i].Update();
        }

        public unsafe byte* BeginBufferUpdate(int bufferIdx, uint baseVertex)
        {
            return BufferAllocators[bufferIdx].BeginBufferUpdate((int)(baseVertex * 4 / Sizes[bufferIdx]));
        }

        public void EndBufferUpdate(int bufferIdx, uint baseVertex)
        {
            BufferAllocators[bufferIdx].EndBufferUpdate((int)(baseVertex * 4 / Sizes[bufferIdx]));
        }

        public void Free(int bufferIdx, uint baseVertex)
        {
            BufferAllocators[bufferIdx].Free(new int[] { (int)(baseVertex * 4 / Sizes[bufferIdx]) });
        }
    }
}
