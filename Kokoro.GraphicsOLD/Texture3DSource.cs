using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public class Texture3DSource : RawTextureSource
    {
        private IntPtr[] data;
        public Texture3DSource(int width, int height, int depth, int levels, PixelFormat pFormat, PixelInternalFormat iFormat, PixelType pType, TextureTarget targ = TextureTarget.Texture3D, params IntPtr[] data) : base(3, width, height, depth, levels, pFormat, iFormat, targ, pType, TextureUsage.Sampled)
        {
            this.data = data;
        }

        public override IntPtr GetPixelData(int level)
        {
            return data[level];
        }
    }
}
