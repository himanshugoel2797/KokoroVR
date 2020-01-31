using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.InteropServices;
using Kokoro.Graphics;

namespace Kokoro.Graphics
{
    public class GPUBuffer : IDisposable, IGPUBuffer
    {
        public ulong Size { get; private set; }
        private int id;
        private IntPtr addr;

        public GPUBuffer(BufferUsage target, ulong size, bool read)
        {
            GL.CreateBuffers(1, out id);
            this.Size = size;
            GL.NamedBufferStorage(id, (IntPtr)size, IntPtr.Zero, BufferStorageFlags.MapPersistentBit | BufferStorageFlags.MapWriteBit | (read ? BufferStorageFlags.MapReadBit : 0));
            addr = GL.MapNamedBufferRange(id, IntPtr.Zero, (IntPtr)size, BufferAccessMask.MapPersistentBit | BufferAccessMask.MapFlushExplicitBit | BufferAccessMask.MapUnsynchronizedBit | BufferAccessMask.MapWriteBit | (read ? BufferAccessMask.MapReadBit : 0));
        }

        public IntPtr GetPtr()
        {
            return addr;
        }

        public void FlushBuffer(ulong offset, ulong size)
        {
            GL.FlushMappedNamedBufferRange(id, (IntPtr)offset, (int)size);
        }

        public void UnmapBuffer()
        {
            GL.UnmapNamedBuffer(id);
        }

        public static implicit operator int(GPUBuffer s)
        {
            return s.id;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        public bool Disposed { get { return disposedValue; } }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                }

                if (addr != IntPtr.Zero)
                {
                    addr = IntPtr.Zero;
                    try
                    {
                        GL.UnmapNamedBuffer(id);
                    }
                    catch (Exception)
                    {

                    }
                }
                GraphicsDevice.QueueForDeletion(id, GLObjectType.Buffer);
                id = 0;
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        ~GPUBuffer()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            //Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}