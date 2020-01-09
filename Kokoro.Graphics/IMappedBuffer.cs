using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public interface IMappedBuffer
    {
        long Size { get; }
        long Offset { get; }

        unsafe byte* Update();
        void UpdateDone();
        void UpdateDone(long off, long usize);
    }
}
