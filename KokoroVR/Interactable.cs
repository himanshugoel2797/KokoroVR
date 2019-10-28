using Kokoro.Graphics;
using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valve.VR;

namespace KokoroVR
{
    public abstract class Interactable
    {
        public abstract void Update(double time);
        public abstract void Render(double time, Framebuffer fbuf, Matrix4 p, Matrix4 v, VREye eye);
    }
}
