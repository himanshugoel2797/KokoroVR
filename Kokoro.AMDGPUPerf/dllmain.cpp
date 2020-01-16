// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include "GPAInterfaceLoader.h"

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

#define FEXPORT extern "C" __declspec(dllexport)

GPAApiManager* GPAApiManager::m_pGpaApiManager = nullptr;
GPA_ContextId contextId;
GPAFunctionTable* gpaFuncTable = nullptr;
GPAFuncTableInfo* g_pFuncTableInfo = nullptr;

FEXPORT bool InitializeGPA() {
	bool retVal = false;

	if (GPA_STATUS_OK == GPAApiManager::Instance()->LoadApi(GPA_API_OPENGL)) {
		gpaFuncTable = GPAApiManager::Instance()->GetFunctionTable(GPA_API_OPENGL);

		if (gpaFuncTable != nullptr) {
			retVal = GPA_STATUS_OK == gpaFuncTable->GPA_Initialize(GPA_INITIALIZE_DEFAULT_BIT);
		}
	}
	return retVal;
}

FEXPORT void DestroyGPA() {
	gpaFuncTable->GPA_Destroy();
}

FEXPORT void GLOpenContextGPA(HGLRC gl_context_hndl) {
	gpaFuncTable->GPA_OpenContext(gl_context_hndl, GPA_OPENCONTEXT_ENABLE_HARDWARE_COUNTERS_BIT | GPA_OPENCONTEXT_DEFAULT_BIT, &contextId);
}

FEXPORT void GLCloseContextGPA() {
	gpaFuncTable->GPA_CloseContext(contextId);
}

FEXPORT int GetSupportedSampleTypesGPA(uint64_t* bits) {
	return gpaFuncTable->GPA_GetSupportedSampleTypes(contextId, (GPA_ContextSampleTypeFlags*)bits);
}

FEXPORT int GetNumCountersGPA(uint32_t* cnt) {
	return gpaFuncTable->GPA_GetNumCounters(contextId, cnt);
}

FEXPORT int GetCounterNameGPA(uint32_t idx, const char** name) {
	return gpaFuncTable->GPA_GetCounterName(contextId, idx, name);
}

FEXPORT int GetCounterIndexGPA(const char* name, uint32_t* idx) {
	return gpaFuncTable->GPA_GetCounterIndex(contextId, name, idx);
}

FEXPORT int GetCounterGroupGPA(uint32_t idx, const char** name) {
	return gpaFuncTable->GPA_GetCounterGroup(contextId, idx, name);
}

FEXPORT int GetCounterDescriptionGPA(uint32_t idx, const char** name) {
	return gpaFuncTable->GPA_GetCounterDescription(contextId, idx, name);
}

FEXPORT int GetCounterDataTypeGPA(uint32_t idx, uint32_t* flags) {
	return gpaFuncTable->GPA_GetCounterDataType(contextId, idx, (GPA_Data_Type*)flags);
}

FEXPORT int GetCounterUsageTypeGPA(uint32_t idx, uint32_t* result) {
	return gpaFuncTable->GPA_GetCounterUsageType(contextId, idx, (GPA_Usage_Type*)result);
}

FEXPORT int GetCounterSampleTypeGPA(uint32_t idx, uint32_t* result) {
	return gpaFuncTable->GPA_GetCounterSampleType(contextId, idx, (GPA_Counter_Sample_Type*)result);
}

FEXPORT int CreateSessionGPA(uint64_t* session) {
	return gpaFuncTable->GPA_CreateSession(contextId, GPA_SESSION_SAMPLE_TYPE_DISCRETE_COUNTER, (GPA_SessionId*)session);
}

FEXPORT int DeleteSessionGPA(uint64_t session) {
	return gpaFuncTable->GPA_DeleteSession((GPA_SessionId)session);
}

