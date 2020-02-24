using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public enum ImageViewType
    {
        View1D = VkImageViewType.ImageViewType1d,
        View1DArray = VkImageViewType.ImageViewType1dArray,
        View2D = VkImageViewType.ImageViewType2d,
        View2DArray = VkImageViewType.ImageViewType2dArray,
        View3D = VkImageViewType.ImageViewType3d,
        ViewCube = VkImageViewType.ImageViewTypeCube,
        ViewCubeArray = VkImageViewType.ImageViewTypeCubeArray,
    }
}
