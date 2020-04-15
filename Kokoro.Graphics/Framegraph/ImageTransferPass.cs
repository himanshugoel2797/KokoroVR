namespace Kokoro.Graphics.Framegraph
{
    public class ImageTransferPass : UniquelyNamedObject
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public ulong SourceOffset { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public uint Depth { get; set; }
        public uint MipLevel { get; set; }
        public uint BaseArrayLayer { get; set; }
        public uint LayerCount { get; set; }
    }
}
