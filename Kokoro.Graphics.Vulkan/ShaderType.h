#pragma once
#include "GraphicsDevice.h"

namespace Kokoro::Graphics {
	public enum class ShaderType {
		Vertex = (1 << 0),
		Fragment = (1 << 1),
		Compute = (1 << 2),
		Geometry = (1 << 3),
		TessEval = (1 << 4),
		TessCtrl = (1 << 5),
	};

	public ref class ShaderTypeConv {
	public:
		static VkShaderStageFlagBits Convert(ShaderType s) {
			uint32_t f = 0;
			if ((s & ShaderType::Vertex) != (ShaderType)0)
				f |= VK_SHADER_STAGE_VERTEX_BIT;
			if ((s & ShaderType::Fragment) != (ShaderType)0)
				f |= VK_SHADER_STAGE_FRAGMENT_BIT;
			if ((s & ShaderType::Geometry) != (ShaderType)0)
				f |= VK_SHADER_STAGE_GEOMETRY_BIT;
			if ((s & ShaderType::TessCtrl) != (ShaderType)0)
				f |= VK_SHADER_STAGE_TESSELLATION_CONTROL_BIT;
			if ((s & ShaderType::TessEval) != (ShaderType)0)
				f |= VK_SHADER_STAGE_TESSELLATION_EVALUATION_BIT;
			if ((s & ShaderType::Compute) != (ShaderType)0)
				f |= VK_SHADER_STAGE_COMPUTE_BIT;
			return (VkShaderStageFlagBits)f;
		}
	};
}