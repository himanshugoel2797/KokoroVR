using Kokoro.Graphics;
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
        public const int VRAMCacheSize = 1024;  //TODO make this depend on total available vram
        const uint blk_cnt = 8192 * 8;

        private Chunk[] ChunkList;
        private (ChunkMesh, int, double)[] ChunkCache;
        private BufferGroupAllocator buffer;
        private ShaderStorageBuffer drawParams;
        private RenderQueue2 queue;
        private ShaderProgram voxelShader;
        private RenderState state;
        private List<(int, Vector3)> Draws;

        //private ShaderProgram cullingShader;
        //private ShaderProgram compactShader;

        private BufferAllocator indexBufferAllocator;
        private IndexBuffer indexBuffer;
        //private ShaderStorageBuffer o_indexSSBO;
        //private IndexBuffer o_indexBuffer;

        private ShaderStorageBuffer multiDrawParams;
        private ShaderStorageBuffer multiDrawParams2;

        private double cur_time;
        private VREye cur_eye;

        public VoxelDictionary MaterialMap { get; private set; }
        public int MaxChunkCount { get; private set; }
        public ChunkStreamerEnd Ender { get; private set; }

        public ChunkStreamer(int max_count)
        {
            Ender = new ChunkStreamerEnd(this);
            Draws = new List<(int, Vector3)>();

            indexBufferAllocator = new BufferAllocator(ChunkConstants.BlockSize * sizeof(uint), blk_cnt, false, PixelInternalFormat.R32ui);
            indexBuffer = new IndexBuffer(indexBufferAllocator.BufferTex.Buffer, false);
            //o_indexSSBO = new ShaderStorageBuffer(6 * 64 * 48 * blk_cnt * sizeof(uint), false);
            //o_indexBuffer = new IndexBuffer(o_indexSSBO, false);

            multiDrawParams = new ShaderStorageBuffer((5 * blk_cnt + 4) * sizeof(uint), true);
            multiDrawParams2 = new ShaderStorageBuffer((5 * blk_cnt + 4) * sizeof(uint), false);

            MaxChunkCount = max_count;
            ChunkList = new Chunk[max_count];
            ChunkCache = new (ChunkMesh, int, double)[blk_cnt];
            for (int i = 0; i < ChunkCache.Length; i++)
            {
                ChunkCache[i].Item1 = new ChunkMesh(indexBufferAllocator);
                ChunkCache[i].Item2 = -1;
                ChunkCache[i].Item3 = double.MinValue;
            }

            queue = new RenderQueue2(blk_cnt, true);
            drawParams = new ShaderStorageBuffer(blk_cnt * 8 * sizeof(uint), false);
            voxelShader = new ShaderProgram(ShaderSource.Load(ShaderType.VertexShader, "Shaders/Deferred/Voxel/vertex.glsl"),
                                            ShaderSource.Load(ShaderType.FragmentShader, "Shaders/Deferred/Voxel/fragment.glsl"));

            //cullingShader = new ShaderProgram(ShaderSource.Load(ShaderType.ComputeShader, "Shaders/Deferred/Voxel/Cull/filter.glsl"));
            //compactShader = new ShaderProgram(ShaderSource.Load(ShaderType.ComputeShader, "Shaders/Deferred/Voxel/Cull/compact.glsl"));

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

                //if (c.faces == null) c.RebuildFullMesh();
                if (!c.empty) c.update_pending = true;
            }

            //Upload the current mesh state if it has been updated recently
            if (!c.empty && c.update_pending)
            {
                unsafe
                {
                    ChunkCache[mesh_idx].Item1.Reallocate(c.faces, c.indices, c.bounds, c.norm_mask, Vector3.One * ChunkConstants.Side * -0.5f + offset);

                    var dP_p = (float*)drawParams.Update();
                    for (int j = 0; j < ChunkCache[mesh_idx].Item1.AllocIndices.Length; j++)
                    {
                        int idx = ChunkCache[mesh_idx].Item1.AllocIndices[j];
                        dP_p[idx * 8 + 0] = offset.X - ChunkConstants.Side * 0.5f;
                        dP_p[idx * 8 + 1] = offset.Y - ChunkConstants.Side * 0.5f;
                        dP_p[idx * 8 + 2] = offset.Z - ChunkConstants.Side * 0.5f;

                        ((long*)dP_p)[idx * 4 + 2] = ChunkCache[mesh_idx].Item1.VertexBuffer;
                    }
                    drawParams.UpdateDone();
                }
                c.faces = null;
                c.indices = null;
                c.bounds = null;
                c.norm_mask = null;
                c.update_pending = false;
            }

            //TODO Sort these draws front to back
            //TODO do the same internally for each mesh
            //Record this chunk's draw
            if (!c.empty)
                Draws.Add((mesh_idx, offset - Vector3.One * ChunkConstants.Side * 0.5f));
        }

        public override void Update(double time, World parent)
        {
            for (int i = 0; i < ChunkList.Length; i++)
            {
                if (ChunkList[i] != null) ChunkList[i].Update();    //Trigger any necessary rebuilds
            }
        }

        public override void Render(double time, Framebuffer fbuf, StaticMeshRenderer staticMesh, DynamicMeshRenderer dynamicMesh, VREye eye)
        {
            cur_time += time;
            cur_eye = eye;

            unsafe
            {
                var mp_p = (uint*)multiDrawParams.Update();
                mp_p[0] = blk_cnt / 64;
                mp_p[1] = 1;
                mp_p[2] = 1;
                mp_p[3] = 0;
                for (int i = 0; i < blk_cnt; i++)
                {
                    mp_p[i * 5 + 4] = 0;
                    mp_p[i * 5 + 5] = 1;
                    mp_p[i * 5 + 6] = (uint)(i * ChunkConstants.BlockSize);
                    mp_p[i * 5 + 7] = 0;
                    mp_p[i * 5 + 8] = 0;
                }
                multiDrawParams.UpdateDone();

                var mp_p = (uint*)multiDrawParams2.Update();
                mp_p[0] = blk_cnt / 64;
                mp_p[1] = 1;
                mp_p[2] = 1;
                mp_p[3] = 0;
                for (int i = 0; i < blk_cnt; i++)
                {
                    mp_p[i * 5 + 4] = 0;
                    mp_p[i * 5 + 5] = 1;
                    mp_p[i * 5 + 6] = (uint)(i * ChunkConstants.BlockSize);
                    mp_p[i * 5 + 7] = 0;
                    mp_p[i * 5 + 8] = 0;
                }
                multiDrawParams2.UpdateDone();
            }

            Draws.Clear();
            voxelShader.Set("eyePos", Engine.CurrentPlayer.Position);
            voxelShader.Set("ViewProj", Engine.View[(int)eye] * Engine.Projection[(int)eye]);
            state = new RenderState(fbuf, voxelShader, new ShaderStorageBuffer[] { MaterialMap.voxelData, drawParams }, null, true, true, DepthFunc.Greater, InverseDepth.Far, InverseDepth.Near, BlendFactor.One, BlendFactor.Zero, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.Back, indexBuffer);
            queue.ClearAndBeginRecording();
        }

        public class ChunkStreamerEnd : Interactable
        {
            ChunkStreamer parent;
            internal ChunkStreamerEnd(ChunkStreamer p)
            {
                parent = p;
            }

            public override void Render(double time, Framebuffer fbuf, StaticMeshRenderer staticMesh, DynamicMeshRenderer dynamicMesh, VREye eye)
            {
                var sorted = parent.Draws.OrderBy(a => (a.Item2 - Engine.CurrentPlayer.Position).LengthSquared);
                foreach (var (mesh_idx, _) in sorted)
                {
                    parent.queue.RecordDraw(new RenderQueue2.DrawData()
                    {
                        State = parent.state,
                        Meshes = new RenderQueue2.MeshData[]
                        {
                            new RenderQueue2.MeshData(){
                                BaseInstance = 0,
                                InstanceCount = 1,
                                Mesh = parent.ChunkCache[mesh_idx].Item1
                            }
                        }
                    });
                }

                parent.queue.EndRecording(Engine.Frustums[(int)eye], Engine.CurrentPlayer.Position);

                /*parent.cullingShader.Set("eyePos", Engine.CurrentPlayer.Position);
                GraphicsDevice.SetShaderStorageBufferBinding(parent.drawParams, 1);
                GraphicsDevice.SetShaderStorageBufferBinding(parent.indexBuffer.Buffer, 2);
                GraphicsDevice.SetShaderStorageBufferBinding(parent.queue.MultidrawParams, 3);
                GraphicsDevice.SetShaderStorageBufferBinding(parent.o_indexSSBO, 4);
                GraphicsDevice.SetShaderStorageBufferBinding(parent.multiDrawParams, 5);
                GraphicsDevice.SetShaderStorageBufferBinding(parent.multiDrawParams2, 6);
                GraphicsDevice.DispatchIndirectSyncComputeJob(parent.cullingShader, parent.queue.MultidrawParams, 0);
                parent.multiDrawParams.UpdateDone();
                GraphicsDevice.DispatchIndirectSyncComputeJob(parent.compactShader, parent.multiDrawParams, 0);
                parent.multiDrawParams2.UpdateDone();
                parent.o_indexBuffer.UpdateDone();
                
                GraphicsDevice.SetRenderState(parent.state);
                GraphicsDevice.SetMultiDrawParameterBuffer(parent.multiDrawParams2);
                GraphicsDevice.SetParameterBuffer(parent.multiDrawParams2);
                GraphicsDevice.MultiDrawIndirectCount(PrimitiveType.Triangles, parent.multiDrawParams2.Offset + sizeof(uint) * 4, parent.multiDrawParams2.Offset + sizeof(uint) * 3, blk_cnt, true, false);
                */
                parent.queue.Submit();
            }

            public override void Update(double time, World parent)
            {

            }
        }
    }
}
