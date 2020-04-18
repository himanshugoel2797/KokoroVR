using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;

namespace KokoroVR2.Graphics.Voxel
{
    public class VoxelMesher
    {
        public static uint[] MeshChunk(VoxelData vox)
        {
            uint buildFace(int x, int y, int z, int face)
            {
                var mat = vox.GetMaterialData(vox, x, y, z);
                return (uint)((face & 0x7) << 24 | (mat & 0xff) << 16 | (z & 0x1f) << 10 | (y & 0x1f) << 5 | (x & 0x1f));
            }

            var inds = new List<uint>();

            for (int y = 1; y < VoxelConstants.ChunkSideWithNeighbors - 1; y++)
            {
                var left_col = vox.VisibilityMasks[VoxelData.GetVisibilityIndex(0, y, 0)];
                var cur_col = vox.VisibilityMasks[VoxelData.GetVisibilityIndex(1, y, 0)];
                for (int x = 1; x < VoxelConstants.ChunkSideWithNeighbors - 1; x++)
                {
                    var top_col = vox.VisibilityMasks[VoxelData.GetVisibilityIndex(x, y - 1, 0)];
                    var right_col = vox.VisibilityMasks[VoxelData.GetVisibilityIndex(x + 1, y, 0)];
                    var btm_col = vox.VisibilityMasks[VoxelData.GetVisibilityIndex(x, y + 1, 0)];

                    if (cur_col != 0)
                    {
                        var left_vis = (left_col ^ cur_col) & cur_col;
                        var top_vis = (top_col ^ cur_col) & cur_col;
                        var right_vis = (right_col ^ cur_col) & cur_col;
                        var btm_vis = (btm_col ^ cur_col) & cur_col;

                        bool isNZ = true;
                        while (cur_col != 0)
                        {
                            var fidx = Lzcnt.LeadingZeroCount(cur_col);
                            if (fidx != 0 && fidx != 31)
                            {
                                inds.Add(buildFace(x, y, (int)fidx, isNZ ? 1 : 0));    //TODO: Append face
                            }
                            isNZ = !isNZ;
                            cur_col = ~cur_col & ~((1u << (int)fidx) - 1);
                        }

                        while (top_vis != 0)
                        {
                            var fidx = Lzcnt.LeadingZeroCount(top_vis);
                            if (fidx != 0 && fidx != 31)
                            {
                                inds.Add(buildFace(x, y, (int)fidx, 2));    //TODO: append face
                            }
                            top_vis &= ~(1u << (int)fidx);
                        }

                        while (btm_vis != 0)
                        {
                            var fidx = Lzcnt.LeadingZeroCount(btm_vis);
                            if (fidx != 0 && fidx != 31)
                            {
                                inds.Add(buildFace(x, y, (int)fidx, 3));    //TODO: append face
                            }
                            btm_vis &= ~(1u << (int)fidx);
                        }

                        while (left_vis != 0)
                        {
                            var fidx = Lzcnt.LeadingZeroCount(left_vis);
                            if (fidx != 0 && fidx != 31)
                            {
                                inds.Add(buildFace(x, y, (int)fidx, 4));    //TODO: append face
                            }
                            left_vis &= ~(1u << (int)fidx);
                        }

                        while (right_vis != 0)
                        {
                            var fidx = Lzcnt.LeadingZeroCount(right_vis);
                            if (fidx != 0 && fidx != 31)
                            {
                                inds.Add(buildFace(x, y, (int)fidx, 5));    //TODO: append face
                            }
                            right_vis &= ~(1u << (int)fidx);
                        }
                    }

                    left_col = cur_col;
                    cur_col = right_col;
                }
            }
            return inds.ToArray();
        }
    }
}
