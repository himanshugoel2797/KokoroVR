﻿using Kokoro.Graphics;
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

                Random rng = new Random(0);
                ulong cnt = 0;
                Perlin p = new Perlin();
                for (int x = ChunkConstants.Side * -40; x < ChunkConstants.Side * 40; x++)
                    //for (int y = ChunkConstants.Side * 0; y < ChunkConstants.Side * 1; y++)
                    for (int z = ChunkConstants.Side * -40; z < ChunkConstants.Side * 40; z++)
                    {
                        //if (x * x + y * y + z * z <= 200 * 200)
                        //if(y > -32 && y < 32)
                        {
                            int y = (int)((p.GetValue(x * 0.001f, z * 0.001f, 0) * 0.5f + 0.5f) * 250);
                            //if (y >= 0)
                            for (int y0 = y; y0 >= 0; y0--)
                                obj.Set(x, y0, z, mat_id);
                            //else
                            //if(rng.NextDouble() > 0.5f)
                            //    obj.Set(x, y, z, mat_id);
                            cnt++;
                        }
                    }
                obj.RebuildAll();

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
