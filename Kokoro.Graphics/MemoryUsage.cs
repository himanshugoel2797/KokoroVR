using static VulkanSharp.Raw.Vma;

namespace Kokoro.Graphics
{
    public enum MemoryUsage
    {
        CpuToGpu = VmaMemoryUsage.CpuToGpu,
        GpuOnly = VmaMemoryUsage.GpuOnly,
        CpuOnly = VmaMemoryUsage.CpuOnly
    }
}
