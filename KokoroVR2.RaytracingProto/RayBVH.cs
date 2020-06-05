using System;
using System.Collections.Generic;
using System.Text;

namespace KokoroVR2.RaytracingProto
{
    public class RayBVH
    {
        public unsafe struct TreeEntry
        {
            public float minx;
            public float miny;
            public float minz;
            public uint  base_idx;
            public float maxx;
            public float maxy;
            public float maxz;
            public float tmax;
        }

        public IntPtr tree;

        public void Build(float[] verts, uint[] inds)
        {
            //compute centroids
            //sort by 
        }
    }
}
