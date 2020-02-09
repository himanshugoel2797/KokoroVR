using System;
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
        internal TextureView parent;

        internal ImageHandle(long hndl, TextureView parent)
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
        internal TextureView parent;
        private bool isResident = false;

        public TextureSampler Sampler { get; private set; }

        internal TextureHandle(long hndl, TextureView parent, TextureSampler sampler)
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
        public static float MaxAnisotropy { get; internal set; }

        static Texture()
        {
            GL.GetFloat((GetPName)OpenTK.Graphics.OpenGL.ExtTextureFilterAnisotropic.MaxTextureMaxAnisotropyExt, out float a);
            MaxAnisotropy = a;

            Default = new Texture();
        }

        private int id;

        public int Width { get; set; }
        public int Height { get; set; }
        public int Depth { get; set; }
        public int LevelCount { get; set; }
        public int LayerCount { get; set; }
        public PixelInternalFormat Format { get; set; }
        public bool GenerateMipmaps { get; set; }
        public TextureTarget Target { get; set; }

        public Texture()
        {
            id = 0;
            GraphicsDevice.Cleanup.Add(Dispose);
        }

        public virtual void Build()
        {
            if (id == 0)
            {
                GL.CreateTextures((OpenTK.Graphics.OpenGL4.TextureTarget)Target, 1, out id);

                switch (Target)
                {
                    case TextureTarget.Texture1D:
                        GL.TextureStorage1D(id, LevelCount, (SizedInternalFormat)Format, Width);
                        break;
                    case TextureTarget.Texture1DArray:
                    case TextureTarget.Texture2D:
                    case TextureTarget.TextureCubeMap:
                        GL.TextureStorage2D(id, LevelCount, (SizedInternalFormat)Format, Width, Height);
                        break;
                    case TextureTarget.Texture2DArray:
                    case TextureTarget.Texture3D:
                    case TextureTarget.TextureCubeMapArray:
                        GL.TextureStorage3D(id, LevelCount, (SizedInternalFormat)Format, Width, Height, Depth);
                        break;
                }
            }
        }

        public static explicit operator int(Texture t)
        {
            return t.id;
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
