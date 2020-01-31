#pragma once
#include "GraphicsDevice.h"

namespace Kokoro::Graphics {
	public enum class TopologyType {
		Triangle,
		TriangleStrip,
		Line,
		LineStrip,
		Point
	};

	public ref class TopologyTypeConv {
	public:
		static VkPrimitiveTopology Convert(TopologyType s) {
			switch (s) {
			case TopologyType::Line:
				return VK_PRIMITIVE_TOPOLOGY_LINE_LIST;
			case TopologyType::LineStrip:
				return VK_PRIMITIVE_TOPOLOGY_LINE_STRIP;
			case TopologyType::Point:
				return VK_PRIMITIVE_TOPOLOGY_POINT_LIST;
			case TopologyType::Triangle:
				return VK_PRIMITIVE_TOPOLOGY_TRIANGLE_LIST;
			case TopologyType::TriangleStrip:
				return VK_PRIMITIVE_TOPOLOGY_TRIANGLE_STRIP;
			default:
				throw gcnew System::Exception("Unexpected ShaderType value.");
			}
		}
	};
}