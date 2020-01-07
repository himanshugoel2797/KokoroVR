using Kokoro.Graphics;
using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Graphics
{
    public class StaticMeshRenderer
    {
        struct csn_struct
        {
            public Mesh m;
            public Matrix4 w;
            public TextureHandle c;
            public TextureHandle s;
            public TextureHandle n;
        }
        struct cs_struct
        {
            public Mesh m;
            public Matrix4 w;
            public TextureHandle c;
            public TextureHandle s;
        }
        struct c_struct
        {
            public Mesh m;
            public Matrix4 w;
            public TextureHandle c;
        }

        int _target_cnt;
        ShaderProgram[] csn_shader, cs_shader, c_shader;
        ShaderStorageBuffer csn_ssbo, cs_ssbo, c_ssbo;
        ShaderStorageBuffer csn_w_ssbo, cs_w_ssbo, c_w_ssbo;
        RenderState[] csn_state, cs_state, c_state;
        RenderQueue _queue;

        Matrix4[] projs, views;
        List<csn_struct> csn_objs;
        List<cs_struct> cs_objs;
        List<c_struct> c_objs;
        IRenderer renderer;

        const int csn_size = (3 * 8);
        const int cs_size = (2 * 8);
        const int c_size = (1 * 8);

        public StaticMeshRenderer(int maxDraws, IRenderer renderer)
        {
            csn_objs = new List<csn_struct>();
            cs_objs = new List<cs_struct>();
            c_objs = new List<c_struct>();

            csn_ssbo = new ShaderStorageBuffer(maxDraws * csn_size, true);
            cs_ssbo = new ShaderStorageBuffer(maxDraws * cs_size, true);
            c_ssbo = new ShaderStorageBuffer(maxDraws * c_size, true);

            csn_w_ssbo = new ShaderStorageBuffer(maxDraws * sizeof(float) * 16, true);
            cs_w_ssbo = new ShaderStorageBuffer(maxDraws * sizeof(float) * 16, true);
            c_w_ssbo = new ShaderStorageBuffer(maxDraws * sizeof(float) * 16, true);

            this.renderer = renderer;
            _target_cnt = renderer.Framebuffers.Length;
            csn_state = new RenderState[_target_cnt];
            cs_state = new RenderState[_target_cnt];
            c_state = new RenderState[_target_cnt];
            csn_shader = new ShaderProgram[_target_cnt];
            cs_shader = new ShaderProgram[_target_cnt];
            c_shader = new ShaderProgram[_target_cnt];

            _queue = new RenderQueue(maxDraws * 3, true);
            _queue.ClearFramebufferBeforeSubmit = false;

            Resize(renderer.Framebuffers);

            Engine.WindowResized += (a, b) => Resize(renderer.Framebuffers);
        }

        private void Resize(Framebuffer[] dest_fbufs)
        {
            for (int i = 0; i < _target_cnt; i++)
            {
                csn_shader[i] = new ShaderProgram(ShaderSource.Load(ShaderType.VertexShader, "Shaders/Deferred/Mesh/CSN/vertex.glsl"),
                                                ShaderSource.Load(ShaderType.FragmentShader, "Shaders/Deferred/Mesh/CSN/fragment.glsl"));

                cs_shader[i] = new ShaderProgram(ShaderSource.Load(ShaderType.VertexShader, "Shaders/Deferred/Mesh/CS/vertex.glsl"),
                                                ShaderSource.Load(ShaderType.FragmentShader, "Shaders/Deferred/Mesh/CS/fragment.glsl"));

                c_shader[i] = new ShaderProgram(ShaderSource.Load(ShaderType.VertexShader, "Shaders/Deferred/Mesh/C/vertex.glsl"),
                                                ShaderSource.Load(ShaderType.FragmentShader, "Shaders/Deferred/Mesh/C/fragment.glsl"));

                csn_state[i] = new RenderState(dest_fbufs[i], csn_shader[i], new ShaderStorageBuffer[] { csn_ssbo, csn_w_ssbo }, null, true, true, DepthFunc.Greater, InverseDepth.Far, InverseDepth.Near, BlendFactor.One, BlendFactor.Zero, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.Back);
                cs_state[i] = new RenderState(dest_fbufs[i], cs_shader[i], new ShaderStorageBuffer[] { cs_ssbo, cs_w_ssbo }, null, true, true, DepthFunc.Greater, InverseDepth.Far, InverseDepth.Near, BlendFactor.One, BlendFactor.Zero, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.Back);
                c_state[i] = new RenderState(dest_fbufs[i], c_shader[i], new ShaderStorageBuffer[] { c_ssbo, c_w_ssbo }, null, true, true, DepthFunc.Greater, InverseDepth.Far, InverseDepth.Near, BlendFactor.One, BlendFactor.Zero, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.Back);
            }
        }

        public void SetMatrices(Matrix4[] projs, Matrix4[] views)
        {
            this.projs = projs;
            this.views = views;
        }

        public void DrawCSN(Mesh m, Matrix4 w, TextureHandle color, TextureHandle specular, TextureHandle normal)
        {
            csn_objs.Add(new csn_struct()
            {
                m = m,
                w = w,
                c = color,
                s = specular,
                n = normal,
            });
        }

        public void DrawCS(Mesh m, Matrix4 w, TextureHandle color, TextureHandle specular)
        {
            cs_objs.Add(new cs_struct()
            {
                m = m,
                w = w,
                c = color,
                s = specular,
            });
        }

        public void DrawC(Mesh m, Matrix4 w, TextureHandle color)
        {
            c_objs.Add(new c_struct()
            {
                m = m,
                w = w,
                c = color,
            });
        }

        public void Submit()
        {
            //Upload data
            var csn_drawcmds = new RenderQueue.MeshData[csn_objs.Count];
            var cs_drawcmds = new RenderQueue.MeshData[cs_objs.Count];
            var c_drawcmds = new RenderQueue.MeshData[c_objs.Count];
            unsafe
            {
                byte* b_p, b_w_p;
                float* f_p;
                long* l_p;

                {
                    b_p = csn_ssbo.Update();
                    b_w_p = csn_w_ssbo.Update();
                    l_p = (long*)b_p;
                    f_p = (float*)b_p;
                    for (int j = 0; j < csn_objs.Count; j++)
                    {
                        l_p[(j * csn_size) / sizeof(long) + 0] = csn_objs[j].c;
                        l_p[(j * csn_size) / sizeof(long) + 1] = csn_objs[j].s;
                        l_p[(j * csn_size) / sizeof(long) + 2] = csn_objs[j].n;

                        var m_fs = csn_objs[j].w;
                        *(Matrix4*)&b_w_p[j * 16 * sizeof(float)] = m_fs;

                        csn_drawcmds[j].BaseInstance = j;
                        csn_drawcmds[j].InstanceCount = 1;
                        csn_drawcmds[j].Mesh = csn_objs[j].m;
                    }
                    csn_w_ssbo.UpdateDone();
                    csn_ssbo.UpdateDone();
                }

                {
                    b_p = cs_ssbo.Update();
                    b_w_p = cs_w_ssbo.Update();
                    l_p = (long*)b_p;
                    f_p = (float*)b_p;
                    for (int j = 0; j < cs_objs.Count; j++)
                    {
                        l_p[(j * cs_size) / sizeof(long) + 0] = cs_objs[j].c;
                        l_p[(j * cs_size) / sizeof(long) + 1] = cs_objs[j].s;

                        var m_fs = cs_objs[j].w;
                        *(Matrix4*)&b_w_p[j * 16 * sizeof(float)] = m_fs;

                        cs_drawcmds[j].BaseInstance = j;
                        cs_drawcmds[j].InstanceCount = 1;
                        cs_drawcmds[j].Mesh = cs_objs[j].m;
                    }
                    cs_w_ssbo.UpdateDone();
                    cs_ssbo.UpdateDone();
                }

                {
                    b_p = c_ssbo.Update();
                    b_w_p = c_w_ssbo.Update();
                    l_p = (long*)b_p;
                    f_p = (float*)b_p;
                    for (int j = 0; j < c_objs.Count; j++)
                    {
                        l_p[(j * c_size) / sizeof(long) + 0] = c_objs[j].c;

                        var m_fs = c_objs[j].w;
                        *(Matrix4*)&b_w_p[j * 16 * sizeof(float)] = m_fs;

                        c_drawcmds[j].BaseInstance = j;
                        c_drawcmds[j].InstanceCount = 1;
                        c_drawcmds[j].Mesh = c_objs[j].m;
                    }
                    c_w_ssbo.UpdateDone();
                    c_ssbo.UpdateDone();
                }

            }

            _queue.ClearAndBeginRecording();
            for (int i = 0; i < _target_cnt; i++)
            {
                //Submit draws
                csn_shader[i].Set("View", views[i]);
                csn_shader[i].Set("Proj", projs[i]);

                cs_shader[i].Set("View", views[i]);
                cs_shader[i].Set("Proj", projs[i]);

                c_shader[i].Set("View", views[i]);
                c_shader[i].Set("Proj", projs[i]);

                if (csn_drawcmds.Length > 0)
                    _queue.RecordDraw(new RenderQueue.DrawData()
                    {
                        State = csn_state[i],
                        Meshes = csn_drawcmds
                    });
                if (cs_drawcmds.Length > 0)
                    _queue.RecordDraw(new RenderQueue.DrawData()
                    {
                        State = cs_state[i],
                        Meshes = cs_drawcmds
                    });

                if (c_drawcmds.Length > 0)
                    _queue.RecordDraw(new RenderQueue.DrawData()
                    {
                        State = c_state[i],
                        Meshes = c_drawcmds
                    });
            }
            _queue.EndRecording();
            _queue.Submit();

            c_objs.Clear();
            cs_objs.Clear();
            csn_objs.Clear();
        }
    }
}
