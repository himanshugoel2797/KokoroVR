using System.Diagnostics;
using System;
using KokoroVR2.Graphics.Voxel;
using Kokoro.Math;
using System.IO;
using System.Runtime.Intrinsics.X86;

namespace SpaceGamePrototype
{
    class Program
    {
        static VoxelData vox;

        static void Main(string[] args)
        {
            vox = new VoxelData(Vector3.Zero);

            int runs = 10000;

            double netTime = 0;
            double inds_cnt = 0;
            double req_cnt = 0;
            Random rng = new Random(0);

            string time_samples = "";

            /*unsafe
            {
                int iter_cnt = 10000;
                var iter_table = new byte[iter_cnt][];
                var iter_table_u = new uint[iter_cnt];
                for (int iters = 0; iters < iter_cnt; iters++)
                {
                    iter_table[iters] = new byte[3];
                    rng.NextBytes(iter_table[iters]);
                    iter_table_u[iters] = MortonCoder.Encode(iter_table[iters][0], iter_table[iters][1], iter_table[iters][2]);
                }
                for (int samples = 0; samples < runs; samples++)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();

                    for (int iters = 0; iters < iter_cnt; iters++)
                    {
                        var res = MortonCoder.Encode(iter_table[iters][0], iter_table[iters][1], iter_table[iters][2]);
                        MortonCoder.Decode(res, out iter_table[iters][0], out iter_table[iters][1], out iter_table[iters][2]);
                    }

                    stopwatch.Stop();

                    netTime += (stopwatch.Elapsed.TotalMilliseconds);
                    Console.WriteLine($"[Sample # {samples}]");
                }
            }*/


            unsafe
            {
                for (int samples = 0; samples < runs; samples++)
                {
                    for (int i = 0; i < 32; i++)
                        for (int y = 0; y < 32; y++)
                        {
                            vox.VisibilityMasks[y * 32 + i] = (ulong)(0x5555555555555555 << ((i + y % 2) % 2)); //(ulong)rng.Next() | (ulong)(rng.Next() << 32);

                            if (i > 0 && i < 31)
                                if (y > 0 && y < 31)
                                    req_cnt += Popcnt.X64.PopCount(vox.VisibilityMasks[y * 32 + i]);// & ~0x8000000000000001);
                        }
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    VoxelMesher.MeshChunk(ref vox, out var inds_pos);
                    stopwatch.Stop();

                    time_samples += $"{samples},{stopwatch.Elapsed.TotalMilliseconds}\n";

                    netTime += stopwatch.Elapsed.TotalMilliseconds;
                    inds_cnt += inds_pos;
                }
            }

            File.WriteAllText("samples.csv", time_samples);

            Console.WriteLine($"Net Time: {netTime / runs}ms, {inds_cnt / runs}, {req_cnt / runs}");
        }
    }
}
