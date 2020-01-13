using Kokoro.Graphics;
using Kokoro.Math;
using KokoroVR.Graphics.Voxel;
using SharpNoise.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Test
{
    public class VoxelWorld : World
    {
        public VoxelWorld(string name, int maxLights) : base(name, maxLights)
        {
            Initializer = () =>
            {
                MeshGroup grp = new MeshGroup(MeshGroupVertexFormat.X32F_Y32F_Z32F, 40000, 40000);

                LightManager.AddLight(new Graphics.Lights.DirectionalLight()
                {
                    Color = Vector3.UnitZ,
                    Intensity = 640.0f,
                    Direction = new Vector3(0, 0, 1)
                });

                ChunkStreamer chunkStreamer = new ChunkStreamer(1 << 16);
                var mat_id = chunkStreamer.MaterialMap.Register(Vector3.One, Vector3.One * 0.5f, 1f);
                ChunkObject obj = new ChunkObject(chunkStreamer);

                Engine.CurrentPlayer.Position = Vector3.UnitX * -150;

                Random rng = new Random(0);
                ulong cnt = 0;
                Perlin p = new Perlin();
                for (int x = ChunkConstants.Side * -30; x < ChunkConstants.Side * 30; x++)
                    for (int y = ChunkConstants.Side * -1; y < /*ChunkConstants.Side **/ 1; y++)
                        for (int z = ChunkConstants.Side * -30; z < ChunkConstants.Side * 30; z++)
                        {
                            //if (x * x + y * y + z * z <= 100 * 100)
                            //if(y > -32 && y < 32)
                            {
                                if (y >= 0)
                                    obj.Set(x, (int)((p.GetValue(x * 0.001f, z * 0.001f, 0) * 0.5f + 0.5f) * 250) + y, z, mat_id);
                                else
                                    obj.Set(x, y, z, mat_id);
                                cnt++;
                            }
                        }

                Console.WriteLine(cnt);

                //w.AddRenderable(new StaticRenderable(Kokoro.Graphics.Prefabs.SphereFactory.Create(grp)));
                AddRenderable(chunkStreamer);
                AddRenderable(obj);

                AddRenderable(chunkStreamer.Ender);
                //w.AddInterpreter(new Input.DefaultControlInterpreter("/actions/vrworld/in/hand_left", "/actions/vrworld/in/hand_right", grp));
            };
        }

        public override void Render(double time)
        {
            base.Render(time);
        }

        public override void Update(double time)
        {
            base.Update(time);
        }
    }
}
