using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{

    public class BufferGroup
    {
        private Dictionary<string, BufferTexture> buffers;
        public ulong Size { get; private set; }
        public int Count { get => buffers.Count; }
        public Dictionary<string, BufferTexture> Buffers { get => buffers; }

        public BufferGroup(uint element_cnt, bool stream, params (string, uint, PixelInternalFormat)[] elements)
        {
            buffers = new Dictionary<string, BufferTexture>();
            for (int i = 0; i < elements.Length; i++)
                buffers[elements[i].Item1] = new BufferTexture(elements[i].Item2 * element_cnt, elements[i].Item3, stream);
        }

        public unsafe byte* Update(string i)
        {
            return buffers[i].Update();
        }

        public void UpdateDone(string i)
        {
            buffers[i].UpdateDone();
        }

        public void UpdateDone(string i, long off, long usize)
        {
            buffers[i].UpdateDone(off, usize);
        }
    }
}
