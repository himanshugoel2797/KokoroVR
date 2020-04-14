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

        public void FinalizeRenderGraph()
        {
            //TODO: Add Hi-Z generation pass
        }

        public void Update(double time_ms, double delta_ms)
        {

        }
    }
}
