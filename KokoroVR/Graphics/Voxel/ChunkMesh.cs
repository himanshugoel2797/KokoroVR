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
        private Vector4[] bounding_spheres;

        public int Length { get; private set; }

        public uint BlockSize { get => index_buf.BlockSize; }

        public int[] AllocIndices { get; private set; }
        public ImageHandle VertexBuffer { get => vertex_buf.Image; }

        public ChunkMesh(BufferAllocator index_buf)
        {
            this.index_buf = index_buf;
        }

        public void Reallocate(byte[] vertices, uint[] indices, Vector3 offset)
        {
            //allocate a new vertex buffer
            vertex_buf = new BufferTexture(vertices.Length, PixelInternalFormat.Rgba8ui, false);
            Length = indices.Length * sizeof(uint);

            if (AllocIndices != null) index_buf.Free(AllocIndices);
            AllocIndices = index_buf.Allocate(indices.Length * sizeof(uint));
            bounding_spheres = new Vector4[AllocIndices.Length];
            unsafe
            {
                //Upload the vertex data
                var v_d_p = vertex_buf.Update();
                fixed (byte* v_s_p = vertices)
                    Buffer.MemoryCopy(v_s_p, v_d_p, vertices.Length, vertices.Length);
                vertex_buf.UpdateDone(0, vertices.Length);

                //While uploading the index data compute each block's bounds too
                int idx_buf_idx = 0;
                for (int i = 0; i < AllocIndices.Length; i++)
                {
                    Vector3 min = new Vector3(float.MaxValue);
                    Vector3 max = new Vector3(float.MinValue);

                    var i_d_p = (uint*)index_buf.Update(AllocIndices[i]);
                    for (int j = 0; j < index_buf.BlockSize / sizeof(uint); j++)
                    {
                        var cur = new Vector3(vertices[(ushort)(indices[idx_buf_idx] & 0xffff) * 4], vertices[(ushort)(indices[idx_buf_idx] & 0xffff) * 4 + 1], vertices[(ushort)(indices[idx_buf_idx] & 0xffff) * 4 + 2]);
                        min = Vector3.ComponentMin(min, cur + offset);
                        max = Vector3.ComponentMax(max, cur + offset);

                        i_d_p[j] = indices[idx_buf_idx];
                        idx_buf_idx++;
                        if (idx_buf_idx == indices.Length)
                            break;
                    }
                    index_buf.UpdateDone(AllocIndices[i]);
                    bounding_spheres[i] = new Vector4((min + max) * 0.5f, (max - min).Length * 0.5f);

                    if (idx_buf_idx == indices.Length)
                        break;
                }
            }
        }

        public (int, uint)[] Sort(Frustum f, Vector3 eyePos)
        {
            var blocks = new List<(int, uint)>();
            long cnt = 0;
            uint last_blk_len = (uint)(Length - (AllocIndices.Length - 1) * BlockSize);

            for (int i = 0; i < AllocIndices.Length; i++)
                if (IsVisible(f, i))
                    if (i < AllocIndices.Length - 1)
                        blocks.Add((i, BlockSize));
                    else
                        blocks.Add((i, last_blk_len));

            return blocks.OrderBy(a => (bounding_spheres[a.Item1].Xyz - eyePos).LengthSquared).ToArray();
        }

        public bool IsVisible(Frustum f, int k)
        {
            return f.IsVisible(bounding_spheres[k]);
        }
    }
}
