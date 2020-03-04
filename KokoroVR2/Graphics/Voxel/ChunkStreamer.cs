using Kokoro.Graphics;
using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KokoroVR2.Graphics.Voxel
{
    public class ChunkStreamer// : Interactable
    {
        //Receive chunk faces
        public const int VRAMCacheSize = 16384;  //TODO make this depend on total available vram
        const int blk_cnt = 16 * 1024;

        private Chunk[] ChunkList;
        private (ChunkMesh, int, double)[] ChunkCache;
        private StreamableBuffer drawParams;
        private IndexedRenderQueue queue;
        private ShaderSource voxelV, voxelF, cullShader;
        //private RenderState state;
        private List<(int, Vector3)> Draws;

        private ChunkBuffer VertexBuffer;
        private DeferredRenderer renderer;

        private BufferAllocator indexBufferAllocator;

        private double cur_time;

        public VoxelDictionary MaterialMap { get; private set; }
        public int MaxChunkCount { get; private set; }

        public ChunkStreamer(int max_count, DeferredRenderer renderer)
        {
            Draws = new List<(int, Vector3)>();

            VertexBuffer = new ChunkBuffer();
            indexBufferAllocator = new BufferAllocator("IndexBufferAlloc", Engine.Graph, ChunkConstants.BlockSize * sizeof(uint), blk_cnt, BufferUsage.Index, ImageFormat.R32UInt);

            this.renderer = renderer;

            MaxChunkCount = max_count;
            ChunkList = new Chunk[max_count];
            ChunkCache = new (ChunkMesh, int, double)[VRAMCacheSize];
            for (int i = 0; i < ChunkCache.Length; i++)
            {
                ChunkCache[i].Item1 = new ChunkMesh(indexBufferAllocator, VertexBuffer);
                ChunkCache[i].Item2 = -1;
                ChunkCache[i].Item3 = double.MinValue;
            }

            queue = new IndexedRenderQueue("VoxelRenderQueue", Engine.Graph, blk_cnt, IndexType.U32);
            drawParams = new StreamableBuffer("VoxelDrawParams", Engine.Graph, blk_cnt * 8 * sizeof(uint), BufferUsage.Storage);
            voxelV = ShaderSource.Load(ShaderType.VertexShader, "Deferred/Voxel/vertex.glsl");
            voxelF = ShaderSource.Load(ShaderType.FragmentShader, "Deferred/Voxel/fragment.glsl");
            //cullShader = new ShaderProgram(ShaderSource.Load(ShaderType.ComputeShader, "Shaders/HiZ/culldraws.glsl", $"#define MIP_COUNT {renderer.HiZMap[0].Length}"));

            MaterialMap = new VoxelDictionary();
        }

        public Chunk Allocate()
        {
            for (int i = 0; i < ChunkList.Length; i++)
                if (ChunkList[i] == null)
                {
                    ChunkList[i] = new Chunk(this, i);
                    return ChunkList[i];
                }

            return null;
        }

        public void Free(Chunk chunk)
        {
            if (ChunkList[chunk.id] != null)
                ChunkList[chunk.id] = null;
        }

        public void RenderChunk(Chunk c, Vector3 offset)
        {
            if (c.streamer != this) throw new Exception("Chunk not owned by current renderer.");
            if (c.empty) return;

            int mesh_idx = -1;
            for (int i = 0; i < ChunkCache.Length; i++) if (ChunkCache[i].Item2 == c.id) { mesh_idx = i; break; }
            if (mesh_idx == -1)
            {
                //Allocate the least recently used mesh for this chunk
                int lru = 0;
                double lru_tm = double.MaxValue;
                for (int i = 0; i < ChunkCache.Length; i++)
                    if (ChunkCache[i].Item3 < lru_tm)
                    {
                        lru_tm = ChunkCache[i].Item3;
                        lru = i;
                    }

                mesh_idx = lru;
                ChunkCache[lru].Item3 = cur_time;
                ChunkCache[lru].Item2 = c.id;

                if (!c.empty) c.update_pending = true;
            }

            if (c.empty)
                return;

            //Upload the current mesh state if it has been updated recently
            if (c.update_pending)
            {
                unsafe
                {
                    ChunkCache[mesh_idx].Item1.Reallocate(c.faces, c.indices, c.boundAABBs, c.norm_mask, offset);

                    var dP_p = (float*)drawParams.BeginBufferUpdate();
                    for (int j = 0; j < ChunkCache[mesh_idx].Item1.AllocIndices.Length; j++)
                    {
                        int idx = ChunkCache[mesh_idx].Item1.AllocIndices[j];
                        dP_p[idx * 4 + 0] = offset.X;
                        dP_p[idx * 4 + 1] = offset.Y;
                        dP_p[idx * 4 + 2] = offset.Z;
                    }
                    drawParams.EndBufferUpdate();
                }
                c.faces = null;
                c.indices = null;
                c.boundSpheres = null;
                c.norm_mask = null;
                c.update_pending = false;
            }
            Draws.Add((mesh_idx, offset));
        }

        public void InitialUpdate(double time)
        {
            Draws.Clear();
            for (int i = 0; i < ChunkList.Length; i++)
            {
                if (ChunkList[i] != null) ChunkList[i].Update();    //Trigger any necessary rebuilds
            }
            indexBufferAllocator.Update();
            VertexBuffer.Update();
            drawParams.Update();
        }

        public void GenerateRenderGraph()
        {
            indexBufferAllocator.GenerateRenderGraph();
            VertexBuffer.GenerateRenderGraph();
            drawParams.GenerateRenderGraph();
            queue.GenerateRenderGraph();
            Engine.Graph.RegisterShaderParams(new ShaderParameterSet()
            {
                Name = "VoxelStreamerParams",
                Buffers = new BufferInfo[]
                {
                    new BufferInfo()
                    {
                        Name = "VoxelDrawParamBuffer",
                        BindingIndex = 0,
                        DescriptorType = DescriptorType.StorageBuffer,
                        DeviceBuffer = new GpuBuffer[]{ drawParams.LocalBuffer },
                    },
                    new BufferInfo()
                    {
                        Name = "VoxelIndirectDrawBuffer",
                        BindingIndex = 1,
                        DescriptorType = DescriptorType.StorageBuffer,
                        DeviceBuffer = new GpuBuffer[]{ queue.IndirectBuffer.LocalBuffer }
                    },
                    new BufferInfo()
                    {
                        Name = "VoxelVertices",
                        BindingIndex = 2,
                        DescriptorType = DescriptorType.StorageTexelBuffer,
                        View = VertexBuffer.Views
                    }
                }
            });
            Engine.DeferredRenderer.RegisterDependency("VoxelStreamerPass");
            Engine.Graph.RegisterPass(new GraphicsPass()
            {
                Name = "VoxelStreamerPass",
                ShaderParamName = new string[]
                {
                    Engine.Graph.GlobalParametersName,
                    "VoxelStreamerParams",
                    VoxelDictionary.Name
                },
                PassDependencies = new string[]
                {
                    Engine.Graph.GlobalParametersName,
                    drawParams.Name,
                    queue.IndirectBuffer.Name,
                    VertexBuffer.Names[0],
                    VertexBuffer.Names[1],
                    VertexBuffer.Names[2],
                    VertexBuffer.Names[3],
                    VertexBuffer.Names[4],
                    VertexBuffer.Names[5],
                    VertexBuffer.Names[6],
                    VoxelDictionary.Name,
                    indexBufferAllocator.Name,
                },
                DrawCmd = new IndexedIndirectDrawCmd()
                {
                    CountBuffer = queue.IndirectBuffer.LocalBuffer,
                    DrawBuffer = queue.IndirectBuffer.LocalBuffer,
                    CountOffset = IndexedRenderQueue.DrawCountOffset,
                    DrawBufferOffset = IndexedRenderQueue.DrawInfoOffset,
                    IndexBuffer = indexBufferAllocator.LocalBuffer,
                    IndexOffset = 0,
                    IndexType = IndexType.U32,
                    MaxCount = blk_cnt,
                    Stride = IndexedRenderQueue.Stride,
                },
                DepthAttachment = new AttachmentUsageInfo()
                {
                    Name = DeferredRenderer.DepthMapName,
                    Usage = AttachmentUsage.WriteOnlyClear
                },
                AttachmentUsage = new AttachmentUsageInfo[]
                {
                    new AttachmentUsageInfo()
                    {
                        Name = DeferredRenderer.PositionMapName,
                        Usage = AttachmentUsage.WriteOnlyClear,
                    },
                    new AttachmentUsageInfo()
                    {
                        Name = DeferredRenderer.NormalMapName,
                        Usage = AttachmentUsage.WriteOnlyClear
                    }
                },
                Topology = PrimitiveType.Triangle,
                DepthTest = DepthTest.Greater,
                CullMode = CullMode.Front,
                Shaders = new ShaderSource[] { voxelV, voxelF },
            });
        }

        public void FinalUpdate(double time)
        {
            queue.Reset();
            var sorted = Draws.OrderBy(a => (a.Item2 - Engine.CurrentPlayer.Position).LengthSquared);
            foreach (var (mesh_idx, _) in sorted)
            {
                queue.RecordDraw(0, 1, ChunkCache[mesh_idx].Item1);
            }
            queue.Update(Engine.Frustum);
        }

    }
}
