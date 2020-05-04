using System.Diagnostics;
using System;
using KokoroVR2.Graphics.Voxel;
using Kokoro.Math;

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

            unsafe
            {
                for (int samples = 0; samples < runs; samples++)
                {
                    for (int i = 0; i < 32; i++)
                        for (int y = 0; y < 32; y++)
                            vox.VisibilityMasks[y * 32 + i] = (ulong)(0x5555555555555555 << ((i + y % 2) % 2));  //((ulong)rng.Next() << 32) | (ulong)rng.Next() ;
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    VoxelMesher.MeshChunk(ref vox, out var inds_pos);
                    netTime += stopwatch.Elapsed.TotalMilliseconds;
                    inds_cnt += inds_pos;
                }
            }
            Console.WriteLine($"Net Time: {netTime / runs}ms, {inds_cnt / runs}");
        }
    }
}
