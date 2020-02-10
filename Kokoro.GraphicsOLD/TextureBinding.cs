using OpenTK.Graphics.OpenGL4;

namespace Kokoro.Graphics
{
    public struct TextureBinding
    {
        public TextureView View { get; set; }
        public TextureSampler Sampler { get; set; }

        public TextureHandle GetTextureHandle()
        {
            if ((int)Sampler == 0)
                return new TextureHandle(GL.Arb.GetTextureHandle((int)View), View, Sampler);
            else
                return new TextureHandle(GL.Arb.GetTextureSamplerHandle((int)View, (int)Sampler), View, Sampler);
        }
    }
}
