#pragma once
#include "GraphicsDevice.h"
#include "ShaderType.h"
#include "ImageView.h"
#include "Sampler.h"
#include "GPUBuffer.h"

using namespace System::Collections::Generic;

namespace Kokoro::Graphics {
	enum class DescriptorType {
		Sampler,
		CombinedImageSampler,
		SampledImage,
		StorageImage,
		UniformTexelBuffer,
		StorageTexelBuffer,
		UniformBuffer,
		StorageBuffer,
		InputAttachment
	};
	inline DescriptorType operator |(DescriptorType lhs, DescriptorType rhs)
	{
		return static_cast<DescriptorType>(static_cast<char>(lhs) | static_cast<char>(rhs));
	}
	inline DescriptorType& operator |= (DescriptorType& lhs, DescriptorType rhs)
	{
		lhs = lhs | rhs;
		return lhs;
	}
	inline DescriptorType operator &(DescriptorType lhs, DescriptorType rhs)
	{
		return static_cast<DescriptorType>(static_cast<char>(lhs)& static_cast<char>(rhs));
	}
	inline DescriptorType& operator &= (DescriptorType& lhs, DescriptorType rhs)
	{
		lhs = lhs & rhs;
		return lhs;
	}
	class DescriptorTypeConv {
	public:
		static VkDescriptorType Convert(DescriptorType s) {
			switch (s) {
			case DescriptorType::Sampler:
				return VK_DESCRIPTOR_TYPE_SAMPLER;
			case DescriptorType::CombinedImageSampler:
				return VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;
			case DescriptorType::SampledImage:
				return VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE;
			case DescriptorType::StorageImage:
				return VK_DESCRIPTOR_TYPE_STORAGE_IMAGE;
			case DescriptorType::UniformTexelBuffer:
				return VK_DESCRIPTOR_TYPE_UNIFORM_TEXEL_BUFFER;
			case DescriptorType::StorageTexelBuffer:
				return VK_DESCRIPTOR_TYPE_STORAGE_TEXEL_BUFFER;
			case DescriptorType::UniformBuffer:
				return VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER;
			case DescriptorType::StorageBuffer:
				return VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;
			case DescriptorType::InputAttachment:
				return VK_DESCRIPTOR_TYPE_INPUT_ATTACHMENT;
			default:
				return (VkDescriptorType)0;
			}
		}
	};

	ref class DescriptorSet
	{
	private:
		ref struct DescriptorLayout {
		public:
			property int BindingIndex;
			property DescriptorType Type;
			property int Count;
			property ShaderType Stages;
		};
		ref struct PoolEntry {
		public:
			property DescriptorType Type;
			property int Count;
		};
		
		List<DescriptorLayout^>^ layouts;
		List<PoolEntry^>^ pool_entries;

		bool locked;
		VkDescriptorSetLayout desc_set_layout;
		VkDescriptorPool desc_pool;
		VkDescriptorSet* sets;
		int set_cnt;
	internal:
		VkDescriptorSetLayout GetLayout();
		VkDescriptorPool GetPool();
		VkDescriptorSet GetSet(int idx);
		int GetSetCount();
	public:
		DescriptorSet();
		~DescriptorSet();
		void Add(int bindingIndex, DescriptorType type, int count, ShaderType stages);
		void Build(int pool_sz);
		void Set(int set, int binding, int idx, ImageView^ img, Sampler^ sampler);
		void Set(int set, int binding, int idx, GPUBuffer^ buf, size_t off, size_t len);
		void SetImageView(int set, int binding, int idx, ImageView^ img, bool rw);
		void SetBufferView(int set, int binding, int idx, GPUBuffer^ buf);
	};
}

