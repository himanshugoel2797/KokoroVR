﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Kokoro.Graphics
{
    public class BufferAllocator
    {
        private BlockAllocator alloc;
        private bool isDirty;

        public uint BlockSize { get => alloc.BlockSize; }
        public GpuBufferView BufferTex { get; }
        public GpuBuffer LocalBuffer { get; }
        public GpuBuffer HostBuffer { get; }
        public string Name { get; }

        public BufferAllocator(string name, uint block_sz, uint block_cnt, BufferUsage usage, ImageFormat iFmt)
        {
            this.Name = name;
            alloc = new BlockAllocator(block_cnt, block_sz);
            LocalBuffer = new GpuBuffer()
            {
                Name = name,
                MemoryUsage = MemoryUsage.GpuOnly,
                Size = block_sz * block_cnt,
                Usage = usage | BufferUsage.TransferDst,
            };
            LocalBuffer.Build(0);

            if (((usage & BufferUsage.StorageTexel) | (usage & BufferUsage.UniformTexel)) != 0)
            {
                BufferTex = new GpuBufferView()
                {
                    Name = name + "_view",
                    Format = iFmt,
                    Offset = 0,
                    Size = block_sz * block_cnt,
                };
                BufferTex.Build(LocalBuffer);
            }

            HostBuffer = new GpuBuffer()
            {
                Mapped = true,
                Name = name + "_host",
                Size = block_sz * block_cnt,
                MemoryUsage = MemoryUsage.CpuOnly,
                Usage = BufferUsage.TransferSrc
            };
            HostBuffer.Build(0);
            isDirty = true;
        }

        public int[] Allocate(long len)
        {
            return alloc.Allocate((ulong)len);
        }

        public void Free(int[] blocks)
        {
            alloc.Free(blocks);
        }

        public unsafe byte* BeginBufferUpdate(int block_idx)
        {
            return (byte*)HostBuffer.GetAddress() + block_idx * alloc.BlockSize;
        }

        public void EndBufferUpdate(int block_idx)
        {
            isDirty = true;
        }

        public void Update()
        {
            if (isDirty)
            {
                isDirty = false;
            }
        }
    }
}