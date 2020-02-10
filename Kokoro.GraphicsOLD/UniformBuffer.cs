using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public class UniformBuffer : IMappedBuffer
    {
        #region Bind point allocation
        private static int freebindPoint = 0;
        private static int maxBindPoints = 0;

        private static int GetFreeBindPoint()
        {
            if (freebindPoint >= maxBindPoints)
                throw new Exception("Too many UBOs!");
            return (freebindPoint++ % maxBindPoints);
        }
        #endregion


        private const int UniformBufferSize = 64 * 1024;

        static UniformBuffer()
        {
            maxBindPoints = GL.GetInteger(GetPName.MaxUniformBufferBindings);
        }

        const int rungs = 4;
        internal GPUBuffer buf;
        internal int bindPoint = 0;
        internal int curRung = 0;
        internal Fence[] readyFence;
        internal bool dynamic;

        public bool IsReady
        {
            get
            {
                return readyFence[curRung].Raised(1);
            }
        }

        public long Size
        {
            get
            {
                return dynamic ? UniformBufferSize / rungs : UniformBufferSize;
            }
        }

        public long Offset
        {
            get
            {
                return curRung * Size;
            }
        }

        public UniformBuffer(bool dynamic)
        {
            this.dynamic = dynamic;
            buf = new GPUBuffer(BufferUsage.UniformBuffer, (ulong)UniformBufferSize, false);
            bindPoint = GetFreeBindPoint();

            readyFence = new Fence[dynamic ? rungs : 1];
            for (int i = 0; i < readyFence.Length; i++)
            {
                readyFence[i] = new Fence();
                readyFence[i].PlaceFence();
            }
        }

        internal ulong GetReadyOffset()
        {
            int idx = curRung;
            for (int i = 0; i < readyFence.Length; i++)
            {
                if (readyFence[idx].Raised(1))
                    return (ulong)idx * (ulong)Size;

                if (idx == 0)
                    idx = readyFence.Length - 1;
                else
                    idx--;
            }

            return (ulong)curRung - 1;
        }

        public unsafe byte* Update()
        {
            if (dynamic) curRung = (curRung + 1) % rungs;
            while (!readyFence[curRung].Raised(0)) ;
            return (byte*)buf.GetPtr() + (ulong)curRung * (ulong)Size; ;
        }

        public void UpdateDone()
        {
            buf.FlushBuffer((ulong)curRung * (ulong)Size, (ulong)Size);
            readyFence[curRung].PlaceFence();
        }

        public void UpdateDone(long off, long usize)
        {
            buf.FlushBuffer((ulong)curRung * (ulong)Size + (ulong)off, (ulong)usize);
            readyFence[curRung].PlaceFence();
        }

        public static explicit operator GPUBuffer(UniformBuffer s)
        {
            return s.buf;
        }

        public static explicit operator int(UniformBuffer s)
        {
            return s.buf;
        }

    }
}