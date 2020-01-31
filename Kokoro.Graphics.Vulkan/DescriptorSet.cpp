#include "DescriptorSet.h"
#include <vector>

VkDescriptorSetLayout Kokoro::Graphics::DescriptorSet::GetLayout()
{
	return desc_set_layout;
}

VkDescriptorPool Kokoro::Graphics::DescriptorSet::GetPool()
{
	return desc_pool;
}

VkDescriptorSet Kokoro::Graphics::DescriptorSet::GetSet(int idx)
{
	if (idx >= set_cnt)
		throw gcnew System::IndexOutOfRangeException("idx is out of range.");
	return sets[idx];
}

int Kokoro::Graphics::DescriptorSet::GetSetCount()
{
	return set_cnt;
}

Kokoro::Graphics::DescriptorSet::DescriptorSet() {
	layouts = gcnew List<DescriptorLayout^>();
	pool_entries = gcnew List<PoolEntry^>();
	locked = false;
}

Kokoro::Graphics::DescriptorSet::~DescriptorSet() {
	if (locked) {
		delete[] sets;
		vkDestroyDescriptorPool(GraphicsDevice::GetDevice(), desc_pool, nullptr);
		vkDestroyDescriptorSetLayout(GraphicsDevice::GetDevice(), desc_set_layout, nullptr);
		delete layouts;
		delete pool_entries;
	}
}

void Kokoro::Graphics::DescriptorSet::Add(int bindingIndex, DescriptorType type, int count, ShaderType stage) {
	auto l = gcnew DescriptorLayout;
	l->BindingIndex = bindingIndex;
	l->Type = type;
	l->Count = count;
	l->Stages = stage;
	layouts->Add(l);

	int i = 0;
	for (; i < pool_entries->Count; i++)
		if (pool_entries[i]->Type == type) {
			pool_entries[i]->Count++;
			break;
		}
	if (i == pool_entries->Count) {
		auto p = gcnew PoolEntry;
		p->Count = 1;
		p->Type = type;
		pool_entries->Add(p);
	}
}

void Kokoro::Graphics::DescriptorSet::Build(int pool_sz)
{
	if (!locked) {
		std::vector<VkDescriptorSetLayoutBinding> bindings(layouts->Count);
		for (int i = 0; i < layouts->Count; i++) {
			bindings[i].binding = static_cast<uint32_t>(layouts[i]->BindingIndex);
			bindings[i].descriptorCount = static_cast<uint32_t>(layouts[i]->Count);
			bindings[i].descriptorType = DescriptorTypeConv::Convert(layouts[i]->Type);
			bindings[i].stageFlags = ShaderTypeConv::Convert(layouts[i]->Stages);
			bindings[i].pImmutableSamplers = nullptr;
		}

		VkDescriptorSetLayoutCreateInfo creatInfo = {};
		creatInfo.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO;
		creatInfo.flags = 0;
		creatInfo.bindingCount = static_cast<uint32_t>(bindings.size());
		creatInfo.pBindings = bindings.data();

		pin_ptr<VkDescriptorSetLayout> desc_set_layout_ptr = &desc_set_layout;
		if (vkCreateDescriptorSetLayout(GraphicsDevice::GetDevice(), &creatInfo, nullptr, desc_set_layout_ptr) != VK_SUCCESS)
			throw gcnew System::Exception("Failed to create descriptor set.");

		std::vector<VkDescriptorPoolSize> psize(pool_entries->Count);
		for (int i = 0; i < pool_entries->Count; i++) {
			psize[i].type = DescriptorTypeConv::Convert(pool_entries[i]->Type);
			psize[i].descriptorCount = static_cast<uint32_t>(pool_entries[i]->Count);
		}

		VkDescriptorPoolCreateInfo poolCreatInfo = {};
		poolCreatInfo.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO;
		poolCreatInfo.flags = 0;
		poolCreatInfo.maxSets = static_cast<uint32_t>(pool_sz);
		poolCreatInfo.poolSizeCount = static_cast<uint32_t>(psize.size());
		poolCreatInfo.pPoolSizes = psize.data();

		pin_ptr<VkDescriptorPool> desc_pool_ptr = &desc_pool;
		if (vkCreateDescriptorPool(GraphicsDevice::GetDevice(), &poolCreatInfo, nullptr, desc_pool_ptr) != VK_SUCCESS)
			throw gcnew System::Exception("Failed to create descriptor pool.");

		set_cnt = pool_sz;
		std::vector<VkDescriptorSetLayout> layout_sets(set_cnt);
		for (int i = 0; i < set_cnt; i++)
			layout_sets[i] = desc_set_layout;

		VkDescriptorSetAllocateInfo desc_set_alloc_info = {};
		desc_set_alloc_info.sType = VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO;
		desc_set_alloc_info.descriptorPool = desc_pool;
		desc_set_alloc_info.descriptorSetCount = static_cast<uint32_t>(set_cnt);
		desc_set_alloc_info.pSetLayouts = layout_sets.data();

		sets = new VkDescriptorSet[set_cnt];
		if (vkAllocateDescriptorSets(GraphicsDevice::GetDevice(), &desc_set_alloc_info, sets) != VK_SUCCESS)
			throw gcnew System::Exception("Failed to allocate descriptor sets.");

		locked = true;
	}
}

