using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public interface ITextureSource
    {
        TextureTarget GetTextureTarget();
        int GetDimensions();
        int GetWidth();
        int GetHeight();
        int GetDepth();

        int GetBaseWidth();
        int GetBaseHeight();
        int GetBaseDepth();
        int GetSampleCount();
        int GetLevels();
        PixelInternalFormat GetInternalFormat();
        PixelFormat GetFormat();
        PixelType GetPixelType();
        TextureUsage GetObjectUsage();
        int GetBpp();
        IntPtr GetPixelData(int level);
    }
}
