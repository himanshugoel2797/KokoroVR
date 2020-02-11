using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public class BufferGroupAllocator
    {
        private BufferGroup grp;
        private BlockAllocator alloc;
        private (string, uint, PixelInternalFormat)[] element_names;

        public uint BlockSize { get => alloc.BlockSize; }
        public BufferGroup Group { get => grp; }

        public BufferGroupAllocator(uint blockSz, uint blockCnt, bool stream, params (string, uint, PixelInternalFormat)[] elements)
        {
            alloc = new BlockAllocator(blockCnt, blockSz);
            grp = new BufferGroup(blockSz * blockCnt, stream, elements);
            element_names = elements;
        }

        public int[] Allocate(long len)
        {
            return alloc.Allocate((ulong)len);
        }

        public void Free(int[] blocks)
        {
            alloc.Free(blocks);
        }

        public unsafe byte*[] Update(int block_idx)
        {
            var ptrs = new byte*[grp.Count];
            for (int i = 0; i < grp.Count; i++)
                ptrs[i] = grp.Update(element_names[i].Item1) + block_idx * element_names[i].Item2 * alloc.BlockSize;

            return ptrs;
        }

        public void UpdateDone(int block_idx)
        {
            for (int i = 0; i < grp.Count; i++)
                grp.UpdateDone(element_names[i].Item1, block_idx * element_names[i].Item2 * alloc.BlockSize, element_names[i].Item2 * alloc.BlockSize);
        }
    }
}
