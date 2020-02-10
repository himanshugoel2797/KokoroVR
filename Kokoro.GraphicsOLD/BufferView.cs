using System;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public class BufferView : IDisposable
    {
        public PixelInternalFormat Format { get; set; }

        private bool locked;
        private int id;
        private ImageHandle imgHandle;
        private TextureHandle texHandle;

        public BufferView()
        {
            locked = false;
        }

        public void Build(StorageBuffer buf)
        {
            if (!locked)
            {
                GL.CreateTextures(OpenTK.Graphics.OpenGL4.TextureTarget.TextureBuffer, 1, out id);
                GL.TextureBuffer(id, (SizedInternalFormat)Format, (GPUBuffer)buf);
                locked = true;
            }
        }

        public ImageHandle GetImageHandle()
        {
            if (imgHandle == null)
                imgHandle = new ImageHandle(GL.Arb.GetImageHandle(id, 0, false, 0, (OpenTK.Graphics.OpenGL4.PixelFormat)Format), this);
            return imgHandle;
        }

        public TextureHandle GetTextureHandle()
        {
            if (texHandle == null)
                texHandle = new TextureHandle(GL.Arb.GetTextureHandle(id), this);
            return texHandle;
        }

        public static explicit operator int(BufferView v)
        {
            return v.id;
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

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~BufferView()
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
