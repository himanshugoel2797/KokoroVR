#pragma once
#include "GraphicsDevice.h"
#include "ShaderType.h"

using namespace System::Collections::Generic;

namespace Kokoro::Graphics {
	ref class ShaderModule;
	ref class SpecializedShaderModule
	{
	internal:
		ShaderType sType;
		SpecializedShaderModule(Kokoro::Graphics::ShaderModule^ sMod, IntPtr specData, size_t specSz);
		VkPipelineShaderStageCreateInfo* GetCreateInfo();
		~SpecializedShaderModule();
	private:
		VkSpecializationInfo* specInfo;
		VkSpecializationMapEntry* specMap;
		int specializationInfoCnt;
		IntPtr entryPoint;
		VkPipelineShaderStageCreateInfo* creatInfo;
	public:
	};
}
