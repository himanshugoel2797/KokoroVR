using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public enum BufferUsage
    {
        VertexBuffer = 34962,
        IndexBuffer = 34963,
        PixelPackBuffer = 35051,
        PixelUnpackBuffer = 35052,
        UniformBuffer = 35345,
        TextureBuffer = 35882,
        TransformFeedbackBuffer = 35982,
        CopyReadBuffer = 36662,
        CopyWriteBuffer = 36663,
        IndirectBuffer = 36671,
        StorageBuffer = 37074,
        DispatchIndirectBuffer = 37102,
        QueryBuffer = 37266,
        AtomicCounterBuffer = 37568
    }
}