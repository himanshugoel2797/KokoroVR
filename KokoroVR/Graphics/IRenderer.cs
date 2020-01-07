using Kokoro.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Graphics
{
    public interface IRenderer
    {
        Framebuffer[] Framebuffers { get; }
    }
}
