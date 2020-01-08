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
        public const int VRAMCacheSize = 384;  //TODO make this depend on total available vram

        private Chunk[] ChunkList;
        private (Mesh2, int, double)[] ChunkCache;
        private MeshGroup2 buffer;
        private RenderQueue2 queue;
        private double cur_time;
        private VREye cur_eye;
        private RenderState state;
        private ShaderProgram voxelShader;
        private ShaderStorageBuffer drawParams;

        public VoxelDictionary MaterialMap { get; private set; }
        public int MaxChunkCount { get; private set; }
        public ChunkStreamerEnd Ender { get; private set; }

        public ChunkStreamer(int max_count)
        {
            int blk_cnt = 1024;
            buffer = new MeshGroup2(8, 0, 0, 3 * 36 * 1024, blk_cnt);
            MaxChunkCount = max_count;
            ChunkList = new Chunk[max_count];

            Ender = new ChunkStreamerEnd(this);

            ChunkCache = new (Mesh2, int, double)[VRAMCacheSize];
            for (int i = 0; i < ChunkCache.Length; i++)
            {
                ChunkCache[i].Item1 = new Mesh2(buffer);
                ChunkCache[i].Item2 = -1;
                ChunkCache[i].Item3 = double.MinValue;
            }
            queue = new RenderQueue2(blk_cnt, true);

            drawParams = new ShaderStorageBuffer(blk_cnt * 4 * sizeof(uint), false);

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

            //Don't proceed if this chunk isn't supposed to be visible
            if (!Engine.Frustums[(int)cur_eye].IsVisible(new Vector4(offset, (float)(ChunkConstants.Side * System.Math.Sqrt(3)))))
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

                c.update_pending = true;
            }

            //Upload the current mesh state if it has been updated recently
            if (c.update_pending)
            {
                unsafe
                {
                    var b = c.faces.ToArray();
                    fixed (byte* b_p = b)
                        ChunkCache[mesh_idx].Item1.Reallocate(b_p, null, null, 6, Vector3.One * ChunkConstants.Side * -0.5f + offset, b.Length / 4);

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
                c.update_pending = false;
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
                        Mesh = ChunkCache[mesh_idx].Item1
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

        float angle = 0;
        public override void Render(double time, Framebuffer fbuf, StaticMeshRenderer staticMesh, DynamicMeshRenderer dynamicMesh, VREye eye)
        {
            angle += (float)(time * 0.5f);
            Engine.CurrentPlayer.Position = Vector3.FromSpherical(new Vector3(100, angle, 0));
            Engine.View[0] = Matrix4.LookAt(Engine.CurrentPlayer.Position, Vector3.Zero, Vector3.UnitY);
            Engine.Frustums[0] = new Frustum(Engine.View[0], Engine.Projection[0], Engine.CurrentPlayer.Position);
            cur_time += time;
            cur_eye = eye;
            //voxelShader.Set("View", Engine.View[(int)eye]);
            voxelShader.Set("ViewProj", Engine.View[(int)eye] * Engine.Projection[(int)eye]);
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

            public override void Render(double time, Framebuffer fbuf, StaticMeshRenderer staticMesh, DynamicMeshRenderer dynamicMesh, VREye eye)
            {
                parent.queue.EndRecording(Engine.Frustums[(int)eye]);
                parent.queue.Submit();
            }

            public override void Update(double time, World parent)
            {

            }
        }
    }
}
