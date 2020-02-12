using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Graphics.Voxel
{
    public class VoxelGI
    {
        //List of all visible voxels (and normals) which are emissive
        //Trace each photon to intersection, update stored color at location
        //Use russian roulette to decide if a new ray should be emitted, for now only do diffuse reflections

        List<Vector4> emissionSources;
    }
}
