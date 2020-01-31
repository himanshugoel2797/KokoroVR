#include "SpecializedShaderModule.h"
#include "ShaderModule.h"

using namespace System::Runtime::InteropServices;

Kokoro::Graphics::SpecializedShaderModule::SpecializedShaderModule(ShaderModule^ sMod, IntPtr specData, size_t specSz) {
	creatInfo = new VkPipelineShaderStageCreateInfo();
	creatInfo->sType = VK_STRUCTURE_TYPE_PIPELINE_SHADER_STAGE_CREATE_INFO;
	creatInfo->pNext = nullptr;
	creatInfo->flags = sMod->RequireFullSubgroups ? VK_PIPELINE_SHADER_STAGE_CREATE_REQUIRE_FULL_SUBGROUPS_BIT_EXT : VK_PIPELINE_SHADER_STAGE_CREATE_ALLOW_VARYING_SUBGROUP_SIZE_BIT_EXT;
	creatInfo->stage = ShaderTypeConv::Convert(sMod->sType);
	creatInfo->module = sMod->shaderModule;

	sType = sMod->sType;
	entryPoint = Marshal::StringToHGlobalAnsi(sMod->EntryPoint);
	creatInfo->pName = (const char*)entryPoint.ToPointer();

	if (specData != IntPtr::Zero && sMod->specializationDef->Count != 0) {
		specializationInfoCnt = sMod->specializationDef->Count;
		specMap = new VkSpecializationMapEntry[specializationInfoCnt];
		for (int i = 0; i < specializationInfoCnt; i++) {
			specMap[i].constantID = sMod->specializationDef[i].id;
			specMap[i].offset = sMod->specializationDef[i].offset;
			specMap[i].size = sMod->specializationDef[i].sz;
		}
		specInfo = new VkSpecializationInfo;
		specInfo->mapEntryCount = specializationInfoCnt;
		specInfo->pMapEntries = specMap;
		specInfo->dataSize = specSz;
		specInfo->pData = specData.ToPointer();

		creatInfo->pSpecializationInfo = specInfo;
	}
	else {
		creatInfo->pSpecializationInfo = nullptr;
		specializationInfoCnt = 0;
	}
}

VkPipelineShaderStageCreateInfo* Kokoro::Graphics::SpecializedShaderModule::GetCreateInfo() {
	return creatInfo;
}

Kokoro::Graphics::SpecializedShaderModule::~SpecializedShaderModule() {
	if (specializationInfoCnt != 0) {
		delete specInfo;
		delete[] specMap;
	}
	Marshal::FreeHGlobal(entryPoint);
	delete creatInfo;
}