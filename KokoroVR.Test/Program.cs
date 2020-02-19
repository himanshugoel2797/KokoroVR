using Kokoro.Graphics;
using Kokoro.Math;
using KokoroVR.Graphics;
using KokoroVR.Graphics.Voxel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Test
{
    class Program
    {


        static void Main()
        {
            Engine.Initialize(ExperienceKind.Standing);
            Engine.LogMetrics = false;
            Engine.LogAMDMetrics = false;
            //For ray tracing, store 32x32x32 cubemaps with direct 
            var w = new VoxelWorld("TestWorld", 10);
            Engine.AddWorld(w);
            Engine.SetActiveWorld("TestWorld");
            Engine.Start();
        }
    }
}