void Kokoro::Graphics::DescriptorSet::Set(int set, int binding, int idx, ImageView^ img, Sampler^ sampler)
{
	if (set >= set_cnt)
		throw gcnew System::IndexOutOfRangeException("set is out of range.");

	VkDescriptorImageInfo img_info = {};
	img_info.sampler = sampler->GetSampler();
	img_info.imageView = img->GetImageView();
	img_info.imageLayout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;

	VkWriteDescriptorSet desc_write = {};
	desc_write.sType = VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;
	desc_write.dstSet = sets[set];
	desc_write.dstBinding = static_cast<uint32_t>(binding);
	desc_write.dstArrayElement = static_cast<uint32_t>(idx);
	desc_write.descriptorCount = 1;
	desc_write.pImageInfo = &img_info;
	desc_write.pBufferInfo = nullptr;
	desc_write.pTexelBufferView = nullptr;

	vkUpdateDescriptorSets(GraphicsDevice::GetDevice(), 1, &desc_write, 0, nullptr);
}

void Kokoro::Graphics::DescriptorSet::SetImageView(int set, int binding, int idx, ImageView^ img, bool rw)
{
	if (set >= set_cnt)
		throw gcnew System::IndexOutOfRangeException("set is out of range.");

	VkDescriptorImageInfo img_info = {};
	img_info.sampler = nullptr;
	img_info.imageView = img->GetImageView();
	img_info.imageLayout = VK_IMAGE_LAYOUT_GENERAL;

	VkWriteDescriptorSet desc_write = {};
	desc_write.sType = VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;
	desc_write.dstSet = sets[set];
	desc_write.dstBinding = static_cast<uint32_t>(binding);
	desc_write.dstArrayElement = static_cast<uint32_t>(idx);
	desc_write.descriptorCount = 1;
	desc_write.pImageInfo = &img_info;
	desc_write.pBufferInfo = nullptr;
	desc_write.pTexelBufferView = nullptr;

	vkUpdateDescriptorSets(GraphicsDevice::GetDevice(), 1, &desc_write, 0, nullptr);
}

void Kokoro::Graphics::DescriptorSet::Set(int set, int binding, int idx, GPUBuffer^ buf, size_t off, size_t len)
{
	if (set >= set_cnt)
		throw gcnew System::IndexOutOfRangeException("set is out of range.");

	VkDescriptorBufferInfo buf_info = {};
	buf_info.buffer = buf->GetBuffer();
	buf_info.offset = off;
	buf_info.range = len;

	VkWriteDescriptorSet desc_write = {};
	desc_write.sType = VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;
	desc_write.dstSet = sets[set];
	desc_write.dstBinding = static_cast<uint32_t>(binding);
	desc_write.dstArrayElement = static_cast<uint32_t>(idx);
	desc_write.descriptorCount = 1;
	desc_write.pImageInfo = nullptr;
	desc_write.pBufferInfo = &buf_info;
	desc_write.pTexelBufferView = nullptr;

	vkUpdateDescriptorSets(GraphicsDevice::GetDevice(), 1, &desc_write, 0, nullptr);
}

void Kokoro::Graphics::DescriptorSet::SetBufferView(int set, int binding, int idx, GPUBuffer^ buf)
{
	if (set >= set_cnt)
		throw gcnew System::IndexOutOfRangeException("set is out of range.");

	auto view = buf->GetView();

	VkWriteDescriptorSet desc_write = {};
	desc_write.sType = VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET;
	desc_write.dstSet = sets[set];
	desc_write.dstBinding = static_cast<uint32_t>(binding);
	desc_write.dstArrayElement = static_cast<uint32_t>(idx);
	desc_write.descriptorCount = 1;
	desc_write.pImageInfo = nullptr;
	desc_write.pBufferInfo = nullptr;
	desc_write.pTexelBufferView = &view;

	vkUpdateDescriptorSets(GraphicsDevice::GetDevice(), 1, &desc_write, 0, nullptr);
}
