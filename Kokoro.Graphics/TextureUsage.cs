using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public enum TextureUsage
    {
        ColorAttachment,
        DepthStencilAttachment,
        InputAttachment,
        TransferDst,
        TransferSrc,
        TransientAttachment,
        Storage,
        Sampled,
    }
}