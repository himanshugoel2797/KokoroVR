using Kokoro.Math;
using System;

namespace Kokoro.Graphics
{
    public class PerObjectShaderParams
    {
        public Vector3 Albedo { get; set; }
        public float Roughness { get; set; }
        public Vector3 Specular { get; set; }
        public Vector3 Position { get; set; }
        public Vector4 Orientation { get; set; }
    }

    public class PerObjectShaderParamManager
    {
        PerObjectShaderParams[] PerObjectShaderParams;

        public PerObjectShaderParamManager(int maxObjs)
        {
            PerObjectShaderParams = new PerObjectShaderParams[maxObjs];
        }

        public int Allocate()
        {
            for (int i = 0; i < PerObjectShaderParams.Length; i++)
                if (PerObjectShaderParams[i] == null)
                    return i;
            throw new Exception("Maximum object count exceeded!");
        }

        public void Free(int id)
        {
            PerObjectShaderParams[id] = null;
        }

        public void Set(int id, PerObjectShaderParams val)
        {
            PerObjectShaderParams[id] = val;
        }
    }
}
