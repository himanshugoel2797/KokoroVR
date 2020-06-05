using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;

namespace KokoroVR2.Graphics.Voxel
{
    public class VoxelHashMap
    {
        private const uint hash_modulus0 = (1 << 7) - 1;
        private const uint hash_modulus1 = (1 << 13) - 1;
        private const uint hash_modulus2 = (1 << 17) - 1;
        private const uint hash_modulus3 = (1 << 19) - 1;
        private const uint hash_modulus4 = (1 << 22) + 1;
        private static uint[] hash_modulus;

        static VoxelHashMap()
        {
            hash_modulus = new uint[]
            {
                hash_modulus0,
                hash_modulus1,
                hash_modulus2,
                hash_modulus3,
                hash_modulus4
            };
        }

        private uint[] data;
        private uint current_modulus_idx;
        public uint Count { get; private set; }

        public VoxelHashMap()
        {
            data = new uint[hash_modulus0];
            current_modulus_idx = 0;
            Count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Add(uint value)
        {
            uint idx = ComputeHash(value);
            var data_idx = data[idx % hash_modulus[current_modulus_idx]];
            var dst_idx = ComputeHash(data_idx);
            if (data_idx != 0)
                if (dst_idx != idx)
                {
                    Rebuild();
                    Add(value);
                    Console.WriteLine("Warning: Rebuild hashmap.");
                    return;
                }
            if (data_idx == 0)
                Count++;
            data[idx % hash_modulus[current_modulus_idx]] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public uint Get(uint value)
        {
            uint idx = ComputeHash(value);
            if (ComputeHash(data[idx % hash_modulus[current_modulus_idx]]) != idx)
            {
                return 0;
            }
            return data[idx % hash_modulus[current_modulus_idx]];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool Exists(uint value)
        {
            return Get(value) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Remove(uint value)
        {
            uint idx = ComputeHash(value);
            if (ComputeHash(data[idx % hash_modulus[current_modulus_idx]]) != idx)
            {
                return;
            }
            if (data[idx % hash_modulus[current_modulus_idx]] != 0) Count--;
            data[idx % hash_modulus[current_modulus_idx]] = 0;
        }

        private void Rebuild()
        {
            //Increase modulus and rehash dictionary
            current_modulus_idx++;
            var data_n = new uint[hash_modulus[current_modulus_idx]];
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] != 0)
                {
                    uint n_hash = ComputeHash(data[i]) % hash_modulus[current_modulus_idx];
                    if (data_n[n_hash] != 0)
                    {
                        data_n = null;
                        Rebuild();
                        Console.WriteLine("Warning: Nested Rebuild.");
                        return;
                    }
                    data_n[n_hash] = data[i];
                }
            }
            data = data_n;
        }

        public void Compact()
        {
            //Try shrinking the map if possible
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static uint ComputeHash(uint key)
        {
            uint hash = key & 0x003fffff;
            return hash;
        }
    }
}
