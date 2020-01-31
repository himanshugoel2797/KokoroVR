#pragma once
#include "GraphicsDevice.h"
#include "ShaderType.h"
#include "TopologyType.h"

using namespace System::Collections::Generic;
using namespace Kokoro::Math;

namespace Kokoro::Graphics {
	public enum class CullMode {
		Back,
		Front,
		None,
		All,
	};

	public enum class FillMode {
		Fill,
		Line,
		Point
	};

	public enum class BlendFactor {
		One,
		Zero,
		SourceAlpha,
		OneMinusSourceAlpha,
	};

	public enum class BlendOp {
		Add,
	};

	class FillModeConv {
	public:
		static VkPolygonMode Convert(FillMode f) {
			switch (f) {
			case FillMode::Fill:
				return VK_POLYGON_MODE_FILL;
			case FillMode::Line:
				return VK_POLYGON_MODE_LINE;
			case FillMode::Point:
				return VK_POLYGON_MODE_POINT;
			default:
				return (VkPolygonMode)0;
			}
		}
	};

	class CullModeConv {
	public:
		static VkCullModeFlags Convert(CullMode f) {
			switch (f) {
			case CullMode::Back:
				return VK_CULL_MODE_BACK_BIT;
			case CullMode::Front:
				return VK_CULL_MODE_FRONT_BIT;
			case CullMode::None:
				return VK_CULL_MODE_NONE;
			case CullMode::All:
				return VK_CULL_MODE_FRONT_AND_BACK;
			default:
				return (VkCullModeFlags)0;
			}
		}
	};

	class BlendOpConv {
	public:
		static VkBlendOp Convert(BlendOp f) {
			switch (f) {
			case BlendOp::Add:
				return VK_BLEND_OP_ADD;
			default:
				return (VkBlendOp)0;
			}
		}
	};

	class BlendFactorConv {
	public:
		static VkBlendFactor Convert(BlendFactor f) {
			switch (f) {
			case BlendFactor::One:
				return VK_BLEND_FACTOR_ONE;
			case BlendFactor::OneMinusSourceAlpha:
				return VK_BLEND_FACTOR_ONE_MINUS_SRC_ALPHA;
			case BlendFactor::SourceAlpha:
				return VK_BLEND_FACTOR_SRC_ALPHA;
			case BlendFactor::Zero:
				return VK_BLEND_FACTOR_ZERO;
			default:
				return (VkBlendFactor)0;
			}
		}
	};

	public value struct BlendEqn {
		property BlendFactor DestFactor;
		property BlendFactor SrcFactor;
		property BlendOp Op;
	};

	ref class SpecializedShaderModule;
	ref class GraphicsPipeline
	{
	private:
		VkPipeline pipeline;
		VkPipelineLayout pipelineLayout;
		List<SpecializedShaderModule^>^ shaders;
		bool locked;
	internal:
	public:
		property TopologyType Topology;
		property bool RasterizerDiscard;
		property float LineWidth;
		property CullMode Cull;
		property BlendEqn^ ColorBlend;
		property BlendEqn^ AlphaBlend;
		property FillMode Fill;

		GraphicsPipeline();
		void SetShader(SpecializedShaderModule^ s);

		void Build(uint32_t w, uint32_t h);
	};
}

