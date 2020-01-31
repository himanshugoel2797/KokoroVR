﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public class BufferTextureSource : ITextureSource
    {
        private long width;
        private GPUBuffer buf;

        public PixelInternalFormat InternalFormat { get; set; }

        public BufferTextureSource(StorageBuffer ssbo)
        {
            buf = ssbo.buf;
            width = (int)ssbo.Size;
        }

        public BufferTextureSource(UniformBuffer ubo)
        {
            buf = ubo.buf;
            width = ubo.Size;
        }

        public int GetDepth()
        {
            return 0;
        }

        public int GetDimensions()
        {
            return 1;
        }

        public PixelFormat GetFormat()
        {
            return PixelFormat.Bgra;
        }

        public int GetHeight()
        {
            return 1;
        }

        public PixelInternalFormat GetInternalFormat()
        {
            return InternalFormat;
        }

        public int GetLevels()
        {
            return 1;
        }

        public IntPtr GetPixelData(int level)
        {
            return (IntPtr)buf.id;
        }

        public PixelType GetPixelType()
        {
            return PixelType.Byte;
        }

        public TextureTarget GetTextureTarget()
        {
            return TextureTarget.TextureBuffer;
        }

        public int GetWidth()
        {
            return (int)width;  //TODO: Fix this mess, make everything consistently long as needed
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
            return TextureUsage.InputAttachment;
        }
    }
}