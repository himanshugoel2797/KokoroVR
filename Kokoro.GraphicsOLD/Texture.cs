﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace Kokoro.Graphics
{
    public enum Residency
    {
        NonResident,
        Resident
    }

    public enum AccessMode
    {
        Read = All.ReadOnly,
        Write = All.WriteOnly,
        ReadWrite = All.ReadWrite,
    }

    public class ImageHandle
    {
        internal long hndl = 0;
        internal Texture parent;

        internal ImageHandle(long hndl, Texture parent)
        {
            this.hndl = hndl;
            this.parent = parent;
        }

        public ImageHandle SetResidency(Residency residency, AccessMode m)
        {
            if (residency == Residency.Resident) GL.Arb.MakeImageHandleResident(hndl, (All)m);
            else GL.Arb.MakeImageHandleNonResident(hndl);
            return this;
        }

        public static implicit operator long(ImageHandle handle)
        {
            return handle.hndl;
        }
    }

    public class TextureHandle
    {
        internal long hndl = 0;
        internal Texture parent;
        private bool isResident = false;

        public TextureSampler Sampler { get; private set; }

        internal TextureHandle(long hndl, Texture parent, TextureSampler sampler)
        {
            this.hndl = hndl;
            this.parent = parent;
            this.Sampler = sampler;
        }

        public TextureHandle SetResidency(Residency residency)
        {
            if (residency == Residency.Resident && !isResident)
            {
                GL.Arb.MakeTextureHandleResident(hndl);
                isResident = true;
            }
            else if (residency == Residency.NonResident && isResident)
            {
                GL.Arb.MakeTextureHandleNonResident(hndl);
                isResident = false;
            }
            return this;
        }

        public static implicit operator long(TextureHandle handle)
        {
            return handle.hndl;
        }
    }

    public class Texture : IDisposable
    {
        public static Texture Default { get; private set; }

        static Texture()
        {
            GL.GetFloat((GetPName)OpenTK.Graphics.OpenGL.ExtTextureFilterAnisotropic.MaxTextureMaxAnisotropyExt, out float a);
            MaxAnisotropy = a;

            Default = new Texture();
            /*var src = new System.Drawing.Bitmap(256, 256);
            var g = System.Drawing.Graphics.FromImage(src);
            g.FillRectangle(System.Drawing.Brushes.BlueViolet, 0, 0, 256, 256);
            g.DrawRectangle(System.Drawing.Pens.Black, 0, 0, 256, 256);

            for (int x = 0; x < 256; x += 16)
                g.DrawLine(System.Drawing.Pens.Green, x, 0, x, 256);

            for (int x = 0; x < 256; x += 16)
                g.DrawLine(System.Drawing.Pens.Red, 0, x, 256, x);
                */
            BitmapTextureSource s = new BitmapTextureSource(new System.Drawing.Bitmap(@"I:\Himanshu\Documents\uv_test.jpg"), 1);
            Default.SetData(s, 0);

        }



        internal int id;
        internal PixelInternalFormat internalformat;
        internal TextureTarget texTarget;
        internal PixelFormat format;
        internal PixelType ptype;

        private int _writeLevel;
        private int _baseReadLevel;
        private int _maxReadLevel;

        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public int Depth { get; internal set; }
        public int LevelCount { get; internal set; }

        public bool GenerateMipmaps { get; set; }

        public int WriteLevel { get; set; }

        //TODO: these can't be changed once gethandle has been called
        public int BaseReadLevel
        {
            get { return _baseReadLevel; }
            set { if (_baseReadLevel != value) { _baseReadLevel = value; GL.TextureParameter(id, TextureParameterName.TextureBaseLevel, (float)_baseReadLevel); } }
        }
        public int MaxReadLevel
        {
            get { return _maxReadLevel; }
            set { if (_maxReadLevel != value) { _maxReadLevel = value; GL.TextureParameter(id, TextureParameterName.TextureMaxLevel, (float)_maxReadLevel); } }
        }

        public static float MaxAnisotropy { get; internal set; }

        private Dictionary<int, TextureHandle> handles;


        public Texture()
        {
            id = 0;
            handles = new Dictionary<int, TextureHandle>();
            GraphicsDevice.Cleanup.Add(Dispose);
        }

        public TextureHandle GetHandle(TextureSampler sampler)
        {
            if (!handles.ContainsKey(sampler.id))
            {
                if (sampler.id != 0)
                    handles[sampler.id] = new TextureHandle(GL.Arb.GetTextureSamplerHandle(id, sampler.id), this, sampler);
                else
                    handles[0] = new TextureHandle(GL.Arb.GetTextureHandle(id), this, sampler);
            }

            return handles[sampler.id];
        }

        public ImageHandle GetImageHandle(int level, int layer, Kokoro.Graphics.PixelInternalFormat iFormat)
        {
            long hndl = 0;
            if (layer < 0)
                hndl = GL.Arb.GetImageHandle(id, level, true, 0, (OpenTK.Graphics.OpenGL4.PixelFormat)iFormat);
            else
                hndl = GL.Arb.GetImageHandle(id, level, false, layer, (OpenTK.Graphics.OpenGL4.PixelFormat)iFormat);
            return new ImageHandle(hndl, this);
        }

        public virtual void SetData(ITextureSource src, int level, CubeMapFace face)
        {
            bool inited = false;
            if (id == 0)
            {
                GL.CreateTextures(OpenTK.Graphics.OpenGL4.TextureTarget.TextureCubeMap, 1, out id);
                inited = true;

                this.Width = src.GetWidth();
                this.Height = src.GetHeight();
                this.Depth = src.GetDepth();
                this.LevelCount = src.GetLevels();

                this.format = src.GetFormat();
                this.internalformat = src.GetInternalFormat();
                this.texTarget = src.GetTextureTarget();
                this.ptype = src.GetPixelType();
            }
            if (inited) GL.TextureStorage2D(id, LevelCount, (SizedInternalFormat)internalformat, Width, Height);
            IntPtr ptr = src.GetPixelData(level);
            if (ptr != IntPtr.Zero)
                switch (internalformat)
                {
                    case PixelInternalFormat.CompressedRedRgtc1:    //BC4
                    case PixelInternalFormat.CompressedRgRgtc2:     //BC5
                    case PixelInternalFormat.CompressedRgbaBptcUnorm:    //BC7
                        {
                            int blockSize = (internalformat == PixelInternalFormat.CompressedRedRgtc1) ? 8 : 16;
                            int size = ((src.GetWidth() >> level + 3) / 4) * ((src.GetHeight() >> level + 3) / 4) * blockSize;
                            GL.CompressedTextureSubImage3D(id, level, src.GetBaseWidth() >> level, src.GetBaseHeight() >> level, (int)face, src.GetWidth() >> level, src.GetHeight() >> level, 1, (OpenTK.Graphics.OpenGL4.PixelFormat)src.GetFormat(), size, ptr);
                        }
                        break;
                    default:
                        GL.TextureSubImage3D(id, level, src.GetBaseWidth() >> level, src.GetBaseHeight() >> level, (int)face, src.GetWidth() >> level, src.GetHeight() >> level, 1, (OpenTK.Graphics.OpenGL4.PixelFormat)src.GetFormat(), (OpenTK.Graphics.OpenGL4.PixelType)src.GetPixelType(), ptr);
                        break;
                }

            if (GenerateMipmaps)
                GL.GenerateTextureMipmap(id);
        }

        public virtual void SetData(ITextureSource src, int level)
        {
            bool inited = false;
            if (id == 0)
            {
                GL.CreateTextures((OpenTK.Graphics.OpenGL4.TextureTarget)src.GetTextureTarget(), 1, out id);
                inited = true;

                this.Width = src.GetWidth();
                this.Height = src.GetHeight();
                this.Depth = src.GetDepth();
                this.LevelCount = src.GetLevels();

                this.format = src.GetFormat();
                this.internalformat = src.GetInternalFormat();
                this.texTarget = src.GetTextureTarget();
                this.ptype = src.GetPixelType();
            }

            switch (src.GetDimensions())
            {
                case 1:
                    if (inited) GL.TextureStorage1D(id, LevelCount, (SizedInternalFormat)internalformat, Width);
                    break;
                case 2:
                    if (inited) GL.TextureStorage2D(id, LevelCount, (SizedInternalFormat)internalformat, Width, Height);
                    break;
                case 3:
                    if (inited) GL.TextureStorage3D(id, LevelCount, (SizedInternalFormat)internalformat, Width, Height, Depth);
                    break;
            }

            if (GenerateMipmaps)
            {
                GL.GenerateTextureMipmap(id);
            }
        }

        public static explicit operator IntPtr(Texture t)
        {
            return (IntPtr)t.id;
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

                if (id != 0) GraphicsDevice.QueueForDeletion(id, GLObjectType.Texture);
                id = 0;

                disposedValue = true;
            }
        }

        ~Texture()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion


    }
}