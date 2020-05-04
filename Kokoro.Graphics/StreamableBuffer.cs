using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Kokoro.Common;

namespace Kokoro.Graphics
{
    public class StreamableBuffer : UniquelyNamedObject
    {
        struct StageOffsets
        {
            public ulong Offset;
            public ulong Size;
        }

        private bool isDirty;
        public GpuBuffer LocalBuffer { get; private set; }
        public GpuBuffer HostBuffer { get; private set; }
        public ulong Size { get; }
        public bool Streamable { get; private set; }

        public StreamableBuffer(string name, ulong sz, BufferUsage usage) : base(name)
        {
            this.Size = sz;
            this.Streamable = true;
            LocalBuffer = new GpuBuffer(name)
            {
                MemoryUsage = MemoryUsage.GpuOnly,
                Size = sz,
                Usage = usage | BufferUsage.TransferDst,
            };
            LocalBuffer.Build(0);

            HostBuffer = new GpuBuffer(name + "_host")
            {
                Mapped = true,
                Size = sz,
                MemoryUsage = MemoryUsage.CpuToGpu,
                Usage = BufferUsage.TransferSrc
            };
            HostBuffer.Build(0);
        }

        public void RebuildGraph()
        {
            GraphicsContext.RenderGraph.RegisterResource(LocalBuffer);

            if (Streamable)
            {
                GraphicsContext.RenderGraph.RegisterResource(HostBuffer);
                GraphicsContext.RenderGraph.RegisterBufferTransferPass(new Framegraph.BufferTransferPass(Name + "_transferOp")
                {
                    Source = HostBuffer.Name,
                    Destination = LocalBuffer.Name,
                    DestinationOffset = 0,
                    SourceOffset = 0,
                    Size = Size
                });
            }
        }

        public unsafe byte* BeginBufferUpdate()
        {
            if (!Streamable)
                throw new InvalidOperationException("Streaming has been ended already!");
            return (byte*)HostBuffer.GetAddress();
        }

        public void EndBufferUpdate()
        {
            isDirty = true;
        }

        public void Update()
        {
            if (Streamable && isDirty)
            {
                GraphicsContext.RenderGraph.QueueOp(new Framegraph.GpuOp()
                {
                    PassName = Name + "_transferOp",
                });
                isDirty = false;
            }
        }

        public void EndStreaming()
        {
            if (Streamable && !isDirty)
            {
                Streamable = false;
                HostBuffer.Dispose();
                HostBuffer = null;
            }
        }
    }
}
