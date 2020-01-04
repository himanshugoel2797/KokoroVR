using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public class Mesh2
    {
        private MeshGroup2 grp;
        private int net_len;
        internal int[] allocs;

        public MeshGroup2 Parent { get => grp; }
        public int Length { get => net_len; }
        public int[] AllocIndices { get => allocs; }

        public Mesh2(MeshGroup2 grp) { this.grp = grp; }

        public unsafe void Reallocate(byte* verts, byte* props, byte* indices, int len)
        {
            Free();
            allocs = grp.Allocate(len);
            net_len = len;
            Update(verts, props, indices, len);
        }

        public unsafe void Update(byte* verts, byte* props, byte* indices, int len)
        {
            if (allocs == null)
                allocs = grp.Allocate(len);
            net_len = len;

            var v_p = verts;
            var p_p = props;
            var i_p = indices;
            var l = len;
            for (int i = 0; i < allocs.Length; i++)
            {
                grp.Update(allocs[i], v_p, p_p, i_p, System.Math.Min(l, grp.BlockSize));

                v_p += grp.BlockSize * 4 * grp.VertexBitWidth / 8;
                if (p_p != null) p_p += grp.BlockSize * 4 * grp.PropertyBitWidth / 8;
                if (i_p != null) i_p += grp.BlockSize * grp.IndexBitWidth / 8;
                l -= grp.BlockSize;
            }
        }

        public void Free()
        {
            if (allocs != null) grp.Free(allocs);
            allocs = null;
            net_len = 0;
        }
    }
}
