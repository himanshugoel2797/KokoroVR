using System;
using System.Collections.Generic;
using System.Text;

namespace Kokoro.Graphics
{
    public class StreamableBuffer
    {
        private bool isDirty;

        public GpuBuffer LocalBuffer { get; }
        public GpuBuffer HostBuffer { get; }
        public string Name { get; }

        public StreamableBuffer(string name, ulong sz, BufferUsage usage)
        {
            this.Name = name;
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
            isDirty = true;
        }

        public unsafe byte* BeginBufferUpdate()
        {
            return (byte*)HostBuffer.GetAddress();
        }

        public void EndBufferUpdate()
        {
            isDirty = true;
        }

        public void Update()
        {
            if (isDirty)
            {
                isDirty = false;
            }
            else
            {

            }
        }
    }
}
