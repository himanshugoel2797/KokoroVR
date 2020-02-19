using Kokoro.Graphics;
using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Graphics.Voxel
{
    public class ChunkMesh : IMesh2
    {
        private ChunkBuffer vbo;
        private BufferAllocator index_buf;

        public int Length { get; private set; }

        public uint BlockSize { get => index_buf.BlockSize; }

        public int[] AllocIndices { get; private set; }

        public ChunkMesh(BufferAllocator index_buf, ChunkBuffer vbo)
        {
            this.index_buf = index_buf;
            this.vbo = vbo;
        }

        private uint baseVertex;
        private int bufferIdx;
        private BoundingBox[] bounds;

        public uint BaseVertex { get => baseVertex; private set => baseVertex = value; }
        public int BufferIdx { get => bufferIdx; private set => bufferIdx = value; }

        public void Reallocate(byte[] vertices, uint[] indices, BoundingBox[] bounds, byte[] norms, Vector3 offset)
        {
            if (this.bounds != null) vbo.Free(bufferIdx, baseVertex);
            this.bounds = bounds;
            //allocate a new vertex buffer
            vbo.Allocate(vertices, out bufferIdx, out baseVertex);
            Length = indices.Length * sizeof(uint);

            if (AllocIndices != null) index_buf.Free(AllocIndices);
            AllocIndices = index_buf.Allocate(indices.Length * sizeof(uint));
            if (bounds.Length != AllocIndices.Length) throw new Exception();
            unsafe
            {
                //Upload the vertex data
                var v_d_p = vbo.Update(BufferIdx, BaseVertex);
                fixed (byte* v_s_p = vertices)
                    Buffer.MemoryCopy(v_s_p, v_d_p, vertices.Length, vertices.Length);
                vbo.UpdateDone(BufferIdx, BaseVertex);

                //While uploading the index data compute each block's bounds too
                long idx_buf_idx = indices.Length * sizeof(uint);
                fixed (uint* i_s_p = indices)
                    for (int i = 0; i < AllocIndices.Length; i++)
                    {
                        var i_d_p = (uint*)index_buf.Update(AllocIndices[i]);
                        Buffer.MemoryCopy(i_s_p + i * index_buf.BlockSize / sizeof(uint), i_d_p, index_buf.BlockSize, Math.Min(idx_buf_idx, index_buf.BlockSize));
                        index_buf.UpdateDone(AllocIndices[i]);
                        idx_buf_idx -= index_buf.BlockSize;
                    }
            }
        }

        public (int, uint, Vector3, Vector3)[] Sort(Frustum f, Vector3 eyePos)
        {
            var blocks = new List<(int, uint, Vector3, Vector3)>();
            uint last_blk_len = (uint)(Length - (AllocIndices.Length - 1) * BlockSize);

            for (int i = 0; i < AllocIndices.Length; i++)
                if (IsVisible(f, i))
                    if (i < AllocIndices.Length - 1)
                        blocks.Add((i, BlockSize, bounds[i].Min, bounds[i].Max));
                    else
                        blocks.Add((i, last_blk_len, bounds[i].Min, bounds[i].Max));

            return blocks.ToArray();
        }

        public bool IsVisible(Frustum f, int k)
        {
            //Check the norm mask
            return f.IsVisible(bounds[k].Min, bounds[k].Max);
        }
    }
}
