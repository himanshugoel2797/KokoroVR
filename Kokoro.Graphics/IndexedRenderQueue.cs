using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kokoro.Graphics
{
    public class IndexedRenderQueue
    {
        struct DrawData
        {
            public uint baseInstance;
            public uint instanceCount;
            public IMesh2 mesh;
        }
        private List<DrawData> draws;
        private int maxDraws;
        private IndexType idxType;

        public StreamableBuffer IndirectBuffer { get; }
        public const uint Stride = 16 * sizeof(uint);
        public const uint DrawCountOffset = 0;
        public const uint DrawInfoOffset = sizeof(uint) * 4;

        public IndexedRenderQueue(string name, Framegraph graph, int max_cnt, IndexType idxType)
        {
            draws = new List<DrawData>();
            this.idxType = idxType;
            maxDraws = max_cnt;
            IndirectBuffer = new StreamableBuffer(name, graph, (ulong)(max_cnt * 16 + 4) * sizeof(uint), BufferUsage.Indirect | BufferUsage.Storage);
        }

        public void Reset()
        {
            draws.Clear();
        }

        public void RecordDraw(uint baseInstance, uint instanceCount, IMesh2 mesh)
        {
            if (draws.Count < maxDraws)
                draws.Add(new DrawData()
                {
                    baseInstance = baseInstance,
                    instanceCount = instanceCount,
                    mesh = mesh
                });
            else
                throw new Exception("Queue is full!");
        }

        public void GenerateRenderGraph()
        {
            IndirectBuffer.GenerateRenderGraph();
        }

        public void Update(Frustum f)
        {
            unsafe
            {
                uint boff = 4;
                uint* drawData = (uint*)IndirectBuffer.BeginBufferUpdate();
                uint idx = 0;
                for (int i = 0; i < draws.Count; i++)
                {
                    var m = draws[i].mesh;
                    var _draws = m.Sort(f, f.EyePosition);

                    for (int j = 0; j < _draws.Length; j++)
                    {
                        (int k, uint cnt, Vector3 min, Vector3 max) = _draws[j];

                        drawData[idx * 16 + boff + 0] = (uint)(cnt / (idxType == IndexType.U16 ? 2 : 4));
                        drawData[idx * 16 + boff + 1] = draws[i].instanceCount;
                        drawData[idx * 16 + boff + 2] = (uint)((m.AllocIndices[k] * m.BlockSize) / (idxType == IndexType.U16 ? 2 : 4));
                        drawData[idx * 16 + boff + 3] = 0;
                        drawData[idx * 16 + boff + 4] = draws[i].baseInstance;
                        drawData[idx * 16 + boff + 5] = (uint)m.AllocIndices[k];
                        drawData[idx * 16 + boff + 6] = (uint)m.BufferIdx;
                        drawData[idx * 16 + boff + 7] = m.BaseVertex;

                        ((float*)drawData)[(idx * 16) + boff + 8] = min.X;
                        ((float*)drawData)[(idx * 16) + boff + 9] = min.Y;
                        ((float*)drawData)[(idx * 16) + boff + 10] = min.Z;

                        ((float*)drawData)[(idx * 16) + boff + 12] = max.X;
                        ((float*)drawData)[(idx * 16) + boff + 13] = max.Y;
                        ((float*)drawData)[(idx * 16) + boff + 14] = max.Z;

                        /*
                         * typedef struct VkDrawIndexedIndirectCommand {
                                uint32_t    indexCount;
                                uint32_t    instanceCount;
                                uint32_t    firstIndex;
                                int32_t     vertexOffset;
                                uint32_t    firstInstance;
                            } VkDrawIndexedIndirectCommand;
                         */

                        idx++;
                    }
                }
                drawData[0] = idx;
                IndirectBuffer.EndBufferUpdate();
            }
            IndirectBuffer.Update();
        }
    }
}
