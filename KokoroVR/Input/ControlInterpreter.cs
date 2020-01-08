using Kokoro.Graphics;
using Kokoro.Math;
using KokoroVR.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Input
{
    public abstract class ControlInterpreter
    {
        public abstract void Update(double time, World parent);
        public abstract void Render(double time, Framebuffer fbuf, StaticMeshRenderer staticMesh, DynamicMeshRenderer dynamicMesh, VREye eye, VRHand hand);
    }
}
