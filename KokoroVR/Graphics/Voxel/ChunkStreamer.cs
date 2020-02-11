using GPUPerfAPI.NET;
using Kokoro.Graphics;
using Kokoro.Graphics.Profiling;
using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Graphics.Voxel
{
    public class ChunkStreamer : Interactable
    {
        //Receive chunk faces
        public const int VRAMCacheSize = 16384;  //TODO make this depend on total available vram
        const int blk_cnt = 64 * 1024;

        private Chunk[] ChunkList;
        private (ChunkMesh, int, double)[] ChunkCache;
        private StorageBuffer drawParams;
        private RenderQueue queue;
        private ShaderProgram voxelShader;
        private RenderState state;
        private CommandBuffer cmdBuffer;
        private List<(int, Vector3)> Draws;

        private ChunkBuffer VertexBuffer;
        private DeferredRenderer renderer;

        private BufferAllocator indexBufferAllocator;
        private IndexBuffer indexBuffer;

        private double cur_time;
        private VREye cur_eye;

        public VoxelDictionary MaterialMap { get; private set; }
        public int MaxChunkCount { get; private set; }
        public ChunkStreamerEnd Ender { get; private set; }

        public ChunkStreamer(int max_count, DeferredRenderer renderer)
        {
            Draws = new List<(int, Vector3)>();

            VertexBuffer = new ChunkBuffer();
            indexBufferAllocator = new BufferAllocator(ChunkConstants.BlockSize * sizeof(uint), blk_cnt, false, PixelInternalFormat.R32ui);
            indexBuffer = new IndexBuffer(indexBufferAllocator.BufferTex.Buffer, false);

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

            queue = new RenderQueue(blk_cnt, IndexType.UInt, !true);
            drawParams = new StorageBuffer(blk_cnt * 8 * sizeof(uint), false);
            voxelShader = new ShaderProgram(ShaderSource.Load(ShaderType.VertexShader, "Shaders/Deferred/Voxel/vertex.glsl"),
                                            ShaderSource.Load(ShaderType.FragmentShader, "Shaders/Deferred/Voxel/fragment.glsl"));

            cmdBuffer = new CommandBuffer();
            MaterialMap = new VoxelDictionary();
            Ender = new ChunkStreamerEnd(this);
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

            //Don't proceed if this chunk isn't supposed to be visible
            if (!Engine.Frustums[(int)cur_eye].IsVisible(new Vector4(offset - Vector3.One * ChunkConstants.Side * 0.5f, (float)(ChunkConstants.Side * 0.75f * System.Math.Sqrt(3)))))
                return;

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

                //if (!c.empty && c.faces == null) c.RebuildFullMesh((int)offset.X, (int)offset.Y, (int)offset.Z);
                if (!c.empty) c.update_pending = true;
            }

            if (c.empty)
                return;

            //Upload the current mesh state if it has been updated recently
            if (c.update_pending)
            {
                unsafe
                {
                    ChunkCache[mesh_idx].Item1.Reallocate(c.faces, c.indices, c.boundSpheres, c.norm_mask, Vector3.One * ChunkConstants.Side * -0.5f + offset);

                    var dP_p = (float*)drawParams.Update();
                    for (int j = 0; j < ChunkCache[mesh_idx].Item1.AllocIndices.Length; j++)
                    {
                        int idx = ChunkCache[mesh_idx].Item1.AllocIndices[j];
                        dP_p[idx * 4 + 0] = offset.X - ChunkConstants.Side * 0.5f;
                        dP_p[idx * 4 + 1] = offset.Y - ChunkConstants.Side * 0.5f;
                        dP_p[idx * 4 + 2] = offset.Z - ChunkConstants.Side * 0.5f;
                    }
                    drawParams.UpdateDone();
                }
                c.faces = null;
                c.indices = null;
                c.boundSpheres = null;
                c.norm_mask = null;
                c.update_pending = false;
            }
            Draws.Add((mesh_idx, offset - Vector3.One * ChunkConstants.Side * 0.5f));
        }

        public override void Update(double time, World parent)
        {
            for (int i = 0; i < ChunkList.Length; i++)
            {
                if (ChunkList[i] != null) ChunkList[i].Update();    //Trigger any necessary rebuilds
            }
        }

        public override void Render(double time, Framebuffer fbuf, VREye eye)
        {
            cur_time += time;
            cur_eye = eye;

            Draws.Clear();
            state = new RenderState(fbuf, voxelShader, new StorageBuffer[] { MaterialMap.voxelData, drawParams, queue.MultidrawParams }, new UniformBuffer[] { Engine.GlobalParameters, VertexBuffer.VertexBuffers }, true, true, DepthFunc.Greater, InverseDepth.Far, InverseDepth.Near, BlendFactor.One, BlendFactor.Zero, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.Back, indexBuffer);
            cmdBuffer.Reset();
            cmdBuffer.SetRenderState(state);
            cmdBuffer.Clear(true, true);
            cmdBuffer.MultiDrawIndirectIndexed(PrimitiveType.Triangles, queue.MultidrawParams, false, RenderQueue.InfoOffset, RenderQueue.DrawCountOffset, queue.MaxDrawCount, RenderQueue.Stride);
            
            queue.Clear();
        }

        public class ChunkStreamerEnd : Interactable
        {
            ChunkStreamer parent;

            internal ChunkStreamerEnd(ChunkStreamer p)
            {
                parent = p;
            }

            public override void Render(double time, Framebuffer fbuf, VREye eye)
            {
                var sorted = parent.Draws.OrderBy(a => (a.Item2 - Engine.CurrentPlayer.Position).LengthSquared);
                foreach (var (mesh_idx, _) in sorted)
                {
                    parent.queue.RecordDraw(new DrawData()
                    {
                        State = parent.state,
                        Meshes = new MeshData[]
                        {
                            new MeshData(){
                                BaseInstance = 0,
                                InstanceCount = 1,
                                Mesh = parent.ChunkCache[mesh_idx].Item1
                            }
                        }
                    });
                }

                parent.voxelShader.Set("eyeIdx", (int)eye);
                parent.queue.Build(Engine.Frustums[(int)eye], Engine.CurrentPlayer.Position);
                parent.cmdBuffer.Submit();
                //parent.queue.Submit();
            }

            public override void Update(double time, World parent)
            {

            }
        }
    }
}
