using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Bmi = System.Runtime.Intrinsics.X86.Bmi1.X64;
using Pop = System.Runtime.Intrinsics.X86.Popcnt.X64;

namespace KokoroVR2.Graphics.Voxel
{
    public class VoxelMesher
    {
        static IntPtr indexCache = IntPtr.Zero;

        public VoxelMesher()
        {
            //indexCache = Marshal.AllocHGlobal(VoxelConstants.ChunkSideWithNeighbors * 2 * VoxelConstants.ChunkSide * VoxelConstants.ChunkSide * sizeof(ushort));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void MeshChunk(ref VoxelData vox, out int inds_pos)
        {
            unsafe
            {
                inds_pos = 0;
                ulong* visMask = vox.VisibilityMasks;
                
                var top_col_p = visMask;
                var cur_col_p = visMask + VoxelConstants.ChunkSideWithNeighbors;
                var btm_col_p = visMask + VoxelConstants.ChunkSideWithNeighbors * 2;

                for (uint y = 0; y < VoxelConstants.ChunkSide << 11; y += 1 << 11)
                {
                    var left_col = *cur_col_p++;
                    var cur_col = *cur_col_p++;

                    top_col_p++;
                    btm_col_p++;
                    for (uint x = 0; x < VoxelConstants.ChunkSide << 6; x += 1 << 6)
                    {
                        var top_col = *top_col_p++;
                        var btm_col = *btm_col_p++;
                        var right_col = *cur_col_p++;

                        var any_vis = Bmi.AndNot(left_col & right_col & top_col & btm_col & (cur_col << 1) & (cur_col >> 1), cur_col);
                        var xy = y | x;

                        while (any_vis != 0)
                        {
                            var fidx = Bmi.TrailingZeroCount(any_vis);
                            var idx = xy | fidx;
                            vox.MaterialData[idx] = (ushort)((vox.MaterialData[idx] & 0xff00) | 1);
                            any_vis = Bmi.ResetLowestSetBit(any_vis);
                        }

                        left_col = cur_col;
                        cur_col = right_col;
                    }
                    top_col_p++;
                    btm_col_p++;
                }
            }
        }
    }
}
