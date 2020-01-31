﻿using System;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public class TextureView : IDisposable
    {
        public PixelInternalFormat Format { get => format; set => format = locked ? format : value; }
        public TextureTarget Target { get => target; set => target = locked ? target : value; }
        public int BaseLevel { get => baseLevel; set => baseLevel = locked ? baseLevel : value; }
        public int BaseLayer { get => baseLayer; set => baseLayer = locked ? baseLayer : value; }
        public int LevelCount { get => levelCount; set => levelCount = locked ? levelCount : value; }
        public int LayerCount { get => layerCount; set => layerCount = locked ? layerCount : value; }

        private int id;
        private bool locked;
        public TextureView()
        {
            locked = false;
        }

        public void CreateView(Texture t)
        {
            if (!locked)
            {
                GL.CreateTextures((OpenTK.Graphics.OpenGL4.TextureTarget)Target, 1, out id);
                GL.TextureView(id, (OpenTK.Graphics.OpenGL4.TextureTarget)Target, (int)t, (OpenTK.Graphics.OpenGL4.PixelInternalFormat)Format, BaseLevel, LevelCount, BaseLayer, LayerCount);

                locked = true;
            }
            else
                throw new Exception("View is already locked.");
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        private PixelInternalFormat format;
        private TextureTarget target;
        private int baseLevel;
        private int baseLayer;
        private int levelCount;
        private int layerCount;

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
        // ~TextureView()
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