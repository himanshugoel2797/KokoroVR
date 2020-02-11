using Kokoro.Graphics;
using Kokoro.Math;
using KokoroVR.Graphics;
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
                LightManager.AddLight(new Graphics.Lights.PointLight()
                {
                    Color = Vector3.One,
                    Intensity = 50.0f,
                    //Direction = new Vector3(0.577f, 0.577f, 0.577f)
                    Position = Vector3.UnitY * 110// + Vector3.UnitX * 50
                });

                var green = new Vector3(0x7e, 0xc8, 0x50) / 255.0f;
                ChunkStreamer chunkStreamer = new ChunkStreamer(1 << 16, this.Renderer);
                var mat_id = chunkStreamer.MaterialMap.Register(green, green * 0.25f, 0.9f);
                ChunkObject obj = new ChunkObject(chunkStreamer);

                //Engine.CurrentPlayer.Position += Vector3.UnitY * 110;

                Random rng = new Random(0);
                ulong cnt = 0;
                Perlin p = new Perlin();
                for (int x = ChunkConstants.Side * -20; x < ChunkConstants.Side * 20; x++)
                    //for (int y = ChunkConstants.Side * -1; y < ChunkConstants.Side * 1; y++)
                    for (int z = ChunkConstants.Side * -20; z < ChunkConstants.Side * 20; z++)
                    {
                        //if (x * x + y * y + z * z <= 200 * 200)
                        //if(y > -32 && y < 32)
                        {
                            int y = (int)((p.GetValue(x * 0.0005f, z * 0.0005f, 0) * 0.5f + 0.5f) * 250) - 64;
                            if (y >= 0)
                                for (int y0 = y; y0 >= 0; y0--)
                                    obj.Set(x, y0, z, mat_id);
                            //else
                            //if (rng.NextDouble() > 0.5f)
                            //    obj.Set(x, y, z, mat_id);
                            cnt++;
                        }
                    }
                obj.RebuildAll();

                Console.WriteLine(cnt);


                //AddRenderable(new StaticRenderable(Kokoro.Graphics.Prefabs.SphereFactory.Create(grp)));
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
