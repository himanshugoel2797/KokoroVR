using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public class BufferAllocator
    {
        private BufferTexture buf;
        private BlockAllocator alloc;

        public uint BlockSize { get => alloc.BlockSize; }
        public BufferTexture BufferTex { get => buf; }

        public BufferAllocator(uint block_sz, uint block_cnt, bool stream, PixelInternalFormat iFmt)
        {
            alloc = new BlockAllocator(block_cnt, block_sz);
            buf = new BufferTexture(block_sz * block_cnt, iFmt, stream);
        }

        public int[] Allocate(long len)
        {
            return alloc.Allocate((ulong)len);
        }

        public void Free(int[] blocks)
        {
            alloc.Free(blocks);
        }

        public unsafe byte* Update(int block_idx)
        {
            return buf.Update() + block_idx * alloc.BlockSize;
        }

        public void UpdateDone(int block_idx)
        {
            buf.UpdateDone(block_idx * alloc.BlockSize, alloc.BlockSize);
        }
    }
}
