#include "ShaderModule.h"
#include "SpecializedShaderModule.h"
#include "GraphicsDevice.h"

using namespace System::IO;

Kokoro::Graphics::ShaderModule::ShaderModule(ShaderType sType, String^ fname) {
	specializationDef = gcnew List<SpecializationInfo>();
	EntryPoint = "main";

	spv = File::ReadAllBytes(fname);
	pin_ptr<unsigned char> spv_ptr = &spv[0];

	VkShaderModuleCreateInfo smCreatInfo = {};
	smCreatInfo.sType = VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO;
	smCreatInfo.codeSize = spv->Length / 4;
	smCreatInfo.pCode = (uint32_t*)spv_ptr;

	pin_ptr<VkShaderModule> shaderModule_ptr = &shaderModule;

	if (vkCreateShaderModule(GraphicsDevice::GetDevice(), &smCreatInfo, nullptr, shaderModule_ptr) != VK_SUCCESS)
		throw gcnew System::Exception("Shader failed to load!");
}

void Kokoro::Graphics::ShaderModule::DefineSpecializationConstant(uint32_t id, uint32_t offset, size_t size) {
	SpecializationInfo v;
	v.id = id;
	v.offset = offset;
	v.sz = size;
	specializationDef->Add(v);
}

Kokoro::Graphics::SpecializedShaderModule^ Kokoro::Graphics::ShaderModule::Specialize(IntPtr ptr, size_t sz) {
	return gcnew SpecializedShaderModule(this, ptr, sz);
}

Kokoro::Graphics::ShaderModule::~ShaderModule() {
	vkDestroyShaderModule(GraphicsDevice::GetDevice(), shaderModule, nullptr);
}