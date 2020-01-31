#include "RenderPass.h"
#include <vector>

Kokoro::Graphics::RenderPass::RenderPass()
{
	attachments = gcnew List<AttachmentInfo>();
	subpasses = gcnew List<SubpassInfo>();
	subpassDeps = gcnew List<SubpassDep>();
	locked = false;
}

Kokoro::Graphics::RenderPass::~RenderPass()
{

}

void Kokoro::Graphics::RenderPass::AddSubpass(SubpassInfo att)
{
	subpasses->Add(att);
}

void Kokoro::Graphics::RenderPass::AddSubpassDep(SubpassDep att)
{
	subpassDeps->Add(att);
}

void Kokoro::Graphics::RenderPass::AddAttachment(AttachmentInfo att)
{
	attachments->Add(att);
}

void Kokoro::Graphics::RenderPass::Build()
{
	if (!locked) {
		VkRenderPassCreateInfo rpassCreatInfo = {};
		rpassCreatInfo.sType = VK_STRUCTURE_TYPE_RENDER_PASS_CREATE_INFO;

		std::vector<VkAttachmentDescription> att(attachments->Count);
		for (int i = 0; i < attachments->Count; i++) {
			att[i].flags = 0;
			att[i].format = ImageFormatConv::Convert(attachments[i].fmt);
			att[i].samples = VK_SAMPLE_COUNT_1_BIT;
			att[i].loadOp = LoadOpConv::Convert(attachments[i].load);
			att[i].storeOp = StoreOpConv::Convert(attachments[i].store);
			att[i].stencilLoadOp = VK_ATTACHMENT_LOAD_OP_DONT_CARE;
			att[i].stencilStoreOp = VK_ATTACHMENT_STORE_OP_DONT_CARE;
			att[i].initialLayout = ImageLayoutConv::Convert(attachments[i].initLayout);
			att[i].finalLayout = ImageLayoutConv::Convert(attachments[i].finLayout);
		}
		rpassCreatInfo.attachmentCount = static_cast<uint32_t>(attachments->Count);
		rpassCreatInfo.pAttachments = att.data();

		std::vector<VkSubpassDescription> sbpass(subpasses->Count);
		for (int i = 0; i < subpasses->Count; i++) {

		}
		rpassCreatInfo.subpassCount = static_cast<uint32_t>(subpasses->Count);
		rpassCreatInfo.pSubpasses = sbpass.data();

		std::vector<VkSubpassDependency> sbpassDep(subpassDeps->Count);
		for (int i = 0; i < subpassDeps->Count; i++) {

		}
		rpassCreatInfo.dependencyCount = static_cast<uint32_t>(subpassDeps->Count);
		rpassCreatInfo.pDependencies = sbpassDep.data();

		locked = true;
	}
}
