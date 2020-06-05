using System;
using System.Runtime.InteropServices;
using Kokoro.Common;
using Kokoro.Graphics;
using Kokoro.Graphics.Framegraph;
using KokoroVR2;

namespace Kokoro.PlanetGen.Gpu
{
    public class HeightMapGen : UniquelyNamedObject
    {
        static Image terrainHeightMap;
        static ImageView terrainHeightView;
        static SpecializedShader genShader;
        static IntPtr Constants;
        public uint Side { get; }

        public HeightMapGen(string name, uint terrainSide) : base(name)
        {
            Side = terrainSide;
            terrainHeightMap = new Image("terrainHeightMap")
            {
                Width = terrainSide,
                Height = terrainSide,
                Depth = 1,
                Levels = 1,
                Layers = 1,
                Dimensions = 2,
                Format = ImageFormat.Rg32f,
                Usage = ImageUsage.Storage | ImageUsage.TransferSrc,
                InitialLayout = ImageLayout.Undefined,
            };
            terrainHeightMap.Build(0);

            terrainHeightView = new ImageView("terrainHeightView")
            {
                Format = ImageFormat.Rg32f,
                ViewType = ImageViewType.View2D,
                BaseLevel = 0,
                LevelCount = 1,
                BaseLayer = 0,
                LayerCount = 1,
            };
            terrainHeightView.Build(terrainHeightMap);

            Constants = Marshal.AllocHGlobal(sizeof(uint));
            unsafe
            {
                uint* ui = (uint*)Constants;
                *ui = 0;
            }
        }

        public void RebuildGraph()
        {
            Engine.RenderGraph.RegisterResource(terrainHeightView);
            Engine.RenderGraph.RegisterShader(genShader);
            Engine.RenderGraph.RegisterComputePass(new ComputePass(Name + "_genPass")
            {
                IsAsync = false,
                Shader = Name + "_genShader",
                Resources = new ResourceUsageEntry[]{
                    new ImageViewUsageEntry(){
                        StartStage = PipelineStage.CompShader,
                        StartAccesses = AccessFlags.None,
                        StartLayout = ImageLayout.General,
                        BaseArrayLayer = 0,
                        LayerCount = 1,
                        BaseMipLevel = 0,
                        LevelCount = 1,
                        FinalAccesses = AccessFlags.ShaderWrite,
                        FinalLayout = ImageLayout.General,
                        FinalStage = PipelineStage.CompShader,
                    }
                },
                DescriptorSetup = new DescriptorSetup()
                {
                    Descriptors = new DescriptorConfig[]{
                        new DescriptorConfig(){
                            Count = 1,
                            Index = 0,
                            DescriptorType = DescriptorType.StorageImage,
                        },
                    },
                    PushConstants = new PushConstantConfig[]{   //Seed
                        new PushConstantConfig(){
                            Stages = ShaderType.ComputeShader,
                            Offset = 0,
                            Size = 4,
                        }
                    },
                },
            });
        }

        public void Generate()
        {
            Engine.RenderGraph.QueueOp(new GpuOp()
            {
                Cmd = GpuCmd.Compute,
                PassName = Name + "_genShader",
                Resources = new string[]{
                    terrainHeightView.Name,
                },
                PushConstants = Constants,
            });
        }
    }
}
