using Kokoro.Graphics;
using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Graphics
{
    public class DeferredRenderer
    {
        private int _targetCount;
        private Framebuffer[] _destFramebuffers;
        private Framebuffer[] _accumulators;

        private Texture[] _colorMaps;
        private Texture[] _normalMaps;
        private Texture[] _specMaps;
        private Texture[] _depthMaps;

        private RenderState[] _pointL_state;
        private RenderState[] _spotL_state;
        private RenderState[] _direcL_state;
        private RenderState[] _state;
        private RenderQueue[] _queue;
        private ShaderProgram _pointLightShader;
        private ShaderProgram _spotLightShader;
        private ShaderProgram _directionalLightShader;
        private ShaderProgram _outputShader;
        private Mesh _fst;

        public Framebuffer[] Framebuffers { get; private set; }
        public LightManager Lights { get; private set; }

        public DeferredRenderer(Framebuffer[] dest, LightManager lights)
        {
            _targetCount = dest.Length;
            _destFramebuffers = dest;
            Lights = lights;

            _pointLightShader = new ShaderProgram(
                        ShaderSource.Load(ShaderType.VertexShader, "Shaders/Deferred/Point/vertex.glsl"),
                        ShaderSource.Load(ShaderType.FragmentShader, "Shaders/Deferred/Point/fragment.glsl"));
            _spotLightShader = new ShaderProgram(
                        ShaderSource.Load(ShaderType.VertexShader, "Shaders/Deferred/Spot/vertex.glsl"),
                        ShaderSource.Load(ShaderType.FragmentShader, "Shaders/Deferred/Spot/fragment.glsl"));
            _directionalLightShader = new ShaderProgram(
                        ShaderSource.Load(ShaderType.VertexShader, "Shaders/Deferred/Directional/vertex.glsl"),
                        ShaderSource.Load(ShaderType.FragmentShader, "Shaders/Deferred/Directional/fragment.glsl"));
            _outputShader = new ShaderProgram(
                        ShaderSource.Load(ShaderType.VertexShader, "Shaders/Deferred/Output/vertex.glsl"),
                        ShaderSource.Load(ShaderType.FragmentShader, "Shaders/Deferred/Output/fragment.glsl"));

            _fst = Kokoro.Graphics.Prefabs.FullScreenTriangleFactory.Create(Engine.iMeshGroup);

            //Color: 16DiffR:16DiffG:16DiffB:16Roughness
            //Normal: 16NX:16NY:16DerivX:16DerivY
            //Specular: 16SpecR:16SpecG:16SpecB
            //Depth: 32D

            Framebuffers = new Framebuffer[_targetCount];
            _accumulators = new Framebuffer[_targetCount];
            _pointL_state = new RenderState[_targetCount];
            _spotL_state = new RenderState[_targetCount];
            _direcL_state = new RenderState[_targetCount];
            _state = new RenderState[_targetCount];
            _queue = new RenderQueue[_targetCount];
            _colorMaps = new Texture[_targetCount];
            _normalMaps = new Texture[_targetCount];
            _specMaps = new Texture[_targetCount];
            _depthMaps = new Texture[_targetCount];
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
                    InternalFormat = PixelInternalFormat.Rgba16f,
                    PixelType = PixelType.HalfFloat
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
                var accumulator = new Texture();
                accumulator.SetData(accumulatorSrc, 0);

                _accumulators[i] = new Framebuffer(dest[i].Width, dest[i].Height);
                _accumulators[i][FramebufferAttachment.ColorAttachment0] = accumulator;

                _pointL_state[i] = new RenderState(_accumulators[i], _pointLightShader, new ShaderStorageBuffer[] { Lights.pointLights_buffer }, null, false, true, DepthFunc.Always, InverseDepth.Far, InverseDepth.Near, BlendFactor.One, BlendFactor.One, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.Back);
                _spotL_state[i] = new RenderState(_accumulators[i], _spotLightShader, new ShaderStorageBuffer[] { Lights.spotLights_buffer }, null, false, true, DepthFunc.Always, InverseDepth.Far, InverseDepth.Near, BlendFactor.One, BlendFactor.One, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.Back);
                _direcL_state[i] = new RenderState(_accumulators[i], _directionalLightShader, new ShaderStorageBuffer[] { Lights.direcLights_buffer }, null, false, true, DepthFunc.Always, InverseDepth.Far, InverseDepth.Near, BlendFactor.One, BlendFactor.One, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.Back);
                _state[i] = new RenderState(_destFramebuffers[i], _outputShader, null, null, false, true, DepthFunc.Always, InverseDepth.Far, InverseDepth.Near, BlendFactor.One, BlendFactor.Zero, Vector4.Zero, InverseDepth.ClearDepth, CullFaceMode.Back);

                _queue[i] = new RenderQueue(1, true);
                _queue[i].ClearFramebufferBeforeSubmit = true;

                {
                    var colorHandle = _colorMaps[i].GetHandle(TextureSampler.Default);
                    colorHandle.SetResidency(Residency.Resident);
                    _pointLightShader.Set("ColorMap", colorHandle);

                    var normalHandle = _normalMaps[i].GetHandle(TextureSampler.Default);
                    normalHandle.SetResidency(Residency.Resident);
                    _pointLightShader.Set("NormalMap", normalHandle);

                    var specHandle = _specMaps[i].GetHandle(TextureSampler.Default);
                    specHandle.SetResidency(Residency.Resident);
                    _pointLightShader.Set("SpecularMap", specHandle);

                    var depthHandle = _depthMaps[i].GetHandle(TextureSampler.Default);
                    depthHandle.SetResidency(Residency.Resident);
                    _pointLightShader.Set("DepthMap", depthHandle);
                }

                {
                    var colorHandle = _colorMaps[i].GetHandle(TextureSampler.Default);
                    colorHandle.SetResidency(Residency.Resident);
                    _spotLightShader.Set("ColorMap", colorHandle);

                    var normalHandle = _normalMaps[i].GetHandle(TextureSampler.Default);
                    normalHandle.SetResidency(Residency.Resident);
                    _spotLightShader.Set("NormalMap", normalHandle);

                    var specHandle = _specMaps[i].GetHandle(TextureSampler.Default);
                    specHandle.SetResidency(Residency.Resident);
                    _spotLightShader.Set("SpecularMap", specHandle);

                    var depthHandle = _depthMaps[i].GetHandle(TextureSampler.Default);
                    depthHandle.SetResidency(Residency.Resident);
                    _spotLightShader.Set("DepthMap", depthHandle);
                }

                {
                    var accumHandle = accumulator.GetHandle(TextureSampler.Default);
                    accumHandle.SetResidency(Residency.Resident);
                    _outputShader.Set("Accumulator", accumHandle);
                }
            }
        }

        public void Clear()
        {
            var fbuf = GraphicsDevice.Framebuffer;
            for (int i = 0; i < _targetCount; i++)
            {
                GraphicsDevice.Framebuffer = Framebuffers[i];
                GraphicsDevice.ClearDepthBuffer();
                GraphicsDevice.Clear();
            }
            GraphicsDevice.Framebuffer = fbuf;
        }

        public void Submit(Matrix4[] View, Matrix4[] Proj, Vector3 pos)
        {
            //Apply lighting and other screen space effects and write to the output framebuffers
            for (int i = 0; i < _targetCount; i++)
            {
                Matrix4 v = View[i];
                Matrix4 p = Proj[i];

                _pointLightShader.Set("View", v);
                _pointLightShader.Set("Proj", p);
                _pointLightShader.Set("EyePos", pos);

                _spotLightShader.Set("View", v);
                _spotLightShader.Set("Proj", p);
                _spotLightShader.Set("EyePos", pos);

                _directionalLightShader.Set("View", v);
                _directionalLightShader.Set("Proj", p);
                _directionalLightShader.Set("EyePos", pos);

                _queue[i].ClearAndBeginRecording();
                _queue[i].RecordDraw(new RenderQueue.DrawData()
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
                _queue[i].RecordDraw(new RenderQueue.DrawData()
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
                _queue[i].RecordDraw(new RenderQueue.DrawData()
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
                _queue[i].Submit();
            }
        }

    }
}
