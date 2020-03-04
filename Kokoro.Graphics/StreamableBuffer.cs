using System;
using System.Collections.Generic;
using System.Text;

namespace Kokoro.Graphics
{
    public class StreamableBuffer
    {
        private Framegraph graph;
        private bool isDirty;

        public GpuBuffer LocalBuffer { get; }
        public GpuBuffer HostBuffer { get; }
        public string Name { get; }

        public StreamableBuffer(string name, Framegraph graph, ulong sz, BufferUsage usage)
        {
            this.graph = graph;
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

        public void GenerateRenderGraph()
        {
            graph.RegisterPass(new BufferUploadPass()
            {
                Active = true,
                SourceBuffer = HostBuffer,
                DestBuffer = LocalBuffer,
                DeviceOffset = 0,
                LocalOffset = 0,
                Name = Name,
                Size = HostBuffer.Size
            });
        }

        public void Update()
        {
            if (isDirty)
            {
                graph.SetActiveState(Name, true);
                isDirty = false;
            }
            else
            {
                graph.SetActiveState(Name, false);
            }
        }
    }
}
