#include "GraphicsPipeline.h"
#include "SpecializedShaderModule.h"

Kokoro::Graphics::GraphicsPipeline::GraphicsPipeline() {
	shaders = gcnew List<SpecializedShaderModule^>();
	Topology = TopologyType::Triangle;
	RasterizerDiscard = false;
	LineWidth = 1;
	Cull = CullMode::None;

	ColorBlend = gcnew BlendEqn();
	ColorBlend->DestFactor = BlendFactor::OneMinusSourceAlpha;
	ColorBlend->SrcFactor = BlendFactor::SourceAlpha;
	ColorBlend->Op = BlendOp::Add;

	AlphaBlend = gcnew BlendEqn();
	AlphaBlend->DestFactor = BlendFactor::OneMinusSourceAlpha;
	AlphaBlend->SrcFactor = BlendFactor::SourceAlpha;
	AlphaBlend->Op = BlendOp::Add;

	Fill = FillMode::Fill;

	locked = false;
}

void Kokoro::Graphics::GraphicsPipeline::SetShader(SpecializedShaderModule^ s) {
	shaders->Add(s);
}

void Kokoro::Graphics::GraphicsPipeline::Build(uint32_t w, uint32_t h) {
	if (!locked) {
		VkPipelineVertexInputStateCreateInfo vInputCreatInfo = {};
		vInputCreatInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO;
		vInputCreatInfo.vertexBindingDescriptionCount = 0;
		vInputCreatInfo.pVertexBindingDescriptions = nullptr;
		vInputCreatInfo.vertexAttributeDescriptionCount = 0;
		vInputCreatInfo.pVertexAttributeDescriptions = nullptr;

		VkPipelineInputAssemblyStateCreateInfo iaCreatInfo = {};
		iaCreatInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO;
		iaCreatInfo.flags = 0;
		iaCreatInfo.topology = TopologyTypeConv::Convert(Topology);
		iaCreatInfo.primitiveRestartEnable = VK_FALSE;

		VkViewport vport = {};
		vport.x = 0.0f;
		vport.y = 0.0f;
		vport.width = static_cast<float>(w);
		vport.height = static_cast<float>(h);
		vport.minDepth = 0.0f;
		vport.maxDepth = 1.0f;

		VkRect2D scissor = {};
		scissor.offset = { 0,0 };
		scissor.extent = {
			w, h
		};

		VkPipelineViewportStateCreateInfo vportCreatInfo = {};
		vportCreatInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_VIEWPORT_STATE_CREATE_INFO;
		vportCreatInfo.viewportCount = 1;
		vportCreatInfo.pViewports = &vport;
		vportCreatInfo.scissorCount = 1;
		vportCreatInfo.pScissors = &scissor;

		VkPipelineRasterizationStateCreateInfo rasCreatInfo = {};
		rasCreatInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_RASTERIZATION_STATE_CREATE_INFO;
		rasCreatInfo.depthClampEnable = VK_FALSE;
		rasCreatInfo.rasterizerDiscardEnable = RasterizerDiscard ? VK_TRUE : VK_FALSE;
		rasCreatInfo.polygonMode = FillModeConv::Convert(Fill);
		rasCreatInfo.lineWidth = LineWidth;
		rasCreatInfo.cullMode = CullModeConv::Convert(Cull);
		rasCreatInfo.frontFace = VK_FRONT_FACE_COUNTER_CLOCKWISE;
		rasCreatInfo.depthBiasEnable = VK_FALSE;
		rasCreatInfo.depthBiasConstantFactor = 0.0f;
		rasCreatInfo.depthBiasClamp = 0.0f;
		rasCreatInfo.depthBiasSlopeFactor = 0.0f;

		VkPipelineMultisampleStateCreateInfo msCreatInfo = {};
		msCreatInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_MULTISAMPLE_STATE_CREATE_INFO;
		msCreatInfo.sampleShadingEnable = VK_FALSE;
		msCreatInfo.rasterizationSamples = VK_SAMPLE_COUNT_1_BIT;
		msCreatInfo.minSampleShading = 1.0f;
		msCreatInfo.pSampleMask = nullptr;
		msCreatInfo.alphaToCoverageEnable = VK_FALSE;
		msCreatInfo.alphaToOneEnable = VK_FALSE;

		VkPipelineDepthStencilStateCreateInfo dpthCreatInfo = {};
		dpthCreatInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_DEPTH_STENCIL_STATE_CREATE_INFO;

		VkPipelineColorBlendAttachmentState colCreatInfo = {};
		colCreatInfo.alphaBlendOp = BlendOpConv::Convert(AlphaBlend->Op);
		colCreatInfo.srcAlphaBlendFactor = BlendFactorConv::Convert(AlphaBlend->SrcFactor);
		colCreatInfo.dstAlphaBlendFactor = BlendFactorConv::Convert(AlphaBlend->DestFactor);
		colCreatInfo.colorBlendOp = BlendOpConv::Convert(ColorBlend->Op);
		colCreatInfo.srcColorBlendFactor = BlendFactorConv::Convert(ColorBlend->SrcFactor);
		colCreatInfo.dstColorBlendFactor = BlendFactorConv::Convert(ColorBlend->DestFactor);
		colCreatInfo.colorWriteMask = VK_COLOR_COMPONENT_R_BIT | VK_COLOR_COMPONENT_G_BIT | VK_COLOR_COMPONENT_B_BIT | VK_COLOR_COMPONENT_A_BIT;
		colCreatInfo.blendEnable = VK_TRUE;

		VkPipelineColorBlendStateCreateInfo colBlendCreatInfo = {};
		colBlendCreatInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_COLOR_BLEND_STATE_CREATE_INFO;

		VkDynamicState dynState = VK_DYNAMIC_STATE_VIEWPORT;
		VkPipelineDynamicStateCreateInfo dynCreatInfo = {};
		dynCreatInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_DYNAMIC_STATE_CREATE_INFO;
		dynCreatInfo.dynamicStateCount = 1;
		dynCreatInfo.pDynamicStates = &dynState;

		VkPipelineLayoutCreateInfo pipelineCreatInfo = {};
		pipelineCreatInfo.sType = VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO;
		//pipelineCreatInfo.s
	}
}