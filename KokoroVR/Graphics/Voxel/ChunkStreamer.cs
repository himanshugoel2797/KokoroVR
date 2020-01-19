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
        public const int VRAMCacheSize = 1024;  //TODO make this depend on total available vram
        const uint blk_cnt = 8192 * 16;

        private Chunk[] ChunkList;
        private (ChunkMesh, int, double)[] ChunkCache;
        private ShaderStorageBuffer drawParams;
        private RenderQueue2 queue;
        private ShaderProgram voxelShader;
        private RenderState state;
        private List<(int, Vector3)> Draws;

        private RenderState cubeMapState;
        private RenderQueue2[] cubeMapRender;
        private GIMapRenderer gi;
        private ShaderProgram voxelGiShader;
        private DeferredRenderer renderer;

        private BufferAllocator indexBufferAllocator;
        private IndexBuffer indexBuffer;

        private double cur_time;
        private VREye cur_eye;

        public VoxelDictionary MaterialMap { get; private set; }
        public int MaxChunkCount { get; private set; }
        public ChunkStreamerEnd Ender { get; private set; }

        public ChunkStreamer(int max_count, GIMapRenderer gi, DeferredRenderer renderer)
        {
            Draws = new List<(int, Vector3)>();

            indexBufferAllocator = new BufferAllocator(ChunkConstants.BlockSize * sizeof(uint), blk_cnt, false, PixelInternalFormat.R32ui);
            indexBuffer = new IndexBuffer(indexBufferAllocator.BufferTex.Buffer, false);

            this.gi = gi;

            cubeMapRender = new RenderQueue2[6];
            for (int i = 0; i < 6; i++)
                cubeMapRender[i] = new RenderQueue2(blk_cnt, true);
            this.renderer = renderer;

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
            voxelGiShader = new ShaderProgram(ShaderSource.Load(ShaderType.VertexShader, "Shaders/Deferred/Voxel/vertex.glsl"),
                                            ShaderSource.Load(ShaderType.FragmentShader, "Shaders/Deferred/Voxel/fragment_gi.glsl"));

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
            //if (!Engine.Frustums[(int)cur_eye].IsVisible(new Vector4(offset - Vector3.One * ChunkConstants.Side * 0.5f, (float)(ChunkConstants.Side * 0.75f * System.Math.Sqrt(3)))))
            //    return;

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
                    ChunkCache[mesh_idx].Item1.Reallocate(c.data, c.faces, c.indices, c.bounds, c.norm_mask, Vector3.One * ChunkConstants.Side * -0.5f + offset);

                    var dP_p = (float*)drawParams.Update();
                    for (int j = 0; j < ChunkCache[mesh_idx].Item1.AllocIndices.Length; j++)
                    {
                        int idx = ChunkCache[mesh_idx].Item1.AllocIndices[j];
                        dP_p[idx * 8 + 0] = offset.X - ChunkConstants.Side * 0.5f;
                        dP_p[idx * 8 + 1] = offset.Y - ChunkConstants.Side * 0.5f;
                        dP_p[idx * 8 + 2] = offset.Z - ChunkConstants.Side * 0.5f;

                        ((long*)dP_p)[idx * 4 + 2] = ChunkCache[mesh_idx].Item1.VertexBuffer;
                        ((long*)dP_p)[idx * 4 + 3] = ChunkCache[mesh_idx].Item1.MeshTexture;
                    }
                    drawParams.UpdateDone();
                }
                c.faces = null;
                c.indices = null;
                c.bounds = null;
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

        public override void Render(double time, Framebuffer fbuf, StaticMeshRenderer staticMesh, DynamicMeshRenderer dynamicMesh, VREye eye)
        {
            cur_time += time;
            cur_eye = eye;

            Draws.Clear();
            voxelShader.Set("eyePos", Engine.CurrentPlayer.Position);
            voxelShader.Set("ViewProj", Engine.View[(int)eye] * Engine.Projection[(int)eye]);
            voxelShader.Set("curLayer", 0);
            state = new RenderState(fbuf, voxelShader, new ShaderStorageBuffer[] { MaterialMap.voxelData, drawParams }, null, true, true, DepthFunc.Greater, InverseDepth.Far, InverseDepth.Near, BlendFactor.One, BlendFactor.Zero, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.Back, indexBuffer);
            cubeMapState = new RenderState(gi.CubeMap, voxelGiShader, new ShaderStorageBuffer[] { MaterialMap.voxelData, drawParams }, null, true, true, DepthFunc.Greater, InverseDepth.Far, InverseDepth.Near, BlendFactor.One, BlendFactor.Zero, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.Back, indexBuffer);
            queue.ClearAndBeginRecording();

            cubeMapRender[0].ClearFramebufferBeforeSubmit = true;
            for (int i = 0; i < 6; i++)
            {
                cubeMapRender[i].ClearAndBeginRecording();
            }
        }

        public class ChunkStreamerEnd : Interactable
        {
            ChunkStreamer parent;
            ShaderProgram gi;

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

                    for (int i = 0; i < 6; i++)
                        parent.cubeMapRender[i].RecordDraw(new RenderQueue2.DrawData()
                        {
                            State = parent.cubeMapState,
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
                parent.queue.Submit();
                
                var p = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), 1, 0.001f);
                var fX = Matrix4.LookAt(Engine.CurrentPlayer.Position, Engine.CurrentPlayer.Position + Vector3.UnitX, -Vector3.UnitY);
                var nX = Matrix4.LookAt(Engine.CurrentPlayer.Position, Engine.CurrentPlayer.Position - Vector3.UnitX, -Vector3.UnitY);
                var fY = Matrix4.LookAt(Engine.CurrentPlayer.Position, Engine.CurrentPlayer.Position + Vector3.UnitY, Vector3.UnitZ);
                var nY = Matrix4.LookAt(Engine.CurrentPlayer.Position, Engine.CurrentPlayer.Position - Vector3.UnitY, -Vector3.UnitZ);
                var fZ = Matrix4.LookAt(Engine.CurrentPlayer.Position, Engine.CurrentPlayer.Position + Vector3.UnitZ, -Vector3.UnitY);
                var nZ = Matrix4.LookAt(Engine.CurrentPlayer.Position, Engine.CurrentPlayer.Position - Vector3.UnitZ, -Vector3.UnitY);

                parent.voxelGiShader.Set("eyePos", Engine.CurrentPlayer.Position);
                parent.voxelGiShader.Set("ViewProj", fX * p);
                parent.voxelGiShader.Set("curLayer", 0);
                parent.cubeMapRender[0].EndRecording(new Frustum(fX, p, Engine.CurrentPlayer.Position), Engine.CurrentPlayer.Position);
                parent.cubeMapRender[0].Submit();

                parent.voxelGiShader.Set("eyePos", Engine.CurrentPlayer.Position);
                parent.voxelGiShader.Set("ViewProj", nX * p);
                parent.voxelGiShader.Set("curLayer", 1);
                parent.cubeMapRender[1].EndRecording(new Frustum(nX, p, Engine.CurrentPlayer.Position), Engine.CurrentPlayer.Position);
                parent.cubeMapRender[1].Submit();

                parent.voxelGiShader.Set("eyePos", Engine.CurrentPlayer.Position);
                parent.voxelGiShader.Set("ViewProj", fY * p);
                parent.voxelGiShader.Set("curLayer", 2);
                parent.cubeMapRender[2].EndRecording(new Frustum(fY, p, Engine.CurrentPlayer.Position), Engine.CurrentPlayer.Position);
                parent.cubeMapRender[2].Submit();

                parent.voxelGiShader.Set("eyePos", Engine.CurrentPlayer.Position);
                parent.voxelGiShader.Set("ViewProj", nY * p);
                parent.voxelGiShader.Set("curLayer", 3);
                parent.cubeMapRender[3].EndRecording(new Frustum(nY, p, Engine.CurrentPlayer.Position), Engine.CurrentPlayer.Position);
                parent.cubeMapRender[3].Submit();

                parent.voxelGiShader.Set("eyePos", Engine.CurrentPlayer.Position);
                parent.voxelGiShader.Set("ViewProj", fZ * p);
                parent.voxelGiShader.Set("curLayer", 4);
                parent.cubeMapRender[4].EndRecording(new Frustum(fZ, p, Engine.CurrentPlayer.Position), Engine.CurrentPlayer.Position);
                parent.cubeMapRender[4].Submit();

                parent.voxelGiShader.Set("eyePos", Engine.CurrentPlayer.Position);
                parent.voxelGiShader.Set("ViewProj", nZ * p);
                parent.voxelGiShader.Set("curLayer", 5);
                parent.cubeMapRender[5].EndRecording(new Frustum(nZ, p, Engine.CurrentPlayer.Position), Engine.CurrentPlayer.Position);
                parent.cubeMapRender[5].Submit();

                if (gi == null)
                {
                    gi = new ShaderProgram(ShaderSource.Load(ShaderType.ComputeShader, "Shaders/Deferred/SSGI/compute.glsl"));
                    var colMap = parent.renderer._colorMaps[0].GetHandle(TextureSampler.Default).SetResidency(Residency.Resident);
                    var normMap = parent.renderer._normalMaps[0].GetHandle(TextureSampler.Default).SetResidency(Residency.Resident);
                    var specMap = parent.renderer._specMaps[0].GetHandle(TextureSampler.Default).SetResidency(Residency.Resident);
                    var accumMap = parent.renderer._shadowTexs[0].GetImageHandle(0, 0, PixelInternalFormat.Rgba16f);//.SetResidency(Residency.Resident, AccessMode.Write);

                    gi.Set("ColorMap", colMap);
                    gi.Set("NormalMap", normMap);
                    gi.Set("SpecularMap", specMap);
                    gi.Set("accumulator", accumMap);

                    gi.Set("positionBuf", parent.gi.CubeMapHandle);
                    gi.Set("colorBuf", parent.gi.CubeMapColorHandle);
                }

                //wire up the testing gi shader
                gi.Set("eyePos", Engine.CurrentPlayer.Position);
                gi.Set("eyeDir", Engine.CurrentPlayer.Direction);
                gi.Set("lightPos", Vector3.UnitY * 110);// + Vector3.UnitX * 50);
                gi.Set("lightInten", 50.0f);

                GraphicsDevice.DispatchSyncComputeJob(gi, Engine.Framebuffers[0].Width / 64, Engine.Framebuffers[0].Height, 1);
            }

            public override void Update(double time, World parent)
            {

            }
        }
    }
}
