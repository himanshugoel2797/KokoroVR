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
    public class GPUBuffer : IDisposable
    {
        internal int id;
        internal BufferUsage target;
        internal ulong size;
        public int dataLen;

        private IntPtr addr;

        public GPUBuffer(BufferUsage target, ulong size, bool read, bool host_write)
        {
            GL.CreateBuffers(1, out id);
            this.target = target;

            this.size = size;

            Console.WriteLine(size);
            Console.WriteLine("\t" + (IntPtr)size);
            GL.NamedBufferStorage(id, (IntPtr)size, IntPtr.Zero, BufferStorageFlags.MapPersistentBit | BufferStorageFlags.MapWriteBit | (read ? BufferStorageFlags.MapReadBit : 0));
            addr = GL.MapNamedBufferRange(id, IntPtr.Zero, (IntPtr)size, BufferAccessMask.MapPersistentBit | BufferAccessMask.MapFlushExplicitBit | BufferAccessMask.MapUnsynchronizedBit | BufferAccessMask.MapWriteBit | (read ? BufferAccessMask.MapReadBit : 0));
        }

        public void BufferData<T>(ulong offset, T[] data, BufferUsageHint hint) where T : struct
        {
            //if (data.Length == 0) return;

            dataLen = data.Length;

            if (data.Length != 0) size = (ulong)(Marshal.SizeOf(data[0]) * data.Length);

            if (addr == IntPtr.Zero)
            {
                if (data.Length < 1) throw new Exception("Buffer is empty!");
                GL.NamedBufferData(id, (int)size, data, hint);
            }
            else
            {
                throw new Exception("This buffer is mapped!");
            }
        }

        public IntPtr GetPtr()
        {
            return addr;
        }

        public void FlushBuffer(ulong offset, ulong size)
        {
            GL.FlushMappedNamedBufferRange(id, (IntPtr)offset, (int)size);
        }

        public static void FlushAll()
        {
            GL.MemoryBarrier(MemoryBarrierFlags.ClientMappedBufferBarrierBit);
        }

        public void UnMapBuffer()
        {
            GL.UnmapNamedBuffer(id);
        }

        public void MapBuffer(bool read)
        {
            addr = GL.MapNamedBufferRange(id, IntPtr.Zero, (int)size, BufferAccessMask.MapPersistentBit | BufferAccessMask.MapUnsynchronizedBit | BufferAccessMask.MapFlushExplicitBit | BufferAccessMask.MapWriteBit | (read ? BufferAccessMask.MapReadBit : 0));
        }

        public void MapBuffer(bool read, ulong offset, ulong size)
        {
            addr = GL.MapNamedBufferRange(id, (IntPtr)offset, (int)size, BufferAccessMask.MapPersistentBit | BufferAccessMask.MapUnsynchronizedBit | BufferAccessMask.MapFlushExplicitBit | BufferAccessMask.MapWriteBit | (read ? BufferAccessMask.MapReadBit : 0));
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