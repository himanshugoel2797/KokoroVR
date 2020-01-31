using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public enum DepthFunc
    {
        None = OpenTK.Graphics.OpenGL4.DepthFunction.Never,
        LEqual = OpenTK.Graphics.OpenGL4.DepthFunction.Lequal,
        GEqual = OpenTK.Graphics.OpenGL4.DepthFunction.Gequal,
        Less = OpenTK.Graphics.OpenGL4.DepthFunction.Less,
        Greater = OpenTK.Graphics.OpenGL4.DepthFunction.Greater,
        Equal = OpenTK.Graphics.OpenGL4.DepthFunction.Equal,
        Always = OpenTK.Graphics.OpenGL4.DepthFunction.Always,
    }
}