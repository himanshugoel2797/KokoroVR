using Kokoro.Graphics;
using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Graphics
{
    public class DeferredRenderer : IRenderer
    {
        private int _targetCount;
        private Framebuffer[] _destFramebuffers;
        private Framebuffer[] _accumulators;

        public Texture[] _colorMaps;
        public Texture[] _normalMaps;
        public Texture[] _specMaps;
        public Texture[] _depthMaps;
        public Texture[] _accumulatorTexs;
        public Texture[] _shadowTexs;

        private RenderState[] _pointL_state;
        private RenderState[] _spotL_state;
        private RenderState[] _direcL_state;
        private RenderState[] _state;
        private RenderQueue[] _queue;
        private RenderQueue[] _queue_final;
        private ShaderProgram[] _pointLightShader;
        private ShaderProgram[] _spotLightShader;
        private ShaderProgram[] _directionalLightShader;
        private ShaderProgram[] _outputShader;
        private Mesh _fst;

        public Framebuffer[] Framebuffers { get; private set; }
        public LightManager Lights { get; private set; }

        public DeferredRenderer(Framebuffer[] dest, LightManager lights)
        {
            _targetCount = dest.Length;
            _destFramebuffers = dest;
            Lights = lights;

            _fst = Kokoro.Graphics.Prefabs.FullScreenTriangleFactory.Create(Engine.iMeshGroup);

            //Color: 16DiffR:16DiffG:16DiffB:16Roughness
            //Normal: 8NX|8NY|8NZ:32WX:32WY:32WZ
            //Specular: 16SpecR:16SpecG:16SpecB
            //Depth: 32D

            Framebuffers = new Framebuffer[_targetCount];
            _accumulators = new Framebuffer[_targetCount];
            _pointL_state = new RenderState[_targetCount];
            _spotL_state = new RenderState[_targetCount];
            _direcL_state = new RenderState[_targetCount];
            _state = new RenderState[_targetCount];
            _queue = new RenderQueue[_targetCount];
            _queue_final = new RenderQueue[_targetCount];
            _accumulatorTexs = new Texture[_targetCount];
            _shadowTexs = new Texture[_targetCount];
            _colorMaps = new Texture[_targetCount];
            _normalMaps = new Texture[_targetCount];
            _specMaps = new Texture[_targetCount];
            _depthMaps = new Texture[_targetCount];
            _pointLightShader = new ShaderProgram[_targetCount];
            _spotLightShader = new ShaderProgram[_targetCount];
            _directionalLightShader = new ShaderProgram[_targetCount];
            _outputShader = new ShaderProgram[_targetCount];
            Resize(dest);

            Engine.WindowResized += (a,b) => Resize(_destFramebuffers);
        }

        private void Resize(Framebuffer[] dest)
        {
            for (int i = 0; i < _targetCount; i++)
            {
                var colorSrc = new FramebufferTextureSource(dest[i].Width, dest[i].Height, 1)
                {
                    Format = PixelFormat.Bgra,
                    InternalFormat = PixelInternalFormat.Rgba16f,
                    PixelType = PixelType.HalfFloat
                };
                var color = new Texture();
                color.SetData(colorSrc, 0);
                _colorMaps[i] = color;

                var normalSrc = new FramebufferTextureSource(dest[i].Width, dest[i].Height, 1)
                {
                    Format = PixelFormat.Bgra,
                    InternalFormat = PixelInternalFormat.Rgba32f,
                    PixelType = PixelType.Float
                };
                var normal = new Texture();
                normal.SetData(normalSrc, 0);
                _normalMaps[i] = normal;

                var specularSrc = new FramebufferTextureSource(dest[i].Width, dest[i].Height, 1)
                {
                    Format = PixelFormat.Bgra,
                    InternalFormat = PixelInternalFormat.Rgba16f,
                    PixelType = PixelType.HalfFloat
                };
                var specular = new Texture();
                specular.SetData(specularSrc, 0);
                _specMaps[i] = specular;

                var depthSrc = new DepthTextureSource(dest[i].Width, dest[i].Height)
                {
                    InternalFormat = PixelInternalFormat.DepthComponent32f
                };
                var depth = new Texture();
                depth.SetData(depthSrc, 0);
                _depthMaps[i] = depth;

                Framebuffers[i] = new Framebuffer(dest[i].Width, dest[i].Height);
                Framebuffers[i][FramebufferAttachment.ColorAttachment0] = color;
                Framebuffers[i][FramebufferAttachment.ColorAttachment1] = normal;
                Framebuffers[i][FramebufferAttachment.ColorAttachment2] = specular;
                Framebuffers[i][FramebufferAttachment.DepthAttachment] = depth;

                var accumulatorSrc = new FramebufferTextureSource(dest[i].Width, dest[i].Height, 1)
                {
                    Format = PixelFormat.Bgra,
                    InternalFormat = PixelInternalFormat.Rgba16f,
                    PixelType = PixelType.HalfFloat
                };
                _accumulatorTexs[i] = new Texture();
                _accumulatorTexs[i].SetData(accumulatorSrc, 0);

                var shadowSrc = new FramebufferTextureSource(dest[i].Width, dest[i].Height, 1)
                {
                    Format = PixelFormat.Bgra,
                    InternalFormat = PixelInternalFormat.Rgba16f,
                    PixelType = PixelType.HalfFloat
                };
                _shadowTexs[i] = new Texture();
                _shadowTexs[i].SetData(shadowSrc, 0);

                _accumulators[i] = new Framebuffer(dest[i].Width, dest[i].Height);
                _accumulators[i][FramebufferAttachment.ColorAttachment0] = _accumulatorTexs[i];

                _pointLightShader[i] = new ShaderProgram(
                            ShaderSource.Load(ShaderType.VertexShader, "Shaders/Deferred/vertex.glsl"),
                            ShaderSource.Load(ShaderType.FragmentShader, "Shaders/Deferred/Point/fragment.glsl"));
                _spotLightShader[i] = new ShaderProgram(
                            ShaderSource.Load(ShaderType.VertexShader, "Shaders/Deferred/vertex.glsl"),
                            ShaderSource.Load(ShaderType.FragmentShader, "Shaders/Deferred/Spot/fragment.glsl"));
                _directionalLightShader[i] = new ShaderProgram(
                            ShaderSource.Load(ShaderType.VertexShader, "Shaders/Deferred/vertex.glsl"),
                            ShaderSource.Load(ShaderType.FragmentShader, "Shaders/Deferred/Directional/fragment.glsl"));
                _outputShader[i] = new ShaderProgram(
                            ShaderSource.Load(ShaderType.VertexShader, "Shaders/Deferred/vertex.glsl"),
                            ShaderSource.Load(ShaderType.FragmentShader, "Shaders/Deferred/Output/fragment.glsl"));

                GraphicsDevice.AlphaEnabled = true;
                _pointL_state[i] = new RenderState(_accumulators[i], _pointLightShader[i], new StorageBuffer[] { Lights.pointLights_buffer }, null, false, true, DepthFunc.Always, InverseDepth.Far, InverseDepth.Near, BlendFactor.One, BlendFactor.One, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.None);
                _spotL_state[i] = new RenderState(_accumulators[i], _spotLightShader[i], new StorageBuffer[] { Lights.spotLights_buffer }, null, false, true, DepthFunc.Always, InverseDepth.Far, InverseDepth.Near, BlendFactor.One, BlendFactor.One, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.None);
                _direcL_state[i] = new RenderState(_accumulators[i], _directionalLightShader[i], new StorageBuffer[] { Lights.direcLights_buffer }, null, false, true, DepthFunc.Always, InverseDepth.Far, InverseDepth.Near, BlendFactor.One, BlendFactor.One, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.None);
                _state[i] = new RenderState(_destFramebuffers[i], _outputShader[i], null, null, false, true, DepthFunc.Always, InverseDepth.Far, InverseDepth.Near, BlendFactor.One, BlendFactor.Zero, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.None);

                _queue_final[i] = new RenderQueue(4, true);
                _queue_final[i].ClearFramebufferBeforeSubmit = false;

                _queue[i] = new RenderQueue(4, true);
                _queue[i].ClearFramebufferBeforeSubmit = false;

                var colorHandle = _colorMaps[i].GetHandle(TextureSampler.Default);
                colorHandle.SetResidency(Residency.Resident);
                _pointLightShader[i].Set("ColorMap", colorHandle);

                var normalHandle = _normalMaps[i].GetHandle(TextureSampler.Default);
                normalHandle.SetResidency(Residency.Resident);
                _pointLightShader[i].Set("NormalMap", normalHandle);

                var specHandle = _specMaps[i].GetHandle(TextureSampler.Default);
                specHandle.SetResidency(Residency.Resident);
                _pointLightShader[i].Set("SpecularMap", specHandle);

                _spotLightShader[i].Set("ColorMap", colorHandle);
                _spotLightShader[i].Set("NormalMap", normalHandle);
                _spotLightShader[i].Set("SpecularMap", specHandle);

                _directionalLightShader[i].Set("ColorMap", colorHandle);
                _directionalLightShader[i].Set("NormalMap", normalHandle);
                _directionalLightShader[i].Set("SpecularMap", specHandle);

                var accumHandle = _accumulatorTexs[i].GetHandle(TextureSampler.Default);
                accumHandle.SetResidency(Residency.Resident);
                _outputShader[i].Set("Accumulator", accumHandle);

                var shadowHandle = _shadowTexs[i].GetImageHandle(0, 0, PixelInternalFormat.Rgba16f);
                shadowHandle.SetResidency(Residency.Resident, AccessMode.ReadWrite);
                _outputShader[i].Set("Shadow", shadowHandle);

                _queue[i].ClearAndBeginRecording();
                _queue[i].RecordDraw(new RenderQueue.DrawData()
                {
                    State = _state[i],
                    Meshes = new RenderQueue.MeshData[]
                    {
                        new RenderQueue.MeshData()
                        {
                            BaseInstance = 0,
                            InstanceCount = 1,
                            Mesh = _fst
                        }
                    }
                });
                _queue[i].EndRecording();
            }
        }

        public void Clear()
        {
            var fbuf = GraphicsDevice.Framebuffer;
            var clearDepth = GraphicsDevice.ClearDepth;
            var clearCol = GraphicsDevice.ClearColor;
            var dWrite = GraphicsDevice.DepthWriteEnabled;
            for (int i = 0; i < _targetCount; i++)
            {
                GraphicsDevice.DepthWriteEnabled = true;
                //GraphicsDevice.ClearColor = new Vector4(1, 1, 1, 1);
                GraphicsDevice.ClearDepth = InverseDepth.ClearDepth;
                GraphicsDevice.Framebuffer = Framebuffers[i];
                GraphicsDevice.Clear();

                GraphicsDevice.DepthWriteEnabled = true;
                //GraphicsDevice.ClearColor = new Vector4(0, 1, 1, 1);
                GraphicsDevice.ClearDepth = InverseDepth.ClearDepth;
                GraphicsDevice.Framebuffer = _accumulators[i];
                GraphicsDevice.Clear();
            }
            GraphicsDevice.DepthWriteEnabled = dWrite;
            GraphicsDevice.ClearColor = clearCol;
            GraphicsDevice.ClearDepth = clearDepth;
            GraphicsDevice.Framebuffer = fbuf;
        }

        public void Submit(Matrix4[] View, Matrix4[] Proj, Vector3 pos)
        {
            //Apply lighting and other screen space effects and write to the output framebuffers
            for (int i = 0; i < _targetCount; i++)
            {
                Matrix4 v = View[i];
                Matrix4 p = Proj[i];

                _pointLightShader[i].Set("View", v);
                _pointLightShader[i].Set("Proj", p);
                _pointLightShader[i].Set("EyePos", pos);

                _spotLightShader[i].Set("View", v);
                _spotLightShader[i].Set("Proj", p);
                _spotLightShader[i].Set("EyePos", pos);

                _directionalLightShader[i].Set("View", v);
                _directionalLightShader[i].Set("Proj", p);
                _directionalLightShader[i].Set("EyePos", pos);

                //TODO apply shadow
                _queue_final[i].ClearAndBeginRecording();
                _queue_final[i].RecordDraw(new RenderQueue.DrawData()
                {
                    State = _pointL_state[i],
                    Meshes = new RenderQueue.MeshData[]{
                        new RenderQueue.MeshData()
                        {
                            BaseInstance = 0,
                            InstanceCount = Lights.pointLightCnt,
                            Mesh = _fst
                        }
                    }
                });
                _queue_final[i].RecordDraw(new RenderQueue.DrawData()
                {
                    State = _spotL_state[i],
                    Meshes = new RenderQueue.MeshData[]{
                        new RenderQueue.MeshData()
                        {
                            BaseInstance = 0,
                            InstanceCount = Lights.spotLightCnt,
                            Mesh = _fst
                        }
                    }
                });
                _queue_final[i].RecordDraw(new RenderQueue.DrawData()
                {
                    State = _direcL_state[i],
                    Meshes = new RenderQueue.MeshData[]{
                        new RenderQueue.MeshData()
                        {
                            BaseInstance = 0,
                            InstanceCount = Lights.direcLightCnt,
                            Mesh = _fst
                        }
                    }
                });
                _queue_final[i].EndRecording();
                _queue_final[i].Submit();
                _queue[i].Submit();
            }
        }

    }
}
