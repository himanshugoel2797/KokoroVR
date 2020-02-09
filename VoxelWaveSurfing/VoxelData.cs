using Kokoro.Math;
using Kokoro.Math.Data;
using SharpNoise.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelWaveSurfing
{
    public struct VoxelSpan
    {
        public float Start;
        public float Stop;
    }

    public class VoxelData
    {
        public const int Side = 2048;
        public const float Scale = 10;
        public const float WorldSide = Side / Scale;
        public const float WorldStep = 1.0f / Scale;

        public QuadTree<VoxelSpan[]> SpanTree;
        VoxelSpan[][] Spans;

        public VoxelData()
        {
            SpanTree = new QuadTree<VoxelSpan[]>(Vector2.Zero, Vector2.One * Side, 0);
            var p = new Perlin();
            for (int iy = 0; iy < Side; iy++)
                for (int ix = 0; ix < Side; ix++)
                    SpanTree.Insert(new Vector2(ix, iy), new Vector2(ix + 1, iy + 1), new VoxelSpan[1]
                    {
                        new VoxelSpan()
                        {
                            Start = 0,
                            Stop = (int)((p.GetValue(ix / (float)Scale * 0.005f, iy / (float)Scale * 0.005f, 0) + 1) * 64)
                        }
                    });
        }

        public VoxelSpan[] Get(float x, float y)
        {
            int ix = (int)(x * Scale);
            int iy = (int)(y * Scale);

            if (ix >= Side) return null;
            if (iy >= Side) return null;
            if (ix < 0) return null;
            if (iy < 0) return null;
            return Spans[iy * Side + ix];
        }
    }
}
