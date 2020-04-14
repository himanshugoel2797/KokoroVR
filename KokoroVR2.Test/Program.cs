using Kokoro.Graphics;
using Kokoro.Math;
using KokoroVR2.Graphics;
using Kokoro.Graphics.Framegraph;
//using KokoroVR2.Graphics.Voxel;
using System;

namespace KokoroVR2.Test
{
    class Program
    {
        //static VoxelDictionary dictionary;
        //static ChunkStreamer streamer;
        //static ChunkObject obj;

        static void Main(string[] args)
        {
            Engine.AppName = "Test";
            Engine.EnableValidation = true;
            Engine.Initialize();

            //dictionary = new VoxelDictionary();
            //var mat_id = dictionary.Register(Vector3.One, Vector3.One, 0, 0);

            var rng = new Random(0);
            //streamer = new ChunkStreamer(1 << 16, Engine.DeferredRenderer);
            //obj = new ChunkObject(streamer);

            //for (int x = -ChunkConstants.Side * 5; x < ChunkConstants.Side * 5; x++)
            //    for (int y = -ChunkConstants.Side; y < ChunkConstants.Side; y++)
            //        for (int z = -ChunkConstants.Side * 5; z < ChunkConstants.Side * 5; z++)
                        //if (rng.NextDouble() > 0.5f)
            //                obj.Set(x, y, z, mat_id);
            //obj.RebuildAll();

            Engine.OnRebuildGraph += Engine_OnRebuildGraph;
            Engine.OnUpdate += Engine_OnUpdate;
            Engine.Start(0);
        }

        private static void Engine_OnUpdate(double time_ms, double delta_ms)
        {
            //dictionary.Update();
            //streamer.InitialUpdate(delta_ms);
            //obj.Render(delta_ms);
            //streamer.FinalUpdate(delta_ms);
        }

        private static void Engine_OnRebuildGraph(double time_ms, double delta_ms)
        {
            //dictionary.GenerateRenderGraph();
            //streamer.GenerateRenderGraph();
        }
    }
}
