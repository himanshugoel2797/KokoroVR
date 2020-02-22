using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics.VulkanTest
{
    class Program
    {
        static void Main(string[] args)
        {
            GraphicsDevice.AppName = "Vulkan Test";
            GraphicsDevice.EnableValidation = true;
            GraphicsDevice.Init();
        }
    }
}
