using System;

namespace Kokoro.Graphics
{
    public interface IGPUBuffer
    {
        bool Disposed { get; }
        ulong Size { get; }

        void Dispose();
        void FlushBuffer(ulong offset, ulong size);
        IntPtr GetPtr();
        void UnmapBuffer();
    }
}