FEXPORT int BeginSessionGPA(uint64_t session) {
	return gpaFuncTable->GPA_BeginSession((GPA_SessionId)session);
}

FEXPORT int EndSessionGPA(uint64_t session) {
	return gpaFuncTable->GPA_EndSession((GPA_SessionId)session);
}

FEXPORT int EnableCounterByNameGPA(uint64_t session, const char* name) {
	return gpaFuncTable->GPA_EnableCounterByName((GPA_SessionId)session, name);
}

FEXPORT int DisableCounterByNameGPA(uint64_t session, const char* name) {
	return gpaFuncTable->GPA_DisableCounterByName((GPA_SessionId)session, name);
}

FEXPORT int EnableAllCountersGPA(uint64_t session) {
	return gpaFuncTable->GPA_EnableAllCounters((GPA_SessionId)session);
}

FEXPORT int DisableAllCountersGPA(uint64_t session) {
	return gpaFuncTable->GPA_DisableAllCounters((GPA_SessionId)session);
}

FEXPORT int GetPassCountGPA(uint64_t session, uint32_t* cnt) {
	return gpaFuncTable->GPA_GetPassCount((GPA_SessionId)session, cnt);
}

FEXPORT int GetNumEnabledCountersGPA(uint64_t session, uint32_t* cnt) {
	return gpaFuncTable->GPA_GetNumEnabledCounters((GPA_SessionId)session, cnt);
}

FEXPORT int GetEnabledIndexGPA(uint64_t session, uint32_t enabled_num, uint32_t* idx) {
	return gpaFuncTable->GPA_GetEnabledIndex((GPA_SessionId)session, enabled_num, idx);
}

FEXPORT int IsCounterEnabledGPA(uint64_t session, uint32_t idx) {
	return gpaFuncTable->GPA_IsCounterEnabled((GPA_SessionId)session, idx);
}

FEXPORT int GLBeginCommandListGPA(uint64_t session, uint32_t pass_idx, uint64_t* cmd_list_id) {
	return gpaFuncTable->GPA_BeginCommandList((GPA_SessionId)session, pass_idx, GPA_NULL_COMMAND_LIST, GPA_COMMAND_LIST_NONE, (GPA_CommandListId*)cmd_list_id);
}

FEXPORT int GLEndCommandListGPA(uint64_t cmd_list_id) {
	return gpaFuncTable->GPA_EndCommandList((GPA_CommandListId)cmd_list_id);
}

FEXPORT int BeginSampleGPA(uint32_t sample_id, uint64_t cmd_list_id) {
	return gpaFuncTable->GPA_BeginSample(sample_id, (GPA_CommandListId)cmd_list_id);
}

FEXPORT int EndSampleGPA(uint64_t cmd_list_id) {
	return gpaFuncTable->GPA_EndSample((GPA_CommandListId)cmd_list_id);
}

FEXPORT int GetSampleCountGPA(uint64_t session, uint32_t* sample_cnt) {
	return gpaFuncTable->GPA_GetSampleCount((GPA_SessionId)session, sample_cnt);
}

FEXPORT int IsPassCompleteGPA(uint64_t session, uint32_t idx) {
	return gpaFuncTable->GPA_IsPassComplete((GPA_SessionId)session, idx);
}

FEXPORT int IsSessionCompleteGPA(uint64_t session) {
	return gpaFuncTable->GPA_IsSessionComplete((GPA_SessionId)session);
}

FEXPORT int GetSampleResultSizeGPA(uint64_t session, uint32_t id, uint64_t* resultSz) {
	return gpaFuncTable->GPA_GetSampleResultSize((GPA_SessionId)session, id, (size_t*)resultSz);
}

FEXPORT int GetSampleResultGPA(uint64_t session, uint32_t id, uint64_t sample_res_size, uint64_t* sample_res) {
	return gpaFuncTable->GPA_GetSampleResult((GPA_SessionId)session, id, (size_t)sample_res_size, sample_res);
}