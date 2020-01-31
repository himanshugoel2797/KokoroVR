#pragma once
#include <stdint.h>

namespace Kokoro::Graphics {
	enum class MemoryUsage {
		CpuToGpu,
		GpuOnly,
		CpuOnly,
	};

	class MemoryUsageConv {
	public:
		static uint32_t Convert(MemoryUsage s);
	};
}