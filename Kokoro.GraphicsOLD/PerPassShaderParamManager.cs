using Kokoro.Math;
using System;
using System.Collections.Generic;

namespace Kokoro.Graphics
{
    public class PerPassShaderParams
    {
        public Vector3 EyePos { get; set; }
        public Vector3 EyeDir { get; set; }
        public Matrix4 Projection { get; set; }
        public Matrix4 View { get; set; }
        public int Layer { get; set; }
        public List<BufferView> TexelBuffers { get; private set; }
        public List<TextureBinding> Textures { get; private set; }

        public PerPassShaderParams()
        {
            TexelBuffers = new List<BufferView>();
            Textures = new List<TextureBinding>();
        }
    }

    public class PerPassShaderParamManager
    {
        PerPassShaderParams[] PerPassShaderParams;

        public PerPassShaderParamManager(int maxPasses)
        {
            PerPassShaderParams = new PerPassShaderParams[maxPasses];
        }

        public int Allocate()
        {
            for (int i = 0; i < PerPassShaderParams.Length; i++)
                if (PerPassShaderParams[i] == null)
                    return i;
            throw new Exception("Maximum pass count exceeded!");
        }

        public void Free(int id)
        {
            PerPassShaderParams[id] = null;
        }

        public void Set(int id, PerPassShaderParams val)
        {
            PerPassShaderParams[id] = val;
        }
    }
}
