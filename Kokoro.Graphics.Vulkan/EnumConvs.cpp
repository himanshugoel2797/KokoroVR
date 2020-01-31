#include "MemoryUsage.h"
#include "SharingMode.h"
#include "vk_mem_alloc.h"

uint32_t Kokoro::Graphics::MemoryUsageConv::Convert(MemoryUsage s) {
	switch (s) {
	case MemoryUsage::CpuOnly:
		return VMA_MEMORY_USAGE_CPU_ONLY;
	case MemoryUsage::GpuOnly:
		return VMA_MEMORY_USAGE_GPU_ONLY;
	case MemoryUsage::CpuToGpu:
		return VMA_MEMORY_USAGE_CPU_TO_GPU;
	default:
		return 0;
	}
}

uint32_t Kokoro::Graphics::SharingModeConv::Convert(SharingMode s){
	switch (s) {
	case SharingMode::Exclusive:
		return VK_SHARING_MODE_EXCLUSIVE;
	case SharingMode::Shared:
		return VK_SHARING_MODE_CONCURRENT;
	default:
		return 0;
	}
}