#pragma once
#include "GraphicsDevice.h"
#include "SharingMode.h"
#include "ImageFormat.h"

namespace Kokoro::Graphics {
	public enum class BufferUsage {
		None = 0,
		Vertex = (1 << 0),
		Index = (1 << 1),
		Uniform = (1 << 2),
		Storage = (1 << 3),
		Indirect = (1 << 4),
		TransferSrc = (1 << 5),
		TransferDst = (1 << 6),
		UniformTexel = (1 << 7),
		StorageTexel = (1 << 8),
	};

	public ref class BufferUsageConv {
	public:
		static VkBufferUsageFlagBits Convert(BufferUsage s) {
			uint32_t flags = 0;
			if ((s & BufferUsage::Vertex) != BufferUsage::None)
				flags |= VK_BUFFER_USAGE_VERTEX_BUFFER_BIT;
			if ((s & BufferUsage::Index) != BufferUsage::None)
				flags |= VK_BUFFER_USAGE_INDEX_BUFFER_BIT;
			if ((s & BufferUsage::Uniform) != BufferUsage::None)
				flags |= VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT;
			if ((s & BufferUsage::Storage) != BufferUsage::None)
				flags |= VK_BUFFER_USAGE_STORAGE_BUFFER_BIT;
			if ((s & BufferUsage::Indirect) != BufferUsage::None)
				flags |= VK_BUFFER_USAGE_INDIRECT_BUFFER_BIT;
			if ((s & BufferUsage::TransferSrc) != BufferUsage::None)
				flags |= VK_BUFFER_USAGE_TRANSFER_SRC_BIT;
			if ((s & BufferUsage::TransferDst) != BufferUsage::None)
				flags |= VK_BUFFER_USAGE_TRANSFER_DST_BIT;
			if ((s & BufferUsage::UniformTexel) != BufferUsage::None)
				flags |= VK_BUFFER_USAGE_UNIFORM_TEXEL_BUFFER_BIT;
			if ((s & BufferUsage::StorageTexel) != BufferUsage::None)
				flags |= VK_BUFFER_USAGE_STORAGE_TEXEL_BUFFER_BIT;
			return (VkBufferUsageFlagBits)flags;
		}
	};

	public ref class GPUBuffer
	{
	private:
		GPUBuffer();
		VkBuffer buf;
		VkBufferView bufView;
		WVmaAllocation alloc;
		BufferUsage buf_usage;
		int map_cnt;
		bool persistent_mapped;
		bool viewBuilt;
	internal:
		VkBuffer GetBuffer();
		VkBufferView GetView();
	public:
		property size_t Size;

		static GPUBuffer^ Allocate(SharingMode mode, BufferUsage usage, MemoryUsage memUsage, size_t sz, bool persistent_map);
		~GPUBuffer();

		void Map(size_t off, size_t len, void** ptr);
		void Unmap();
		void Flush(size_t off, size_t len);
		void BuildView(ImageFormat fmt, size_t offset, size_t len);
	};
}

