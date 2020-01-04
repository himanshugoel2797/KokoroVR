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
        public const int VRAMCacheSize = 64;  //TODO make this depend on total available vram

        private Chunk[] ChunkList;
        private (Mesh2[], int, double)[] ChunkCache;
        private MeshGroup2 buffer;
        private RenderQueue2 queue;
        private double cur_time;
        private RenderState state;
        private ShaderProgram voxelShader;
        private ShaderStorageBuffer drawParams;

        public VoxelDictionary MaterialMap { get; private set; }
        public int MaxChunkCount { get; private set; }
        public ChunkStreamerEnd Ender { get; private set; }

        public ChunkStreamer(int max_count)
        {
            int blk_cnt = 8192;
            buffer = new MeshGroup2(8, 0, 0, 3 * 512, blk_cnt);
            MaxChunkCount = max_count;
            ChunkList = new Chunk[max_count];

            Ender = new ChunkStreamerEnd(this);

            ChunkCache = new (Mesh2[], int, double)[VRAMCacheSize];
            for (int i = 0; i < ChunkCache.Length; i++)
            {
                ChunkCache[i].Item2 = -1;
                ChunkCache[i].Item1 = new Mesh2[6];
                for (int j = 0; j < 6; j++)
                    ChunkCache[i].Item1[j] = new Mesh2(buffer);
            }
            queue = new RenderQueue2(6 * blk_cnt, !false);

            drawParams = new ShaderStorageBuffer(blk_cnt * 8 * sizeof(uint), false);

            voxelShader = new ShaderProgram(ShaderSource.LoadV("Shaders/Deferred/Voxel/vertex.glsl", 8, 0, 0),
                                            ShaderSource.Load(ShaderType.FragmentShader, "Shaders/Deferred/Voxel/fragment.glsl"));

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

                unsafe
                {
                    for (int i = 0; i < 6; i++)
                    {
                        var b = c.faces[i].ToArray();
                        fixed (byte* b_p = b)
                            ChunkCache[mesh_idx].Item1[i].Reallocate(b_p, null, null, b.Length / 4);
                    }

                    var dP_p = (float*)drawParams.Update();
                    for (int i = 0; i < 6; i++)
                        for (int j = 0; j < ChunkCache[mesh_idx].Item1[i].AllocIndices.Length; j++)
                        {
                            int idx = ChunkCache[mesh_idx].Item1[i].AllocIndices[j];
                            dP_p[idx * 8 + 0] = offset.X - ChunkConstants.Side * 0.5f;
                            dP_p[idx * 8 + 1] = offset.Y - ChunkConstants.Side * 0.5f;
                            dP_p[idx * 8 + 2] = offset.Z - ChunkConstants.Side * 0.5f;
                            dP_p[idx * 8 + 4] = 0;
                            dP_p[idx * 8 + 5] = 0;
                            dP_p[idx * 8 + 6] = 0;

                            switch (i)
                            {
                                case 0:
                                    dP_p[idx * 8 + 5] = -1;
                                    break;
                                case 1:
                                    dP_p[idx * 8 + 5] = 1;
                                    break;
                                case 2:
                                    dP_p[idx * 8 + 4] = -1;
                                    break;
                                case 3:
                                    dP_p[idx * 8 + 4] = 1;
                                    break;
                                case 4:
                                    dP_p[idx * 8 + 6] = -1;
                                    break;
                                case 5:
                                    dP_p[idx * 8 + 6] = 1;
                                    break;
                            }
                        }
                    drawParams.UpdateDone();
                }
                c.update_pending = false;
            }
            else
            {
                //Upload the current mesh state if it has been updated recently
                if (c.update_pending)
                {
                    unsafe
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            var b = c.faces[i].ToArray();
                            fixed (byte* b_p = b)
                                ChunkCache[mesh_idx].Item1[i].Reallocate(b_p, null, null, b.Length / 4);
                        }

                        var dP_p = (float*)drawParams.Update();
                        for (int i = 0; i < 6; i++)
                            for (int j = 0; j < ChunkCache[mesh_idx].Item1[i].AllocIndices.Length; j++)
                            {
                                int idx = ChunkCache[mesh_idx].Item1[i].AllocIndices[j];
                                dP_p[idx * 8 + 0] = offset.X - ChunkConstants.Side * 0.5f;
                                dP_p[idx * 8 + 1] = offset.Y - ChunkConstants.Side * 0.5f;
                                dP_p[idx * 8 + 2] = offset.Z - ChunkConstants.Side * 0.5f;
                                dP_p[idx * 8 + 4] = 0;
                                dP_p[idx * 8 + 5] = 0;
                                dP_p[idx * 8 + 6] = 0;

                                switch (i)
                                {
                                    case 0:
                                        dP_p[idx * 8 + 5] = -1;
                                        break;
                                    case 1:
                                        dP_p[idx * 8 + 5] = 1;
                                        break;
                                    case 2:
                                        dP_p[idx * 8 + 4] = -1;
                                        break;
                                    case 3:
                                        dP_p[idx * 8 + 4] = 1;
                                        break;
                                    case 4:
                                        dP_p[idx * 8 + 6] = -1;
                                        break;
                                    case 5:
                                        dP_p[idx * 8 + 6] = 1;
                                        break;
                                }
                            }
                        drawParams.UpdateDone();
                    }
                    c.update_pending = false;
                }
            }

            //Record this chunk's draw
            queue.RecordDraw(new RenderQueue2.DrawData()
            {
                State = state,
                Meshes = new RenderQueue2.MeshData[]
                {
                    new RenderQueue2.MeshData()
                    {
                        BaseInstance = 0,
                        InstanceCount = 1,
                        Mesh = ChunkCache[mesh_idx].Item1[0]
                    },
                    new RenderQueue2.MeshData()
                    {
                        BaseInstance = 0,
                        InstanceCount = 1,
                        Mesh = ChunkCache[mesh_idx].Item1[1]
                    },
                    new RenderQueue2.MeshData()
                    {
                        BaseInstance = 0,
                        InstanceCount = 1,
                        Mesh = ChunkCache[mesh_idx].Item1[2]
                    },
                    new RenderQueue2.MeshData()
                    {
                        BaseInstance = 0,
                        InstanceCount = 1,
                        Mesh = ChunkCache[mesh_idx].Item1[3]
                    },
                    new RenderQueue2.MeshData()
                    {
                        BaseInstance = 0,
                        InstanceCount = 1,
                        Mesh = ChunkCache[mesh_idx].Item1[4]
                    },
                    new RenderQueue2.MeshData()
                    {
                        BaseInstance = 0,
                        InstanceCount = 1,
                        Mesh = ChunkCache[mesh_idx].Item1[5]
                    },
                }
            });
        }

        public override void Update(double time, World parent)
        {
            for (int i = 0; i < ChunkList.Length; i++)
            {
                if (ChunkList[i] != null) ChunkList[i].Update();    //Trigger any necessary rebuilds
            }
        }

        float rot_y = 0;

        public override void Render(double time, Framebuffer fbuf, StaticMeshRenderer staticMesh, DynamicMeshRenderer dynamicMesh, Matrix4 p, Matrix4 v, VREye eye)
        {
            cur_time = time;
            rot_y += 0.0005f;
            voxelShader.Set("View", Matrix4.CreateRotationY(rot_y) * Matrix4.CreateRotationX(rot_y * 0.25f) * v);
            voxelShader.Set("Proj", p);
            state = new RenderState(fbuf, voxelShader, new ShaderStorageBuffer[] { MaterialMap.voxelData, drawParams }, null, true, true, DepthFunc.Greater, InverseDepth.Far, InverseDepth.Near, BlendFactor.One, BlendFactor.Zero, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.Back);
            queue.ClearAndBeginRecording();
        }

        public class ChunkStreamerEnd : Interactable
        {
            ChunkStreamer parent;
            internal ChunkStreamerEnd(ChunkStreamer p)
            {
                parent = p;
            }

            public override void Render(double time, Framebuffer fbuf, StaticMeshRenderer staticMesh, DynamicMeshRenderer dynamicMesh, Matrix4 p, Matrix4 v, VREye eye)
            {
                parent.queue.EndRecording();
                parent.queue.Submit();
            }

            public override void Update(double time, World parent)
            {

            }
        }
    }
}
