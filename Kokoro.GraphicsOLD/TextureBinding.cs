using OpenTK.Graphics.OpenGL4;

namespace Kokoro.Graphics
{
    public struct TextureBinding
    {
        private TextureHandle texHandle;

        public TextureView View { get; set; }
        public TextureSampler Sampler { get; set; }

        public TextureHandle GetTextureHandle()
        {
            if (texHandle == null)
                texHandle = new TextureHandle(GL.Arb.GetTextureSamplerHandle((int)View, (int)Sampler), View, Sampler);
            return texHandle;
        }
    }
}
