using Kokoro.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kokoro.Graphics.Framegraph
{
    public class SpecializedShader
    {
        public string Name { get; set; }
        public ShaderType ShaderType { get => Shader.ShaderType; }
        public ShaderSource Shader { get; set; }
        public Memory<int> SpecializationData { get; set; }
    }
}
