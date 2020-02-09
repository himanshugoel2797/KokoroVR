using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics.Prefabs
{
    public class FullScreenTriangleFactory
    {
        public static void Create(out float[] verts, out float[] uvs)
        {
            verts = new float[]{
                -1, -1, 0.5f,
                3, -1, 0.5f,
                -1, 3, 0.5f
            };

            uvs = new float[] {
                0,0,
                2,0,
                0,2
            };
        }
    }
}
