using System;

namespace Kokoro.Graphics
{
    [Flags]
    public enum BarrierType
    {
        ElementArray = (1 << 0),
        UniformBuffer = (1 << 1),
        TextureFetch = (1 << 2),
        ShaderImageAccess = (1 << 3),
        Command = (1 << 4),
        PixelBuffer = (1 << 5),
        TextureUpdate = (1 << 6),
        BufferUpdate = (1 << 7),
        Framebuffer = (1 << 8),
        StorageBuffer = (1 << 9),
    }
}
