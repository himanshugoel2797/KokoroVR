using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Text;

namespace KokoroVR2.Graphics.Voxel
{
    public class VoxelOctree
    {
        const uint Side = 128;
        const uint LeafNodeBit = (1u << 23);
        const byte MaxLevel = 7;
        private VoxelHashMap map;

        public VoxelOctree()
        {
            map = new VoxelHashMap();
        }

        static uint ComputeKey_RootRel(byte x, byte y, byte z, byte lvl, byte mat, bool isLeaf)
        {
            return (isLeaf ? LeafNodeBit : 0u) | (1u << (3 * lvl)) | (MortonCoder.Encode(x, y, z) >> (int)(3 * (MaxLevel - lvl))) | (uint)(mat << 24);
        }

        static uint ComputeKey_RootRel(uint key, byte lvl)
        {
            var mat = key & 0xff80_0000;
            return mat | ((key & 0x003f_ffff) >> (int)((MaxLevel - lvl) * 3));
        }

        static uint ComputeKeyNotLeaf_RootRel(uint key, byte lvl)
        {
            var mat = key & 0xff00_0000;
            return mat | ((key & 0x003f_ffff) >> (int)((MaxLevel - lvl) * 3));
        }

        static uint ComputeKeyMakeLeaf_RootRel(uint key, byte lvl)
        {
            var mat = key & 0xff00_0000 | LeafNodeBit;
            return mat | ((key & 0x003f_ffff) >> (int)((MaxLevel - lvl) * 3));
        }

        public void Insert(byte x, byte y, byte z, byte mat)
        {
            var leaf_key = ComputeKey_RootRel(x, y, z, MaxLevel, mat, true);
            map.Add(leaf_key);

            for (byte i = MaxLevel - 1; i >= 0; i--)
            {
                var cur_key = ComputeKeyNotLeaf_RootRel(leaf_key, i);
                var val_at_key = map.Get(cur_key);
                if (val_at_key != 0)
                {
                    if ((val_at_key & LeafNodeBit) != 0)
                    {
                        //is marked as a leaf, correct this
                        map.Add(cur_key);
                    }
                    break;
                }
                map.Add(cur_key);
            }
        }

        private void Prune(uint key, byte lvl)
        {
            var cur_node = map.Get(key);
            if (cur_node == 0)
                return;

            if ((cur_node & LeafNodeBit) == 0)   //Not a leaf node
            {
                for (uint i = 0; i < 8; i++)    //Depth-first traversal
                    Prune((key << 3) | i, (byte)(lvl + 1));

                //if all children are leaf nodes with the same mat, collapse them
                var cur_mat = map.Get(key << 3) & 0xff80_0000;
                for (uint i = 1; i < 8; i++)
                    if ((map.Get((key << 3) | i) & 0xff80_0000) != cur_mat)
                        return;
                //collapse children
                map.Add(ComputeKeyMakeLeaf_RootRel(key, 0));

                for (uint i = 0; i < 8; i++)
                    map.Remove((key << 3) | i);
            }
        }

        public void Prune()
        {
            Prune(1, 0);
        }

        private void GatherLeaves(uint key, byte lvl, List<uint> leaves)
        {
            var cur_node = map.Get(key);
            if (cur_node == 0)
                return;

            if ((cur_node & LeafNodeBit) == 0)   //Not a leaf node
            {
                for (uint i = 0; i < 8; i++)    //Depth-first traversal
                    GatherLeaves((key << 3) | i, (byte)(lvl + 1), leaves);
            }
            else
            {
                //recode cur_node
                MortonCoder.Decode((key << (3 * (MaxLevel - lvl))) & 0x003fffff, out var x, out var y, out var z);
                leaves.Add(x | (uint)(y << 7) | (uint)(z << 14) | (uint)((lvl & 0x7) << 21) | (key & 0xff00_0000));
            }
        }

        public uint[] GatherLeaves()
        {
            List<uint> leaves = new List<uint>();
            GatherLeaves(1, 0, leaves);
            return leaves.ToArray();
        }
    }
}
