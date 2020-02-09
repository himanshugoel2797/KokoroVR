using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public class StorageBuffer : IMappedBuffer
    {
        private static int alignmentRequirement = 0;
        static StorageBuffer()
        {
            alignmentRequirement = GL.GetInteger((GetPName)All.ShaderStorageBufferOffsetAlignment);
        }

        const int rungs = 3;

        internal int curRung = 0;
        private GPUBuffer buf;
        internal Fence[] readyFence;
        internal ulong size;

        bool stream = false;

        public long Size
        {
            get { return (long)size; }
        }

        public long Offset
        {
            get { return curRung * Size; }
        }

        StorageBuffer(GPUBuffer buf, bool stream)
        {
            size = buf.Size / (ulong)(stream ? rungs : 1);
            this.buf = buf;
            this.stream = stream;

            readyFence = new Fence[rungs];

            for (int i = 0; i < rungs; i++)
            {
                readyFence[i] = new Fence();
                readyFence[i].PlaceFence();
            }
        }

        private static long AlignSize(long size)
        {
            if (size % alignmentRequirement == 0) return size;
            return size + (alignmentRequirement - (size % alignmentRequirement));
        }

        public StorageBuffer(long size, bool stream, bool read = false) : this(new GPUBuffer(BufferUsage.StorageBuffer, (ulong)(AlignSize(size) * (stream ? rungs : 1)), read), stream)
        {

        }

        internal ulong GetReadyOffset()
        {
            if (!stream)
                return 0;

            int idx = curRung;
            for (int i = 0; i < rungs; i++)
            {
                if (readyFence[idx].Raised(1))
                    return (ulong)idx * size;

                if (idx == 0)
                    idx = rungs - 1;
                else
                    idx--;
            }

            return (ulong)(curRung - 1);
        }

        public static explicit operator GPUBuffer(StorageBuffer s)
        {
            Contract.Requires<ArgumentNullException>(s.buf != null);
            return s.buf;
        }

        public static explicit operator int(StorageBuffer s)
        {
            Contract.Requires<ArgumentNullException>(s.buf != null);
            return s.buf;
        }

        public unsafe byte* Update()
        {
            if (stream) curRung = (curRung + 1) % rungs;
            while (!readyFence[curRung].Raised(0)) ;// System.Threading.Thread.Sleep(1);
            return (byte*)buf.GetPtr() + (ulong)curRung * size;
        }

        public void UpdateDone()
        {
            buf.FlushBuffer((ulong)curRung * size, (ulong)size);
            readyFence[curRung].PlaceFence();
        }

        public void UpdateDone(long off, long usize)
        {
            buf.FlushBuffer((ulong)curRung * size + (ulong)off, (ulong)usize);
            readyFence[curRung].PlaceFence();
        }

        public bool IsReady
        {
            get
            {
                return readyFence[curRung].Raised(1);
            }
        }
    }
}