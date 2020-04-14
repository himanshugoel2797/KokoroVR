namespace Kokoro.Graphics
{
    public struct ImageMemoryBarrier
    {
        public ImageLayout OldLayout { get; set; }
        public ImageLayout NewLayout { get; set; }
        public Image Image { get; set; }
        public CommandQueueKind SrcFamily { get; set; }
        public CommandQueueKind DstFamily { get; set; }
        public AccessFlags Accesses { get; set; }
        public AccessFlags Stores { get; set; }
        public uint BaseMipLevel { get; set; }
        public uint LevelCount { get; set; }
        public uint BaseArrayLayer { get; set; }
        public uint LayerCount { get; set; }
    }

    public struct BufferMemoryBarrier
    {
        public CommandQueueKind SrcFamily { get; set; }
        public CommandQueueKind DstFamily { get; set; }
        public AccessFlags Accesses { get; set; }
        public AccessFlags Stores { get; set; }
        public GpuBuffer Buffer { get; set; }
        public ulong Offset { get; set; }
        public ulong Size { get; set; }
    }
}
