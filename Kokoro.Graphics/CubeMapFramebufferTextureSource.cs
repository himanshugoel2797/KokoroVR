using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public class CubeMapFramebufferTextureSource : ITextureSource
    {
        private int width, height, levels;

        public PixelType PixelType { get; set; }
        public PixelInternalFormat InternalFormat { get; set; }
        public PixelFormat Format { get; set; }

        public CubeMapFramebufferTextureSource(int width, int height, int levels)
        {
            this.width = width;
            this.height = height;
            this.levels = levels;
            Format = PixelFormat.Bgra;
        }

        public int GetDepth()
        {
            return 0;
        }

        public int GetDimensions()
        {
            return 2;
        }

        public PixelFormat GetFormat()
        {
            return Format;
        }

        public int GetHeight()
        {
            return height;
        }

        public PixelInternalFormat GetInternalFormat()
        {
            return InternalFormat;
        }

        public int GetLevels()
        {
            return levels;
        }

        public IntPtr GetPixelData(int level)
        {
            return IntPtr.Zero;
        }

        public TextureTarget GetTextureTarget()
        {
            return TextureTarget.TextureCubeMap;
        }

        public int GetWidth()
        {
            return width;
        }

        PixelType ITextureSource.GetPixelType()
        {
            return PixelType;
        }

        public int GetBaseWidth()
        {
            return 0;
        }

        public int GetBaseHeight()
        {
            return 0;
        }

        public int GetBaseDepth()
        {
            return 0;
        }

        public int GetBpp()
        {
            return 4; //TODO
        }

        public int GetSampleCount()
        {
            return 1;
        }

        public TextureUsage GetObjectUsage()
        {
            return TextureUsage.ColorAttachment | TextureUsage.Sampled | TextureUsage.InputAttachment;
        }
    }
}
