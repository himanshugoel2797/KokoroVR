using System;
using System.Collections.Generic;
using System.Text;

namespace Kokoro.Graphics
{
    public class BlockAllocator
    {
        private ulong[] blk_status;
        private uint blk_cnt, free_blks;

        public uint BlockSize { get; private set; }

        public BlockAllocator(uint block_cnt, uint block_sz)
        {
            BlockSize = block_sz;
            blk_cnt = block_cnt;
            free_blks = blk_cnt;

            uint map_len = blk_cnt / (sizeof(ulong) * 8);
            if (blk_cnt % (sizeof(ulong) * 8) != 0) map_len++;
            blk_status = new ulong[map_len];
            for (int i = 0; i < map_len; i++) blk_status[i] = ~(ulong)0;
        }

        public int[] Allocate(ulong sz)
        {
            uint a_blk_cnt = (uint)(sz / BlockSize);
            if (sz % BlockSize != 0) a_blk_cnt++;

            if (free_blks < a_blk_cnt)
                return null;

            var indices = new int[a_blk_cnt];
            int alloc_cntr = 0;
            for (int i = 0; i < blk_cnt; i++)
            {
                if (alloc_cntr >= a_blk_cnt)
                    break;

                int off = i / 64;
                int bit = i % 64;

                if ((blk_status[off] & (1uL << bit)) != 0)
                {
                    indices[alloc_cntr++] = i;
                    blk_status[off] = blk_status[off] & ~(1uL << bit);
                    free_blks--;
                }
            }

            return indices;
        }
        public void Free(int[] blocks)
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                int off = blocks[i] / 64;
                int bit = blocks[i] % 64;

                blk_status[off] |= (1uL << bit);
                free_blks++;
            }
        }
    }
}
