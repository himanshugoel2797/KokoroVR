using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kokoro.Graphics
{
    public interface IMesh2
    {
        int Length { get; }
        uint BlockSize { get; }
        int[] AllocIndices { get; }
        uint BaseVertex { get; }
        int BufferIdx { get; }

        (int, uint, Vector3, Vector3)[] Sort(Frustum f, Vector3 eyePos);
        bool IsVisible(Frustum f, int k);
    }
}
