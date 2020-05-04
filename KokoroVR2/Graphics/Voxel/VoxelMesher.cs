using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using Bmi = System.Runtime.Intrinsics.X86.Bmi1.X64;

namespace KokoroVR2.Graphics.Voxel
{
    public class VoxelMesher
    {
        //[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static unsafe uint buildFace(ref VoxelData vox, ulong xy, ulong z, uint face)
        {
            var idx = xy | z;
            var mat = (ulong)vox.MaterialData[idx];//vox.GetMaterialData(vox, x, y, z);
            return (uint)(face | (mat << 16) | idx);
        }

        const uint backFace = 0;
        const uint frontFace = 1 << 24;
        const uint topFace = 2 << 24;
        const uint btmFace = 3 << 24;
        const uint leftFace = 4 << 24;
        const uint rightFace = 5 << 24;

        //[MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void MeshChunk(ref VoxelData vox, out int inds_pos)
        {
            unsafe
            {
                inds_pos = 0;
                uint* inds_p_base = vox.IndexCache;
                ulong* visMask = vox.VisibilityMasks;
                {
                    uint* inds_p = inds_p_base;

                    var cur_col_p = visMask + VoxelConstants.ChunkSideWithNeighbors;
                    var top_col_p = visMask;
                    var btm_col_p = visMask + VoxelConstants.ChunkSideWithNeighbors * 2;

                    for (ulong y = 1; y < VoxelConstants.ChunkSideWithNeighbors - 1; y++)
                    {
                        var left_col = *cur_col_p++;//vox.VisibilityMasks[VoxelData.GetVisibilityIndex(0, y, 0)];
                        var cur_col = *cur_col_p++;// vox.VisibilityMasks[VoxelData.GetVisibilityIndex(1, y, 0)];
                        var cur_col_orig = cur_col;

                        for (ulong x = 1; x < VoxelConstants.ChunkSideWithNeighbors - 1; x++)
                        {
                            var top_col = top_col_p[x];//vox.VisibilityMasks[VoxelData.GetVisibilityIndex(x, y - 1, 0)];
                            var right_col = *cur_col_p++;//right_col_p[x];//vox.VisibilityMasks[VoxelData.GetVisibilityIndex(x + 1, y, 0)];
                            var btm_col = btm_col_p[x];//vox.VisibilityMasks[VoxelData.GetVisibilityIndex(x, y + 1, 0)];

                            if (cur_col != 0)
                            {
                                var xy = (y << 11) | (x << 6);
                                var left_vis = Bmi.AndNot(left_col, cur_col); //(left_col ^ cur_col) & cur_col;
                                var top_vis = Bmi.AndNot(top_col, cur_col);//(top_col ^ cur_col) & cur_col;
                                var right_vis = Bmi.AndNot(right_col, cur_col);//(right_col ^ cur_col) & cur_col;
                                var btm_vis = Bmi.AndNot(btm_col, cur_col);//(btm_col ^ cur_col) & cur_col;
                                var cur_col2 = Bmi.AndNot(cur_col >> 1, cur_col);

                                while (cur_col != 0)
                                {
                                    var fidx = Bmi.TrailingZeroCount(cur_col);
                                    *inds_p++ = buildFace(ref vox, xy, fidx, backFace);
                                    cur_col = Bmi.ResetLowestSetBit(cur_col);
                                }

                                while (cur_col2 != 0)
                                {
                                    var fidx = Bmi.TrailingZeroCount(cur_col2);
                                    *inds_p++ = buildFace(ref vox, xy, fidx, frontFace);
                                    cur_col2 = Bmi.ResetLowestSetBit(cur_col2);
                                }

                                while (top_vis != 0)
                                {
                                    var fidx = Bmi.TrailingZeroCount(top_vis);
                                    *inds_p++ = buildFace(ref vox, xy, fidx, topFace);    //append face
                                    top_vis = Bmi.ResetLowestSetBit(top_vis);
                                }

                                while (btm_vis != 0)
                                {
                                    var fidx = Bmi.TrailingZeroCount(btm_vis);
                                    *inds_p++ = buildFace(ref vox, xy, fidx, btmFace);    //append face
                                    btm_vis = Bmi.ResetLowestSetBit(btm_vis);
                                }

                                while (left_vis != 0)
                                {
                                    var fidx = Bmi.TrailingZeroCount(left_vis);
                                    *inds_p++ = buildFace(ref vox, xy, fidx, leftFace);    //append face
                                    left_vis = Bmi.ResetLowestSetBit(left_vis);
                                }

                                while (right_vis != 0)
                                {
                                    var fidx = Bmi.TrailingZeroCount(right_vis);
                                    *inds_p++ = buildFace(ref vox, xy, fidx, rightFace);    //append face
                                    right_vis = Bmi.ResetLowestSetBit(right_vis);
                                }
                            }

                            left_col = cur_col_orig;
                            cur_col = right_col;
                            cur_col_orig = right_col;
                        }

                        top_col_p += VoxelConstants.ChunkSideWithNeighbors;
                        btm_col_p += VoxelConstants.ChunkSideWithNeighbors;
                    }
                    inds_pos = (int)(inds_p - inds_p_base);
                }
            }
        }
    }
}
