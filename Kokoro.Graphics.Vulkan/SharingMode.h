#pragma once
#include <stdint.h>

namespace Kokoro::Graphics {
	enum class SharingMode {
		Shared,
		Exclusive
	};
	class SharingModeConv {
	public:
		static uint32_t Convert(SharingMode s);
	};
}