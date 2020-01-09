using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public class BufferTexture : IDisposable, IMappedBuffer
    {
        private ShaderStorageBuffer ssbo;
        private Texture tex;

        public ShaderStorageBuffer Buffer { get => ssbo; }
        public TextureHandle Texture { get; private set; }
        public ImageHandle Image { get; private set; }

        public long Size => ((IMappedBuffer)this.ssbo).Size;

        public long Offset => ((IMappedBuffer)this.ssbo).Offset;

        public BufferTexture(long sz, PixelInternalFormat iFmt, bool stream)
        {
            ssbo = new ShaderStorageBuffer(sz, stream);
            tex = new Texture();
            tex.SetData(new BufferTextureSource(ssbo)
            {
                InternalFormat = iFmt
            }, 0);

            Texture = tex.GetHandle(TextureSampler.Default);
            Image = tex.GetImageHandle(0, 0, iFmt);

            Texture.SetResidency(Residency.Resident);
            Image.SetResidency(Residency.Resident, AccessMode.ReadWrite);
        }

        public unsafe byte* Update()
        {
            return ((IMappedBuffer)this.ssbo).Update();
        }

        public void UpdateDone()
        {
            ((IMappedBuffer)this.ssbo).UpdateDone();
        }

        public void UpdateDone(long off, long usize)
        {
            ((IMappedBuffer)this.ssbo).UpdateDone(off, usize);
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
                    tex.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~BufferTexture()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
