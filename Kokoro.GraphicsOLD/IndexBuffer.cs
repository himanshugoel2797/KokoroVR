using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public class IndexBuffer : IMappedBuffer
    {
        private StorageBuffer buffer;
        private bool is_short_idx;
        internal VertexArray varray;

        public StorageBuffer Buffer { get => buffer; }
        public long IndexCount { get; private set; }
        public bool IsShort { get => is_short_idx; }
        public long Size => ((IMappedBuffer)this.buffer).Size;

        public long Offset => ((IMappedBuffer)this.buffer).Offset;

        public IndexBuffer(StorageBuffer buffer, bool short_idx)
        {
            IndexCount = buffer.Size / (short_idx ? 2L : 4L);
            is_short_idx = short_idx;
            this.buffer = buffer;
            varray = new VertexArray();
            varray.SetElementBufferObject((GpuBuffer)Buffer);
        }

        public unsafe byte* Update()
        {
            return ((IMappedBuffer)this.buffer).Update();
        }

        public void UpdateDone()
        {
            ((IMappedBuffer)this.buffer).UpdateDone();
        }

        public void UpdateDone(long off, long usize)
        {
            ((IMappedBuffer)this.buffer).UpdateDone(off, usize);
        }
    }
}
