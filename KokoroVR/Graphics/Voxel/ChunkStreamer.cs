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
        private (Mesh2, int, double)[] ChunkCache;
        private MeshGroup2 buffer;
        private RenderQueue2 queue;
        private double cur_time;
        private RenderState state;
        private ShaderProgram voxelShader;
        private ShaderStorageBuffer drawParams;

        private MeshGroup tmpGrp;
        private Mesh plane;

        public VoxelDictionary MaterialMap { get; private set; }
        public int MaxChunkCount { get; private set; }
        public ChunkStreamerEnd Ender { get; private set; }

        public ChunkStreamer(int max_count)
        {
            int blk_cnt = 8192 * 2;
            buffer = new MeshGroup2(8, 0, 0, 3 * 36 * 32, blk_cnt);
            MaxChunkCount = max_count;
            ChunkList = new Chunk[max_count];

            Ender = new ChunkStreamerEnd(this);

            ChunkCache = new (Mesh2, int, double)[VRAMCacheSize];
            for (int i = 0; i < ChunkCache.Length; i++)
            {
                ChunkCache[i].Item1 = new Mesh2(buffer);
                ChunkCache[i].Item2 = -1;
            }
            queue = new RenderQueue2(blk_cnt, !false);

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
            else
            {
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
            }

            //for (int i = 0; i < ChunkCache[mesh_idx].Item1.AllocIndices.Length; i++)
            //{
            //    var sphere = ChunkCache[mesh_idx].Item1.Parent.Bounds[ChunkCache[mesh_idx].Item1.AllocIndices[i]];
            //    if (f.IsVisible(sphere))
            //        Spheres.Add(sphere);
            //}

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

        float rot_y = 0;
        Vector3 origin = Vector3.Zero;
        List<Vector4> Spheres;
        Frustum f;
        public override void Render(double time, Framebuffer fbuf, StaticMeshRenderer staticMesh, DynamicMeshRenderer dynamicMesh, Matrix4 p, Matrix4 v, VREye eye)
        {
            Spheres = new List<Vector4>();

            cur_time = time;
            rot_y += 0.001f * (float)Math.PI;
            origin = 40 * new Vector3((float)Math.Sin(rot_y) * (float)Math.Cos(0), (float)Math.Sin(rot_y) * (float)Math.Sin(0), (float)Math.Cos(rot_y));

            Engine.CurrentPlayer.Position = origin;
            Engine.View[0] = Matrix4.LookAt(origin, Vector3.Zero, Vector3.UnitY);
            f = new Frustum(Engine.View[0], p, origin);

            voxelShader.Set("View", Matrix4.LookAt(origin, Vector3.Zero, Vector3.UnitY));
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
                tmpGrp = new MeshGroup(MeshGroupVertexFormat.X32F_Y32F_Z32F, 50000, 50000);
                plane = Kokoro.Graphics.Prefabs.SphereFactory.Create(tmpGrp);
            }

            MeshGroup tmpGrp;
            Mesh plane;

            public override void Render(double time, Framebuffer fbuf, StaticMeshRenderer staticMesh, DynamicMeshRenderer dynamicMesh, Matrix4 p, Matrix4 v, VREye eye)
            {
                var f = new Frustum(Matrix4.LookAt(parent.origin, Vector3.Zero, Vector3.UnitY), p, parent.origin);
                parent.queue.EndRecording(f);
                parent.queue.Submit();
                //Texture.Default.GetHandle(TextureSampler.Default).SetResidency(Residency.Resident);

                //for (int i = 0; i < parent.Spheres.Count; i++)
                //    staticMesh.DrawC(plane, Matrix4.Scale(parent.Spheres[i].W) * Matrix4.CreateTranslation(parent.Spheres[i].Xyz), Texture.Default.GetHandle(TextureSampler.Default));
                //plane = Kokoro.Graphics.Prefabs.QuadFactory.Create(tmpGrp, 1, 1, Vector3.UnitX, new Vector3(0, 2, 1));
                //Texture.Default.GetHandle(TextureSampler.Default).SetResidency(Residency.Resident);
                //staticMesh.DrawC(plane, Matrix4.Identity, Texture.Default.GetHandle(TextureSampler.Default));
            }

            public override void Update(double time, World parent)
            {

            }
        }
    }
}
