using System;
using System.Collections.Generic;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public enum ShaderType : int
    {
        FragmentShader = VkShaderStageFlags.ShaderStageFragmentBit,
        VertexShader = VkShaderStageFlags.ShaderStageVertexBit,
        GeometryShader = VkShaderStageFlags.ShaderStageGeometryBit,
        TessEvaluationShader = VkShaderStageFlags.ShaderStageTessellationEvaluationBit,
        TessControlShader = VkShaderStageFlags.ShaderStageTessellationControlBit,
        ComputeShader = VkShaderStageFlags.ShaderStageComputeBit,
        All = FragmentShader | VertexShader | GeometryShader | TessEvaluationShader | TessControlShader | ComputeShader,
    }

}
