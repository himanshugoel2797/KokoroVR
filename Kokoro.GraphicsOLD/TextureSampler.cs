using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace Kokoro.Graphics
{
    public enum TileMode
    {
        Repeat,
        MirroredRepeat,
        ClampToBorder,
        ClampToEdge,
        MirrorClampToEdge
    }

    public class TextureSampler : IDisposable
    {
        public static TextureSampler Default { get; private set; } = new TextureSampler(0);

        private int id;
        private int _maxReadLevel, _baseReadLevel;
        private bool locked = false;

        public int MinReadLevel
        {
            get { return _baseReadLevel; }
            set
            {
                if (locked && _baseReadLevel != value) throw new Exception("Sampler state has been locked due to use with GetHandle.");
                if (_baseReadLevel != value) { _baseReadLevel = value; GL.SamplerParameter(id, (SamplerParameterName)All.TextureBaseLevel, (float)_baseReadLevel); }
            }
        }

        public int MaxReadLevel
        {
            get { return _maxReadLevel; }
            set
            {

                if (locked && _maxReadLevel != value) throw new Exception("Sampler state has been locked due to use with GetHandle.");
                if (_maxReadLevel != value) { _maxReadLevel = value; GL.SamplerParameter(id, (SamplerParameterName)All.TextureMaxLevel, (float)_maxReadLevel); }
            }
        }

        public TextureSampler()
        {
            GL.CreateSamplers(1, out id);

            GraphicsDevice.Cleanup.Add(Dispose);
        }

        internal TextureSampler(int id)
        {
            this.id = id;
        }

        internal long GetHandle(int tex)
        {
            locked = true;
            return GL.Arb.GetTextureSamplerHandle(tex, id);
        }

        public static explicit operator int(TextureSampler s)
        {
            s.locked = true;
            return s.id;
        }

        private TextureWrapMode conv(TileMode t)
        {
            return t switch
            {
                TileMode.ClampToBorder => TextureWrapMode.ClampToBorder,
                TileMode.ClampToEdge => TextureWrapMode.ClampToEdge,
                TileMode.MirrorClampToEdge => (TextureWrapMode)All.MirrorClampToEdge,
                TileMode.MirroredRepeat => TextureWrapMode.MirroredRepeat,
                TileMode.Repeat => TextureWrapMode.Repeat,
                _ => throw new Exception("Unknown TileMode")
            };
        }

        public void SetTileMode(TileMode tileX)
        {
            GL.SamplerParameter(id, SamplerParameterName.TextureWrapS, (int)conv(tileX));
            GL.SamplerParameter(id, SamplerParameterName.TextureWrapT, (int)conv(tileX));
            GL.SamplerParameter(id, SamplerParameterName.TextureWrapR, (int)conv(tileX));
        }

        public void SetTileMode(TileMode tileX, TileMode tileY)
        {
            GL.SamplerParameter(id, SamplerParameterName.TextureWrapS, (int)conv(tileX));
            GL.SamplerParameter(id, SamplerParameterName.TextureWrapT, (int)conv(tileY));
        }

        public void SetTileMode(TileMode tileX, TileMode tileY, TileMode tileZ)
        {
            GL.SamplerParameter(id, SamplerParameterName.TextureWrapS, (int)conv(tileX));
            GL.SamplerParameter(id, SamplerParameterName.TextureWrapT, (int)conv(tileY));
            GL.SamplerParameter(id, SamplerParameterName.TextureWrapR, (int)conv(tileZ));
        }

        public void SetEnableLinearFilter(bool linear, bool useMipmaps, bool mipmapLinear)
        {
            var mag = TextureMagFilter.Nearest;
            var min = TextureMinFilter.Nearest;
            if (linear)
            {
                mag = TextureMagFilter.Linear;
                if (useMipmaps)
                {
                    if (mipmapLinear)
                    {
                        min = (TextureMinFilter)All.LinearMipmapLinear;
                    }
                    else
                    {
                        min = (TextureMinFilter)All.LinearMipmapNearest;
                    }
                }
                else
                {
                    min = TextureMinFilter.Linear;
                }
            }
            else
            {
                mag = TextureMagFilter.Nearest;
                if (useMipmaps)
                {
                    if (mipmapLinear)
                    {
                        min = (TextureMinFilter)All.NearestMipmapLinear;
                    }
                    else
                    {
                        min = (TextureMinFilter)All.NearestMipmapNearest;
                    }
                }
                else
                {
                    min = TextureMinFilter.Nearest;
                }
            }

            GL.SamplerParameter(id, SamplerParameterName.TextureMagFilter, (int)mag);
            GL.SamplerParameter(id, SamplerParameterName.TextureMinFilter, (int)min);
        }

        public void SetAnisotropicFilter(float taps)
        {
            GL.SamplerParameter(id, (SamplerParameterName)All.TextureMaxAnisotropyExt, taps);
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

                if (id != 0) GraphicsDevice.QueueForDeletion(id, GLObjectType.Sampler);
                id = 0;

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~TextureSampler()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
