#include "GPUBuffer.h"

Kokoro::Graphics::GPUBuffer::GPUBuffer() {
	map_cnt = 0;
	persistent_mapped = false;
	viewBuilt = false;
}

VkBuffer Kokoro::Graphics::GPUBuffer::GetBuffer()
{
	return buf;
}

VkBufferView Kokoro::Graphics::GPUBuffer::GetView()
{
	return bufView;
}

Kokoro::Graphics::GPUBuffer^ Kokoro::Graphics::GPUBuffer::Allocate(SharingMode mode, BufferUsage usage, MemoryUsage memUsage, size_t sz, bool persistent_map) {
	VkBufferCreateInfo creatInfo = {};
	creatInfo.sType = VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO;
	creatInfo.size = sz;
	creatInfo.usage = BufferUsageConv::Convert(usage);
	creatInfo.sharingMode = (VkSharingMode)SharingModeConv::Convert(mode);

	GPUBuffer^ ret = gcnew GPUBuffer();
	pin_ptr<VkBuffer> buf_ptr = &ret->buf;
	pin_ptr<WVmaAllocation> buf_allocation_ptr = &ret->alloc;
	ret->persistent_mapped = persistent_map;
	ret->Size = sz;
	GraphicsDevice::CreateBuffer(&creatInfo, memUsage, persistent_map, buf_ptr, buf_allocation_ptr);

	return ret;
}

Kokoro::Graphics::GPUBuffer::~GPUBuffer() {
	if (!persistent_mapped)
		while (map_cnt > 0)
			Unmap();
	GraphicsDevice::DestroyBuffer(buf, alloc);
}

void Kokoro::Graphics::GPUBuffer::Map(size_t off, size_t len, void** ptr) {
	if (persistent_mapped) {
		*ptr = ((uint8_t*)alloc->GetPtr() + off);
	}
	else {
		map_cnt++;
		if (vkMapMemory(GraphicsDevice::GetDevice(), alloc->GetMemory(), off, len, 0, ptr) != VK_SUCCESS)
			throw gcnew System::Exception("Failed to map buffer.");
	}
}

void Kokoro::Graphics::GPUBuffer::Unmap() {
	if (!persistent_mapped) {
		vkUnmapMemory(GraphicsDevice::GetDevice(), alloc->GetMemory());
		map_cnt--;
	}
}

void Kokoro::Graphics::GPUBuffer::Flush(size_t off, size_t len) {
	VkMappedMemoryRange flush_range = { };
	flush_range.sType = VK_STRUCTURE_TYPE_MAPPED_MEMORY_RANGE;
	flush_range.pNext = nullptr;
	flush_range.memory = alloc->GetMemory();
	flush_range.offset = off;
	flush_range.size = len;
	vkFlushMappedMemoryRanges(GraphicsDevice::GetDevice(), 1, &flush_range);
}

void Kokoro::Graphics::GPUBuffer::BuildView(ImageFormat fmt, size_t offset, size_t len) {
	if (!viewBuilt) {
		VkBufferViewCreateInfo creatInfo = {};
		creatInfo.sType = VK_STRUCTURE_TYPE_BUFFER_VIEW_CREATE_INFO;
		creatInfo.flags = 0;
		creatInfo.buffer = buf;
		creatInfo.format = ImageFormatConv::Convert(fmt);
		creatInfo.offset = offset;
		creatInfo.range = len;

		pin_ptr<VkBufferView> bufView_ptr = &bufView;
		if (vkCreateBufferView(GraphicsDevice::GetDevice(), &creatInfo, nullptr, bufView_ptr) != VK_SUCCESS)
			throw gcnew System::Exception("Failed to create buffer view.");

		viewBuilt = true;
	}
}