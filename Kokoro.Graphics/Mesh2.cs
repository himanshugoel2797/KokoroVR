using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public interface IMesh2
    {
        int Length { get; }
        uint BlockSize { get; }
        int[] AllocIndices { get; }

        bool IsVisible(Frustum f, int k);
    }
}
