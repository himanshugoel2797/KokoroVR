#pragma once
#include "GraphicsDevice.h"
#include "ShaderType.h"
using namespace System;
using namespace System::Collections::Generic;

namespace Kokoro::Graphics {
	ref class SpecializedShaderModule;
	ref class ShaderModule
	{
	internal:
		value struct SpecializationInfo {
			uint32_t id;
			uint32_t offset;
			size_t sz;
		};
		VkShaderModule shaderModule;
		ShaderType sType;
		List<SpecializationInfo>^ specializationDef;
	private:
		array<unsigned char>^ spv;
	public:
		property String^ EntryPoint;
		property bool RequireFullSubgroups;

		ShaderModule(ShaderType sType, String^ fname);
		~ShaderModule();
		void DefineSpecializationConstant(uint32_t id, uint32_t offset, size_t size);
		SpecializedShaderModule^ Specialize(IntPtr ptr, size_t sz);
	};
}

