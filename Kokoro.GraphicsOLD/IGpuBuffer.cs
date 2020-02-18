using System;

namespace Kokoro.Graphics
{
    public interface IGpuBuffer
    {
        bool Disposed { get; }
        ulong Size { get; }

        void Dispose();
        void FlushBuffer(ulong offset, ulong size);
        IntPtr GetPtr();
    }
}