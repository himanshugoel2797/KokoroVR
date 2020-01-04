using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public class MeshGroup2 : IDisposable
    {
        private ulong[] blk_status;
        private int vertW, propW, indexW, blk_sz, blk_cnt, free_blks;
        private ShaderStorageBuffer vertSSBO, propSSBO, indexSSBO;
        private Texture vertBO, propBO, indexBO;
        private ImageHandle vertIH, propIH, indexIH;

        public int BlockSize { get => blk_sz; }
        public int VertexBitWidth { get => vertW; }
        public int PropertyBitWidth { get => propW; }
        public int IndexBitWidth { get => indexW; }
        public ImageHandle Vertices { get => vertIH; }
        public ImageHandle Properties { get => propIH; }
        public ImageHandle Indices { get => indexIH; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vertBitWidth">8/16/32</param>
        /// <param name="propBitWidth">0/16/32</param>
        /// <param name="indexBitWidth">0/16/32</param>
        public MeshGroup2(int vertBitWidth, int propBitWidth, int indexBitWidth, int blk_sz, int blk_cnt)
        {
            this.blk_sz = blk_sz;
            this.blk_cnt = blk_cnt;
            this.free_blks = blk_cnt;
            int sts_map_len = blk_cnt / (sizeof(ulong) * 8);
            if (blk_cnt % (sizeof(ulong) * 8) != 0) sts_map_len++;
            this.blk_status = new ulong[sts_map_len];
            for (int i = 0; i < sts_map_len; i++) blk_status[i] = ~(ulong)0;

            //X8i,Y8i,Z8i,MatID8i
            //X16i,Y16i,Z16i,MatID16i
            //X32f,Y32f,Z32f, MatID32f
            this.vertW = vertBitWidth;

            //NX16,NY16,US16,UT16
            //NX32,NY32,US32,UT32
            this.propW = propBitWidth;

            //INDEX16
            //INDEX32
            this.indexW = indexBitWidth;

            //Each of these is optional, setup similar shader hinting API for access substitution, multidraw can swap vbos as needed, increasing batching
            switch (vertW)
            {
                case 8:
                case 16:
                case 32:
                    var iFormat = PixelInternalFormat.Rgba8i;
                    if (vertW == 16) iFormat = PixelInternalFormat.Rgba16i;
                    if (vertW == 32) iFormat = PixelInternalFormat.Rgba32f;

                    vertSSBO = new ShaderStorageBuffer(4 * blk_sz * blk_cnt * vertW / 8, false);
                    vertBO = new Texture();
                    vertBO.SetData(new BufferTextureSource(vertSSBO)
                    {
                        InternalFormat = iFormat
                    }, 0);
                    vertIH = vertBO.GetImageHandle(0, 0, iFormat);
                    vertIH.SetResidency(Residency.Resident, AccessMode.Read);
                    break;

                default:
                    throw new Exception();
            }

            switch (propW)
            {
                case 0:
                    break;
                case 16:
                case 32:
                    var iFormat = PixelInternalFormat.Rgba16f;
                    if (propW == 32) iFormat = PixelInternalFormat.Rgba32f;

                    propSSBO = new ShaderStorageBuffer(4 * blk_sz * blk_cnt * propW / 8, false);
                    propBO = new Texture();
                    propBO.SetData(new BufferTextureSource(propSSBO)
                    {
                        InternalFormat = iFormat
                    }, 0);
                    propIH = propBO.GetImageHandle(0, 0, iFormat);
                    propIH.SetResidency(Residency.Resident, AccessMode.Read);
                    break;
                default:
                    throw new Exception();
            }

            switch (indexW)
            {
                case 0:
                    break;
                case 16:
                case 32:
                    var iFormat = PixelInternalFormat.R16ui;
                    if (propW == 32) iFormat = PixelInternalFormat.R32ui;

                    indexSSBO = new ShaderStorageBuffer(blk_sz * blk_cnt * indexW / 8, false);
                    indexBO = new Texture();
                    indexBO.SetData(new BufferTextureSource(indexSSBO)
                    {
                        InternalFormat = iFormat
                    }, 0);
                    indexIH = indexBO.GetImageHandle(0, 0, iFormat);
                    indexIH.SetResidency(Residency.Resident, AccessMode.Read);
                    break;
                default:
                    throw new Exception();
            }
        }

        public unsafe void Update(int block_idx, byte* verts, byte* props, byte* indices, int len)
        {
            if (verts != null)
            {
                var v_p = vertSSBO.Update();
                Buffer.MemoryCopy(verts, v_p + 4 * blk_sz * vertW / 8 * block_idx, 4 * len * vertW / 8, 4 * len * vertW / 8);
                vertSSBO.UpdateDone(4 * blk_sz * vertW / 8 * block_idx, 4 * len * vertW / 8);
            }

            if (props != null && propSSBO != null)
            {
                var p_p = propSSBO.Update();
                Buffer.MemoryCopy(props, p_p + 4 * blk_sz * propW / 8 * block_idx, 4 * len * propW / 8, 4 * len * propW / 8);
                propSSBO.UpdateDone(4 * blk_sz * propW / 8 * block_idx, 4 * len * propW / 8);
            }

            if (indices != null && indexSSBO != null)
            {
                var i_p = indexSSBO.Update();
                Buffer.MemoryCopy(props, i_p + blk_sz * indexW / 8 * block_idx, len * indexW / 8, len * indexW / 8);
                propSSBO.UpdateDone(blk_sz * indexW / 8 * block_idx, len * indexW / 8);
            }
        }

        public int[] Allocate(int size)
        {
            int a_blk_cnt = size / blk_sz;
            if (size % blk_sz != 0) a_blk_cnt++;

            if (free_blks < a_blk_cnt)
                return null;

            var indices = new int[a_blk_cnt];
            int alloc_cntr = 0;
            for (int i = 0; i < blk_cnt; i++)
            {
                if (alloc_cntr >= a_blk_cnt)
                    break;

                int off = i / 64;
                int bit = i % 64;

                if ((blk_status[off] & (1uL << bit)) != 0)
                {
                    indices[alloc_cntr++] = i;
                    blk_status[off] = blk_status[off] & ~(1uL << bit);
                    free_blks--;
                }
            }

            return indices;
        }

        public void Free(int[] blocks)
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                int off = blocks[i] / 64;
                int bit = blocks[i] % 64;

                blk_status[off] |= (1uL << bit);
                free_blks++;
            }
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
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MeshGroup2()
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
