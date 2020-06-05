using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;

namespace Kokoro.Math
{
    public static class MortonCoder
    {
        static uint[] byteToMorton;
        static byte[] mortonToByte_lower;

        const uint xMask = 0x49249249;
        const uint yMask = 0x92492492;
        const uint zMask = 0x24924924;
        const uint xyMask = xMask | yMask;
        const uint xzMask = xMask | zMask;
        const uint yzMask = yMask | zMask;

        static MortonCoder()
        {
            byteToMorton = new uint[byte.MaxValue + 1];
            mortonToByte_lower = new byte[1 << (3 * 4)];
            for (uint i = 0; i < byteToMorton.Length; i++)
            {
                uint cur_val = 0;
                for (int j = 0; j < 8; j++)
                    cur_val |= (((i >> j) & 1) << (j * 3));
                byteToMorton[i] = cur_val;
                mortonToByte_lower[cur_val & 0xfff] = (byte)(i & 0x0f);
            }

            for (uint i = 0; i < mortonToByte_lower.Length; i++)
            {
                var idx_l = i & 0x9249;
                mortonToByte_lower[i] = mortonToByte_lower[idx_l];
            }
        }


        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static uint Encode(byte x, byte y, byte z)
        {
            //0.17ms per 100,000 encodes
            //return Bmi.ParallelBitDeposit(z, 0x24924924) | Bmi.ParallelBitDeposit(y, 0x12492492) | Bmi.ParallelBitDeposit(x, 0x09249249);

            //~5 cycles + memory/cache access, should be cheaper than pdep
            //0.314ms per 100,000 encodes
            var m_x = byteToMorton[x];
            var m_y = byteToMorton[y];
            var m_z = byteToMorton[z];
            return m_x | (m_y << 1) | (m_z << 2);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Decode(uint val, out byte x, out byte y, out byte z)
        {
            //pext is far more expensive than lookup on ryzen
            //x = (byte)Bmi.ParallelBitExtract(val, 0x09249249);
            //y = (byte)Bmi.ParallelBitExtract(val, 0x12492492);
            //z = (byte)Bmi.ParallelBitExtract(val, 0x24924924);
            var v_x = val;
            var v_y = (val >> 1);
            var v_z = (val >> 2);

            x = (byte)((mortonToByte_lower[(v_x >> 12)] << 4) | mortonToByte_lower[v_x & 0xfff]);
            y = (byte)((mortonToByte_lower[(v_y >> 12)] << 4) | mortonToByte_lower[v_y & 0xfff]);
            z = (byte)((mortonToByte_lower[(v_z >> 12)] << 4) | mortonToByte_lower[v_z & 0xfff]);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static uint Add(uint a, uint b)
        {
            var xS = (a & yzMask) + (b & xMask);
            var yS = (a & xzMask) + (b & yMask);
            var zS = (a & xyMask) + (b & zMask);
            return (xS & xMask) | (yS & yMask) | (zS & zMask);
        }

        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static uint Sub(uint a, uint b)
        {
            var xS = (a & xMask) - (b & xMask);
            var yS = (a & yMask) - (b & yMask);
            var zS = (a & zMask) - (b & zMask);
            return (xS & xMask) | (yS & yMask) | (zS & zMask);
        }
    }
}
