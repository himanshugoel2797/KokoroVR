using Kokoro.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace KokoroVR.Graphics.Voxel
{
    public class ChunkBuffer
    {
        //Allocate various buffer sizes: 16KiB, 32KiB, 64KiB, 128KiB, 256KiB, 512KiB, 1024KiB,
        //for now: 4Kx1024, 16Kx1024, 32Kx16384, 64Kx2048, 196Kx2048, 256Kx128, 1Mx64
        const int Count4K = 1024;
        const int Count16K = 1024;
        const int Count32K = 16384;
        const int Count64K = 2048;
        const int Count196K = 2048;
        const int Count256K = 128;
        const int Count1M = 64;

        private readonly uint[] Sizes = new uint[] { 4 * 1024, 16 * 1024, 32 * 1024, 64 * 1024, 196 * 1024, 256 * 1024, 1024 * 1024 };
        private readonly uint[] Counts = new uint[] { Count4K, Count16K, Count32K, Count64K, Count196K, Count256K, Count1M };
        private BufferAllocator[] BufferAllocators;

        public UniformBuffer VertexBuffers { get; private set; }

        public ChunkBuffer()
        {
            //Setup all buffer allocators
            VertexBuffers = new UniformBuffer(false);
            BufferAllocators = new BufferAllocator[Sizes.Length];
            unsafe
            {
                long* buf = (long*)VertexBuffers.Update();
                for (int i = 0; i < Sizes.Length; i++)
                {
                    BufferAllocators[i] = new BufferAllocator(Sizes[i], Counts[i], false, PixelInternalFormat.Rgba8ui);
                    buf[i * 2] = BufferAllocators[i].BufferTex.View.GetTextureHandle().SetResidency(Residency.Resident);
                }
                VertexBuffers.UpdateDone();
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

        public unsafe byte* Update(int bufferIdx, uint baseVertex)
        {
            return BufferAllocators[bufferIdx].Update((int)(baseVertex * 4 / Sizes[bufferIdx]));
        }

        public void UpdateDone(int bufferIdx, uint baseVertex)
        {
            BufferAllocators[bufferIdx].UpdateDone((int)(baseVertex * 4 / Sizes[bufferIdx]));
        }

        public void Free(int bufferIdx, uint baseVertex)
        {
            BufferAllocators[bufferIdx].Free(new int[] { (int)(baseVertex * 4 / Sizes[bufferIdx]) });
        }
    }
}
