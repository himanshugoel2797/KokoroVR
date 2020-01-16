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
        class StaticRenderable : Interactable
        {
            private Mesh mesh;
            private TextureHandle def_handle;
            private Vector3 rots;
            public StaticRenderable(Mesh m)
            {
                mesh = m;
                def_handle = Texture.Default.GetHandle(TextureSampler.Default);
                def_handle.SetResidency(Residency.Resident);
            }

            public override void Render(double time, Framebuffer fbuf, StaticMeshRenderer staticMesh, DynamicMeshRenderer dynamicMesh, VREye eye)
            {
                staticMesh.DrawC(mesh, Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rots.X)) * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rots.Y)) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rots.Z)) * Matrix4.CreateTranslation(Vector3.UnitX * 2), def_handle);
            }

            public override void Update(double time, World parent)
            {
                rots.X += 0.01f;
                rots.Y += 0.01f;
                rots.Z += 0.01f;
            }
        }

        static void Main(string[] args)
        {
            Engine.Initialize(ExperienceKind.Standing);
            Engine.LogMetrics = false;
            Engine.LogAMDMetrics = true;
            //For ray tracing, store 32x32x32 cubemaps with direct 
            var w = new VoxelWorld("TestWorld", 10);
            Engine.AddWorld(w);
            Engine.SetActiveWorld("TestWorld");
            Engine.Start();
        }
    }
}
