using System.Diagnostics;
using System;
using KokoroVR2.Graphics.Voxel;
using Kokoro.Math;
using System.IO;

namespace SpaceGamePrototype
{
    class Program
    {
        static VoxelData vox;

        static void Main(string[] args)
        {
            vox = new VoxelData(Vector3.Zero);

            int runs = 100000;

            double netTime = 0;
            double inds_cnt = 0;
            Random rng = new Random(0);

            string time_samples = "";

            unsafe
            {
                for (int samples = 0; samples < runs; samples++)
                {
                    for (int i = 0; i < 32; i++)
                        for (int y = 0; y < 32; y++)
                            vox.VisibilityMasks[y * 32 + i] = (uint)(0x55555555 << ((i + y % 2) % 2)); //(uint)rng.Next(); //(uint)(0x55555555 << ((i + y % 2) % 2));
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    VoxelMesher.MeshChunk(ref vox, out var inds_pos);
                    stopwatch.Stop();

                    time_samples += $"{samples},{stopwatch.Elapsed.TotalMilliseconds}\n";

                    netTime += stopwatch.Elapsed.TotalMilliseconds;
                    inds_cnt += inds_pos;
                }
            }

            File.WriteAllText("samples.csv", time_samples);

            Console.WriteLine($"Net Time: {netTime / runs}ms, {inds_cnt / runs}");
        }
    }
}
