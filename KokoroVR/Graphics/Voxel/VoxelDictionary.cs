using Kokoro.Graphics;
using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Graphics.Voxel
{
    public class VoxelDictionary
    {
        struct voxel_tex
        {
            public Vector3 color;
            public Vector3 specular;
            public float roughness;
        }

        byte offset = 1;
        voxel_tex[] set;

        internal ShaderStorageBuffer voxelData;

        public VoxelDictionary()
        {
            voxelData = new ShaderStorageBuffer(ChunkConstants.DictionaryLen * 32, false);
            set = new voxel_tex[ChunkConstants.DictionaryLen];
        }

        public byte Register(Vector3 Color, Vector3 Specular, float Roughness)
        {
            set[offset].color = Color;
            set[offset].specular = Specular;
            set[offset].roughness = Roughness;

            unsafe
            {
                float* f_p = (float*)voxelData.Update();
                f_p[offset * 8 + 0] = Color.X;
                f_p[offset * 8 + 1] = Color.Y;
                f_p[offset * 8 + 2] = Color.Z;
                f_p[offset * 8 + 3] = Roughness;
                f_p[offset * 8 + 4] = Specular.X;
                f_p[offset * 8 + 5] = Specular.Y;
                f_p[offset * 8 + 6] = Specular.Z;
                voxelData.UpdateDone();
            }

            return offset++;
        }
    }
}
