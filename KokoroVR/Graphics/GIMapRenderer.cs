using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kokoro.Graphics;
using Kokoro.Math;

namespace KokoroVR.Graphics
{
    public class GIMapRenderer
    {
        private const int Side = 256;

        //Camera position cubemap
        private Vector3 eyePos;
        private Vector3 eyeDir;

        private Texture cubemapTex;
        private Texture cubemapTexColor;
        private Texture depthTex;
        private Framebuffer cubemap;

        public Framebuffer CubeMap { get => cubemap; }
        public Texture CubeMapTex { get => cubemapTex; }
        public Texture CubeMapTexColor { get => cubemapTexColor; }

        public TextureHandle CubeMapHandle { get; private set; }
        public TextureHandle CubeMapColorHandle { get; private set; }

        public GIMapRenderer()
        {
            {
                var texSrc = new CubeMapFramebufferTextureSource(Side, Side, 1)
                {
                    InternalFormat = PixelInternalFormat.Rgba32f,
                    Format = PixelFormat.Bgra,
                    PixelType = PixelType.Float
                };
                cubemapTex = new Texture();
                cubemapTex.SetData(texSrc, 0, CubeMapFace.PositiveX);
                cubemapTex.SetData(texSrc, 0, CubeMapFace.NegativeX);
                cubemapTex.SetData(texSrc, 0, CubeMapFace.PositiveY);
                cubemapTex.SetData(texSrc, 0, CubeMapFace.NegativeY);
                cubemapTex.SetData(texSrc, 0, CubeMapFace.PositiveZ);
                cubemapTex.SetData(texSrc, 0, CubeMapFace.NegativeZ);

                var sampler = new TextureSampler();
                sampler.SetEnableLinearFilter(true);
                sampler.SetTileMode(false, false);
                CubeMapHandle = cubemapTex.GetHandle(sampler).SetResidency(Residency.Resident);
            }
            {
                var texSrc = new CubeMapFramebufferTextureSource(Side, Side, 1)
                {
                    InternalFormat = PixelInternalFormat.Rgba16f,
                    Format = PixelFormat.Bgra,
                    PixelType = PixelType.HalfFloat
                };
                cubemapTexColor = new Texture();
                cubemapTexColor.SetData(texSrc, 0, CubeMapFace.PositiveX);
                cubemapTexColor.SetData(texSrc, 0, CubeMapFace.NegativeX);
                cubemapTexColor.SetData(texSrc, 0, CubeMapFace.PositiveY);
                cubemapTexColor.SetData(texSrc, 0, CubeMapFace.NegativeY);
                cubemapTexColor.SetData(texSrc, 0, CubeMapFace.PositiveZ);
                cubemapTexColor.SetData(texSrc, 0, CubeMapFace.NegativeZ);

                var sampler = new TextureSampler();
                sampler.SetEnableLinearFilter(true);
                sampler.SetTileMode(false, false);
                CubeMapColorHandle = cubemapTexColor.GetHandle(sampler).SetResidency(Residency.Resident);
            }
            {
                var depthSrc = new CubeMapFramebufferTextureSource(Side, Side, 1)
                {
                    InternalFormat = PixelInternalFormat.DepthComponent32f
                };
                depthTex = new Texture();
                depthTex.SetData(depthSrc, 0, CubeMapFace.PositiveX);
                depthTex.SetData(depthSrc, 0, CubeMapFace.NegativeX);
                depthTex.SetData(depthSrc, 0, CubeMapFace.PositiveY);
                depthTex.SetData(depthSrc, 0, CubeMapFace.NegativeY);
                depthTex.SetData(depthSrc, 0, CubeMapFace.PositiveZ);
                depthTex.SetData(depthSrc, 0, CubeMapFace.NegativeZ);

                cubemap = new Framebuffer(Side, Side);
                cubemap[FramebufferAttachment.ColorAttachment0] = cubemapTex;
                cubemap[FramebufferAttachment.ColorAttachment1] = cubemapTexColor;
                cubemap[FramebufferAttachment.DepthAttachment] = depthTex;
            }
        }
    }
}
