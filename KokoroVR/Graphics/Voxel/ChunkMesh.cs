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
        private BufferTexture vertex_buf;
        private BufferAllocator index_buf;

        public int Length { get; private set; }

        public uint BlockSize { get => index_buf.BlockSize; }

        public int[] AllocIndices { get; private set; }
        public ImageHandle VertexBuffer { get => vertex_buf.View.GetImageHandle(); }

        public ChunkMesh(BufferAllocator index_buf)
        {
            this.index_buf = index_buf;
        }

        static int cntr = 0;
        public void Reallocate(byte[] chunk, byte[] vertices, uint[] indices, Vector4[] bounds, byte[] norms, Vector3 offset)
        {

            //allocate a new vertex buffer
            vertex_buf = new BufferTexture(vertices.Length, PixelInternalFormat.Rgba8ui, false);
            Length = indices.Length * sizeof(uint);

            if (AllocIndices != null) index_buf.Free(AllocIndices);
            AllocIndices = index_buf.Allocate(indices.Length * sizeof(uint));
            unsafe
            {   
                //Upload the vertex data
                var v_d_p = vertex_buf.Update();
                fixed (byte* v_s_p = vertices)
                    Buffer.MemoryCopy(v_s_p, v_d_p, vertex_buf.Size, vertices.Length);
                vertex_buf.UpdateDone(0, vertices.Length);

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

        public (int, uint)[] Sort(Frustum f, Vector3 eyePos)
        {
            var blocks = new List<(int, uint)>();
            uint last_blk_len = (uint)(Length - (AllocIndices.Length - 1) * BlockSize);

            for (int i = 0; i < AllocIndices.Length; i++)
                if (IsVisible(f, i))
                    if (i < AllocIndices.Length - 1)
                        blocks.Add((i, BlockSize));
                    else
                        blocks.Add((i, last_blk_len));

            return blocks.ToArray();
        }

        public bool IsVisible(Frustum f, int k)
        {
            //Check the norm mask
            return true;
        }
    }
}
