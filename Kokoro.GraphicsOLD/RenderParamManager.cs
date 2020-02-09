using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public class RenderParamManager
    {
        public PerObjectShaderParamManager ObjectParamManager { get; private set; }
        public PerPassShaderParamManager PassParamManager { get; private set; }

        public RenderParamManager(int maxPasses, int maxObjs)
        {
            PassParamManager = new PerPassShaderParamManager(maxPasses);
            ObjectParamManager = new PerObjectShaderParamManager(maxObjs);
        }

        public void Update()
        {
            //Upload parameters
            //Plan out pipeline carefully to avoid stalls
        }
    }
}
