using System;
using Kokoro.Common;
using Kokoro.Graphics.Framegraph;

namespace Kokoro.Graphics
{
    public class StreamableImage : UniquelyNamedObject
    {
        private bool isDirty;
        public Image LocalImage { get; private set; }
        public ImageView LocalImageView { get; private set; }
        public GpuBuffer HostBuffer { get; private set; }
        public ulong Size { get; }
        public bool Streamable { get; private set; }

        public StreamableImage(string name, ulong buf_sz, uint dims, uint w, uint h, uint d, uint layers, uint levels, ImageViewType viewType, ImageFormat format, ImageUsage usage) : base(name)
        {
            this.Size = buf_sz;
            this.Streamable = true;
            LocalImage = new Image(Name + "_upload_img")
            {
                Cubemappable = false,
                Width = w,
                Height = h,
                Depth = d,
                Dimensions = dims,
                InitialLayout = ImageLayout.Undefined,
                Layers = layers,
                Levels = levels,
                MemoryUsage = MemoryUsage.GpuOnly,
                Usage = usage | ImageUsage.TransferDst,
                Format = format,
            };
            LocalImage.Build(0);

            LocalImageView = new ImageView(Name)
            {
                BaseLayer = 0,
                BaseLevel = 0,
                Format = format,
                LayerCount = layers,
                LevelCount = levels,
                ViewType = viewType,
            };
            LocalImageView.Build(LocalImage);

            HostBuffer = new GpuBuffer(name + "_host")
            {
                Mapped = true,
                Size = buf_sz,
                MemoryUsage = MemoryUsage.CpuToGpu,
                Usage = BufferUsage.TransferSrc
            };
            HostBuffer.Build(0);
        }

        public void RebuildGraph(int x, int y, int z, uint w, uint h, uint d, uint baseLayer, uint layerCount, uint baseMipLevel)
        {
            var graph = GraphicsContext.RenderGraph;
            graph.RegisterResource(LocalImageView);
            if (Streamable)
            {
                graph.RegisterResource(HostBuffer);
                var imgUpPass = new ImageTransferPass(Name + "_transferOp")
                {
                    Source = HostBuffer.Name,
                    SourceOffset = 0,
                    Destination = LocalImageView.Name,
                    X = x,
                    Y = y,
                    Z = z,
                    Width = w,
                    Height = h,
                    Depth = d,
                    BaseArrayLayer = baseLayer,
                    LayerCount = layerCount,
                    BaseMipLevel = baseMipLevel,
                };
                graph.RegisterImageTransferPass(imgUpPass);
            }
        }

        public unsafe byte* BeginBufferUpdate()
        {
            if (!Streamable)
                throw new InvalidOperationException("Streaming has been ended already!");
            return (byte*)HostBuffer.GetAddress();
        }

        public void EndBufferUpdate()
        {
            isDirty = true;
        }

        public void Update()
        {
            if (Streamable && isDirty)
            {
                GraphicsContext.RenderGraph.QueueOp(new Framegraph.GpuOp()
                {
                    PassName = Name + "_transferOp",
                    Cmd = GpuCmd.Stage
                });
                isDirty = false;
            }
        }

        public void EndStreaming()
        {
            if (Streamable && !isDirty)
            {
                Streamable = false;
                HostBuffer.Dispose();
                HostBuffer = null;
            }
        }
    }
}
