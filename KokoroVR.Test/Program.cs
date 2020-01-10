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
            Engine.LogMetrics = !false;

            var w = new World("TestWorld", 10);
            w.Initializer = () =>
            {
                MeshGroup grp = new MeshGroup(MeshGroupVertexFormat.X32F_Y32F_Z32F, 40000, 40000);

                float m_off = 40;
                float m_off_h = m_off * 0.5f;

                w.LightManager.AddLight(new Graphics.Lights.PointLight()
                {
                    Color = Vector3.UnitX,
                    Intensity = 64.0f,
                    Position = Vector3.UnitX * -m_off + Vector3.UnitZ * m_off_h + Vector3.UnitY * m_off_h
                });

                w.LightManager.AddLight(new Graphics.Lights.PointLight()
                {
                    Color = Vector3.UnitY,
                    Intensity = 64.0f,
                    Position = Vector3.UnitY * -m_off + Vector3.UnitX * m_off_h + Vector3.UnitZ * m_off_h
                });

                w.LightManager.AddLight(new Graphics.Lights.PointLight()
                {
                    Color = Vector3.UnitZ,
                    Intensity = 64.0f,
                    Position = Vector3.UnitZ * -m_off + Vector3.UnitX * m_off_h + Vector3.UnitY * m_off_h
                });

                w.LightManager.AddLight(new Graphics.Lights.DirectionalLight()
                {
                    Color = Vector3.UnitZ,
                    Intensity = 64.0f,
                    Direction = new Vector3(0, -1, 0)
                });

                ChunkStreamer chunkStreamer = new ChunkStreamer(1024);
                var mat_id = chunkStreamer.MaterialMap.Register(Vector3.One, Vector3.One * 0.5f, 1f);
                ChunkObject obj = new ChunkObject(chunkStreamer);

                Random rng = new Random(0);
                var updates = new List<(int, int, int, byte)>();
                for (int x = ChunkConstants.Side * -10; x < ChunkConstants.Side * 10; x ++)
                    for (int y = 0; y < ChunkConstants.Side; y ++)
                        for (int z = ChunkConstants.Side * -10; z < ChunkConstants.Side * 10; z ++)
                        {
                            //if (x == 11 && y == 0 && z == 11) continue;
                            //if (x == 12 && y == 0 && z == 12) continue;
                            if (rng.NextDouble() > 0.5f) updates.Add((y, z, x, mat_id));
                        }

                Console.WriteLine(updates.Count);
                obj.BulkSet(updates.ToArray());

                //w.AddRenderable(new StaticRenderable(Kokoro.Graphics.Prefabs.SphereFactory.Create(grp)));
                w.AddRenderable(chunkStreamer);
                w.AddRenderable(obj);

                w.AddRenderable(chunkStreamer.Ender);
                //w.AddInterpreter(new Input.DefaultControlInterpreter("/actions/vrworld/in/hand_left", "/actions/vrworld/in/hand_right", grp));
            };
            Engine.AddWorld(w);
            Engine.SetActiveWorld("TestWorld");
            Engine.Start();
        }
    }
}
