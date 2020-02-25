using System.Runtime.InteropServices;
using System;

namespace Kokoro.Graphics
{
    public class StructuredLocalBuffer<T> : IDisposable where T : unmanaged
    {
        public unsafe T* Pointer { get; private set; }
        public ulong Length { get; set; } = 1;
        public BufferUsage Usage { get; set; }
        internal GpuBuffer backingBuffer;
        private bool locked;

        public StructuredLocalBuffer()
        {

        }

        public void Build(int device_index)
        {
            if (!locked)
            {
                backingBuffer = new GpuBuffer()
                {
                    Mapped = true,
                    Size = (ulong)Marshal.SizeOf<T>() * Length,
                    Usage = Usage,
                    MemoryUsage = MemoryUsage.CpuOnly
                };
                backingBuffer.Build(device_index);
                unsafe
                {
                    Pointer = (T*)backingBuffer.GetAddress();
                }
                locked = true;
            }
            else
                throw new Exception("StructuredLocalBuffer is locked.");
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (locked)
                    {
                        backingBuffer.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~StructuredLocalBuffer()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
