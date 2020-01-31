#pragma once
#include "GraphicsDevice.h"
#include "ImageFormat.h"

using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;

namespace Kokoro::Graphics {
	enum class ImageLayout {
		Undefined = VK_IMAGE_LAYOUT_UNDEFINED,
		General = VK_IMAGE_LAYOUT_GENERAL,
		ColorAttachmentOptimal = VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
		DepthAttachmentOptimal = VK_IMAGE_LAYOUT_DEPTH_ATTACHMENT_OPTIMAL_KHR,
		DepthReadOnlyOptimal = VK_IMAGE_LAYOUT_DEPTH_READ_ONLY_OPTIMAL_KHR,
		ShaderReadOnlyOptimal = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
		TransferSrcOptimal = VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
		TransferDstOptimal = VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
		Preinitialized = VK_IMAGE_LAYOUT_PREINITIALIZED,
		PresentSrc = VK_IMAGE_LAYOUT_PRESENT_SRC_KHR,
	};

	class ImageLayoutConv {
	public:
		static VkImageLayout Convert(ImageLayout i) {
			return static_cast<VkImageLayout>(i);
		}
	};

	enum class StoreOp {
		Store = VK_ATTACHMENT_STORE_OP_STORE,
		DontCare = VK_ATTACHMENT_STORE_OP_DONT_CARE,
	};

	class StoreOpConv {
	public:
		static VkAttachmentStoreOp Convert(StoreOp o) {
			return static_cast<VkAttachmentStoreOp>(o);
		}
	};

	enum class LoadOp {
		Load = VK_ATTACHMENT_LOAD_OP_LOAD,
		Clear = VK_ATTACHMENT_LOAD_OP_CLEAR,
		DontCare = VK_ATTACHMENT_LOAD_OP_DONT_CARE,
	};

	class LoadOpConv {
	public:
		static VkAttachmentLoadOp Convert(LoadOp o) {
			return static_cast<VkAttachmentLoadOp>(o);
		}
	};

	enum class PipelineStage {

	};

	enum class AccessFlag {

	};

	enum class DependencyFlag {

	};

	ref class RenderPass
	{
	public:
		value struct AttachmentInfo {
		public:
			StoreOp store;
			LoadOp load;
			ImageFormat fmt;
			ImageLayout initLayout;
			ImageLayout finLayout;
		};

		[StructLayout(LayoutKind::Sequential)]
		value struct AttachmentRef {
		public:
			uint32_t idx;
			ImageLayout layout;
		};

		value struct SubpassInfo {
		public:
			array<AttachmentRef^>^ inputAttachments;
			array<AttachmentRef^>^ colorAttachments;
			array<uint32_t>^ preserveAttachments;
			AttachmentRef^ depthAttachment;
		};

		value struct SubpassDep{
		public:
			uint32_t src;
			uint32_t dst;
			PipelineStage srcStage;
			PipelineStage dstStage;
			AccessFlag srcMask;
			AccessFlag dstMask;
			DependencyFlag depFlags;
		};

	private:
		List<AttachmentInfo>^ attachments;
		List<SubpassInfo>^ subpasses;
		List<SubpassDep>^ subpassDeps;
		bool locked;
	public:
		RenderPass();
		~RenderPass();

		void AddSubpass(SubpassInfo info);
		void AddSubpassDep(SubpassDep dep);
		void AddAttachment(AttachmentInfo att);
		void Build();
	};
}

