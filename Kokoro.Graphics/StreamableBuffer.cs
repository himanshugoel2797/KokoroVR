using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Kokoro.Graphics
{
    public class StreamableBuffer
    {
        struct StageOffsets
        {
            public ulong Offset;
            public ulong Size;
        }

        private bool isDirty;
        private ConcurrentQueue<StageOffsets> stagingSet;

        public GpuBuffer LocalBuffer { get; }
        public GpuBuffer HostBuffer { get; }
        public string Name { get; }
        public ulong Size { get; }

        public StreamableBuffer(string name, ulong sz, BufferUsage usage)
        {
            this.Name = name;
            this.Size = sz;
            this.stagingSet = new ConcurrentQueue<StageOffsets>();
            LocalBuffer = new GpuBuffer()
            {
                Name = name,
                MemoryUsage = MemoryUsage.GpuOnly,
                Size = sz,
                Usage = usage | BufferUsage.TransferDst,
            };
            LocalBuffer.Build(0);

            HostBuffer = new GpuBuffer()
            {
                Mapped = true,
                Name = name + "_host",
                Size = sz,
                MemoryUsage = MemoryUsage.CpuOnly,
                Usage = BufferUsage.TransferSrc
            };
            HostBuffer.Build(0);
        }

        public unsafe byte* BeginBufferUpdate()
        {
            return (byte*)HostBuffer.GetAddress();
        }

        public void EndBufferUpdate()
        {
            stagingSet.Enqueue(new StageOffsets()
            {
                Offset = 0,
                Size = Size
            });
        }

        public void EndBufferUpdate(ulong offset, ulong size)
        {
            stagingSet.Enqueue(new StageOffsets()
            {
                Offset = offset,
                Size = size,
            });
        }

        public void Update()
        {
            while (stagingSet.TryDequeue(out var flushCmd))
                GraphicsContext.RenderGraph.QueueOp()
        }
    }
}
