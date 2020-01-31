#pragma once
#include "GraphicsDevice.h"
#include "ImageView.h"

using namespace System::Collections::Generic;

namespace Kokoro::Graphics {
	ref class Framebuffer
	{
	private:
		List<ImageView^>^ Images;
	public:
		Framebuffer();
		~Framebuffer();

		void AddAttachment();
	};
}

