using Kokoro.Common;

namespace Kokoro.Graphics.Framegraph
{
    public class BufferTransferPass : UniquelyNamedObject
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public ulong SourceOffset { get; set; }
        public ulong DestinationOffset { get; set; }
        public ulong Size { get; set; }

        public BufferTransferPass(string name) : base(name) { }
    }
}
