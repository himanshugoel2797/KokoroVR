using System;
using System.Collections.Generic;
using System.Text;
using Kokoro.Graphics;

namespace KokoroVR2.Graphics
{
    public class DeferredRenderer
    {
        public const string PositionMapName = "PositionGBuffer";
        public const string NormalMapName = "NormalGBuffer";
        public const string DepthMapName = "DepthGBuffer";
        public const string PassName = "OutputPass";

        private List<string> dependencies;

        public DeferredRenderer() {
            dependencies = new List<string>();
        }

        public void RegisterDependency(string name)
        {
            dependencies.Add(name);
        }

        public void Reset()
        {
            dependencies.Clear();
        }

        public void GenerateRenderGraph()
        {
            Engine.Graph.RegisterAttachment(new AttachmentInfo()
            {
                Name = PositionMapName,
                BaseSize = SizeClass.ScreenRelative,
                SizeX = 1,
                SizeY = 1,
                Format = ImageFormat.Rgba32f,
                Layers = 1,
                UseMipMaps = false,
                Usage = ImageUsage.ColorAttachment | ImageUsage.Sampled
            });
            Engine.Graph.RegisterAttachment(new AttachmentInfo()
            {
                Name = NormalMapName,
                BaseSize = SizeClass.ScreenRelative,
                SizeX = 1,
                SizeY = 1,
                Format = ImageFormat.B8G8R8A8Unorm,
                Layers = 1,
                UseMipMaps = false,
                Usage = ImageUsage.ColorAttachment | ImageUsage.Sampled
            });
            Engine.Graph.RegisterAttachment(new AttachmentInfo()
            {
                Name = DepthMapName,
                BaseSize = SizeClass.ScreenRelative,
                SizeX = 1,
                SizeY = 1,
                Format = ImageFormat.Depth32f,
                Layers = 1,
                UseMipMaps = true,
                Usage = ImageUsage.DepthAttachment | ImageUsage.Sampled
            });
        }

        public void FinalizeRenderGraph()
        {
            //TODO: Add Hi-Z generation pass

            Engine.Graph.SetOutputPass(PositionMapName);
        }

        public void Update(double time_ms, double delta_ms)
        {

        }
    }
}
