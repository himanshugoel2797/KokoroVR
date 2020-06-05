using Kokoro.Graphics;
using Kokoro.Graphics.Framegraph;
//using KokoroVR2.Graphics.Voxel;
using System.IO;
using ObjLoader.Loader.Loaders;

namespace KokoroVR2.RaytracingProto
{
    class Program
    {
        static SpecializedShader vertS, fragS;
        static Image[] depthImages;
        static ImageView[] depthImageViews;

        static bool buildGeometry = true;
        static RayGeometry geometry;
        static RayIntersections rayIntersections;
        static StreamableBuffer vertexBuffer;
        static StreamableBuffer indexBuffer;

        static void Main(string[] args)
        {
            Engine.AppName = "Test";
            Engine.EnableValidation = true;
            Engine.RebuildShaders = true;
            Engine.Initialize();

            var graph = new FrameGraph(0);
            Engine.RenderGraph = graph;

            int trace_side = 1024;
            var objLoaderFactory = new ObjLoaderFactory();
            var objLoader = objLoaderFactory.Create();
            using (var fs = File.OpenRead("sponza.obj"))
            {
                var obj_loaded = objLoader.Load(fs);

                ulong idxCount = 0;
                for (int i = 0; i < obj_loaded.Groups.Count; i++)
                    idxCount += (ulong)obj_loaded.Groups[i].Faces.Count * 3;

                //Build a BVH (on the cpu for now)
                //Upload and trace on the gpu

                vertexBuffer = new StreamableBuffer("vbo", (ulong)obj_loaded.Vertices.Count * 3 * sizeof(float), BufferUsage.Storage);
                indexBuffer = new StreamableBuffer("ibo", idxCount * sizeof(uint), BufferUsage.Index | BufferUsage.Storage);
                rayIntersections = new RayIntersections("rayInter", (uint)trace_side * (uint)trace_side, 0);
                geometry = new RayGeometry("rayGeom", (uint)obj_loaded.Vertices.Count);
                unsafe
                {
                    float* fp = (float*)vertexBuffer.BeginBufferUpdate();
                    for (int i = 0; i < obj_loaded.Vertices.Count; i++)
                    {
                        fp[0] = obj_loaded.Vertices[i].X;
                        fp[1] = obj_loaded.Vertices[i].Y;
                        fp[2] = obj_loaded.Vertices[i].Z;

                        fp += 3;
                    }
                    vertexBuffer.EndBufferUpdate();

                    uint* ui = (uint*)indexBuffer.BeginBufferUpdate();
                    for (int i = 0; i < obj_loaded.Groups.Count; i++)
                        for (int j = 0; j < obj_loaded.Groups[i].Faces.Count; j++)
                        {
                            ui[0] = (uint)(obj_loaded.Groups[i].Faces[j][0].VertexIndex - 1);
                            ui[1] = (uint)(obj_loaded.Groups[i].Faces[j][1].VertexIndex - 1);
                            ui[2] = (uint)(obj_loaded.Groups[i].Faces[j][2].VertexIndex - 1);

                            ui += 3;
                        }
                    indexBuffer.EndBufferUpdate();

                    float* rp = (float*)rayIntersections.RayBuffer.BeginBufferUpdate();
                    for (uint x = 0; x < trace_side; x++)
                        for (uint y = 0; y < trace_side; y++)
                        {
                            //uint i = 0;
                            //for(int j = 0; j < 15; j++)
                            //{
                            //    i |= ((x & (1u << j)) << j) | ((y & (1u << j)) << (j + 1));
                            //}

                            //i *= 8;
                            uint i = ((uint)trace_side * y + x) * 8;
                            rp[i + 0] = 0.0f;
                            rp[i + 1] = 15.0f;
                            rp[i + 2] = 0.0f;

                            rp[i + 4] = -1.0f;
                            rp[i + 5] = -1.0f + (2.0f / trace_side) * y;
                            rp[i + 6] = -1.0f + (2.0f / trace_side) * x;

                            rp[i + 3] = 0.001f;
                            rp[i + 7] = 100000.0f;
                        }
                    rayIntersections.RayBuffer.EndBufferUpdate();

                    geometry.SetupBuild(0, (uint)(idxCount / 3), vertexBuffer.LocalBuffer, 0, indexBuffer.LocalBuffer, 0, false);
                }
            }


            vertS = new SpecializedShader()
            {
                Name = "FST_Vert",
                Shader = ShaderSource.Load(ShaderType.VertexShader, "FullScreenTriangle/vertex.glsl"),
                SpecializationData = null
            };

            fragS = new SpecializedShader()
            {
                Name = "UVR_Frag",
                Shader = ShaderSource.Load(ShaderType.FragmentShader, "FullScreenTriangle/clear_frag.glsl"),
                SpecializationData = null,
            };

            depthImages = new Image[GraphicsDevice.MaxFramesInFlight];
            depthImageViews = new ImageView[GraphicsDevice.MaxFramesInFlight];
            for (int i = 0; i < GraphicsDevice.MaxFramesInFlight; i++)
            {
                depthImages[i] = new Image($"depthImage_{i}")
                {
                    Cubemappable = false,
                    Width = GraphicsDevice.Width,
                    Height = GraphicsDevice.Height,
                    Depth = 1,
                    Dimensions = 2,
                    InitialLayout = ImageLayout.Undefined,
                    Layers = 1,
                    Levels = 1,
                    MemoryUsage = MemoryUsage.GpuOnly,
                    Usage = ImageUsage.DepthAttachment | ImageUsage.Sampled,
                    Format = ImageFormat.Depth32f,
                };
                depthImages[i].Build(0);

                depthImageViews[i] = new ImageView($"depthImageView_{i}")
                {
                    BaseLayer = 0,
                    BaseLevel = 0,
                    Format = ImageFormat.Depth32f,
                    LayerCount = 1,
                    LevelCount = 1,
                    ViewType = ImageViewType.View2D,
                };
                depthImageViews[i].Build(depthImages[i]);
            }

            Engine.OnRebuildGraph += Engine_OnRebuildGraph;
            Engine.OnRender += Engine_OnRender;
            Engine.OnUpdate += Engine_OnUpdate;
            Engine.Start(0);
        }

        private static void Engine_OnUpdate(double time_ms, double delta_ms)
        {
            vertexBuffer.Update();
            indexBuffer.Update();
            rayIntersections.Update();
        }

        private static void Engine_OnRender(double time_ms, double delta_ms)
        {
            //Acquire the frame
            GraphicsDevice.AcquireFrame();

            if (buildGeometry)
            {
                Engine.RenderGraph.QueueOp(new GpuOp()
                {
                    PassName = "ray_build_pass",
                    Resources = new string[]{
                        geometry.ScratchBuffer.Name,
                        geometry.BuiltGeometryBuffer.Name,
                        vertexBuffer.LocalBuffer.Name,
                        indexBuffer.LocalBuffer.Name,
                    },
                    RRGeometry = geometry,
                    RRIntersections = rayIntersections,
                    Cmd = GpuCmd.Build,
                });
                buildGeometry = false;
            }
            else
            {
                buildGeometry = true;
                Engine.RenderGraph.QueueOp(new GpuOp()
                {
                    PassName = "ray_intersect_pass",
                    Resources = new string[]{
                        rayIntersections.ScratchBuffer.Name,
                        geometry.BuiltGeometryBuffer.Name,
                        rayIntersections.RayBuffer.LocalBuffer.Name,
                        rayIntersections.HitBuffer.LocalBuffer.Name,
                    },
                    RRGeometry = geometry,
                    RRIntersections = rayIntersections,
                    Cmd = GpuCmd.Intersect,
                });
            }

            Engine.RenderGraph.QueueOp(new GpuOp()
            {
                ColorAttachments = new string[] { GraphicsDevice.DefaultFramebuffer[GraphicsDevice.CurrentFrameID].ColorAttachments[0].Name },
                DepthAttachment = depthImageViews[GraphicsDevice.CurrentFrameID].Name,
                PassName = "main_pass",
                Resources = new string[]{
                        Engine.GlobalParameters.Name,
                        rayIntersections.HitBuffer.LocalBuffer.Name,
                        //rayIntersections.RayBuffer.LocalBuffer.Name,
                    },
                Cmd = GpuCmd.Draw,
                VertexCount = 3,
            });

            Engine.RenderGraph.Build();

            GraphicsDevice.PresentFrame();
        }

        private static void Engine_OnRebuildGraph()
        {
            var graph = Engine.RenderGraph;

            for (int i = 0; i < GraphicsDevice.MaxFramesInFlight; i++)
            {
                var out_img = GraphicsDevice.DefaultFramebuffer[i].ColorAttachments[0];
                graph.RegisterResource(out_img);
                graph.RegisterResource(depthImageViews[i]);
            }

            graph.RegisterShader(vertS);
            graph.RegisterShader(fragS);

            vertexBuffer.RebuildGraph();
            indexBuffer.RebuildGraph();
            rayIntersections.RebuildGraph();

            var gpass = new GraphicsPass("main_pass")
            {
                Shaders = new string[] { vertS.Name, fragS.Name },
                ViewportWidth = GraphicsDevice.Width,
                ViewportHeight = GraphicsDevice.Height,
                ViewportDynamic = false,
                DepthWriteEnable = true,
                DepthTest = DepthTest.Always,
                CullMode = CullMode.None,
                Fill = FillMode.Fill,
                ViewportMinDepth = 0,
                ViewportMaxDepth = 1,
                RenderLayout = new RenderLayout()
                {
                    Color = new RenderLayoutEntry[]
                    {
                        new RenderLayoutEntry()
                        {
                            DesiredLayout = ImageLayout.ColorAttachmentOptimal,
                            FirstLoadStage = PipelineStage.ColorAttachOut,
                            Format = GraphicsDevice.DefaultFramebuffer[GraphicsDevice.CurrentFrameID].ColorAttachments[0].Format,
                            LastStoreStage = PipelineStage.ColorAttachOut,
                            LoadOp = AttachmentLoadOp.DontCare,
                            StoreOp = AttachmentStoreOp.Store,
                        },
                    },
                    Depth = new RenderLayoutEntry()
                    {
                        DesiredLayout = ImageLayout.DepthAttachmentOptimal,
                        FirstLoadStage = PipelineStage.EarlyFragTests,
                        Format = ImageFormat.Depth32f,
                        LastStoreStage = PipelineStage.LateFragTests,
                        LoadOp = AttachmentLoadOp.DontCare,
                        StoreOp = AttachmentStoreOp.Store,
                    },
                },
                DescriptorSetup = new DescriptorSetup()
                {
                    Descriptors = new DescriptorConfig[]{
                        new DescriptorConfig(){
                            Count = 1,
                            Index = 0,
                            DescriptorType = DescriptorType.UniformBuffer,
                        },
                        new DescriptorConfig()  //Hit buffer
                        {
                            Count = 1,
                            Index = 1,
                            DescriptorType = DescriptorType.StorageBuffer,
                        },
                    },
                    PushConstants = null,
                },
                Resources = new ResourceUsageEntry[]{
                    new BufferUsageEntry(){
                        StartStage = PipelineStage.VertShader,
                        StartAccesses = AccessFlags.ShaderRead,
                        FinalStage = PipelineStage.VertShader,
                        FinalAccesses = AccessFlags.None
                    },
                    new BufferUsageEntry(){
                        StartStage = PipelineStage.FragShader,
                        StartAccesses = AccessFlags.ShaderRead,
                        FinalStage = PipelineStage.FragShader,
                        FinalAccesses = AccessFlags.None
                    }
                }
            };
            graph.RegisterGraphicsPass(gpass);


            var cpass = new RayPass("ray_build_pass")
            {
                IsAsync = true,
                DescriptorSetup = new DescriptorSetup()
                {
                    Descriptors = new DescriptorConfig[]{
                        new DescriptorConfig()  //scratch buffer
                        {
                            Count = 1,
                            Index = 1,
                            DescriptorType = DescriptorType.StorageBuffer,
                        },
                        new DescriptorConfig() //BuiltGeometry
                        {
                            Count = 1,
                            Index = 2,
                            DescriptorType = DescriptorType.StorageBuffer,
                        },
                        new DescriptorConfig() //vertices
                        {
                            Count = 1,
                            Index = 3,
                            DescriptorType = DescriptorType.StorageBuffer,
                        },
                        new DescriptorConfig() //indices
                        {
                            Count = 1,
                            Index = 4,
                            DescriptorType = DescriptorType.StorageBuffer,
                        },
                    },
                    PushConstants = null,
                },
                Resources = new ResourceUsageEntry[]{
                    new BufferUsageEntry(){
                        StartStage = PipelineStage.CompShader,
                        StartAccesses = AccessFlags.None,
                        FinalStage = PipelineStage.CompShader,
                        FinalAccesses = AccessFlags.ShaderWrite,
                    },
                    new BufferUsageEntry(){
                        StartStage = PipelineStage.CompShader,
                        StartAccesses = AccessFlags.None,
                        FinalStage = PipelineStage.CompShader,
                        FinalAccesses = AccessFlags.ShaderWrite,
                    },
                    new BufferUsageEntry(){
                        StartStage = PipelineStage.CompShader,
                        StartAccesses = AccessFlags.ShaderRead,
                        FinalStage = PipelineStage.CompShader,
                        FinalAccesses = AccessFlags.None,
                    },
                    new BufferUsageEntry(){
                        StartStage = PipelineStage.CompShader,
                        StartAccesses = AccessFlags.ShaderRead,
                        FinalStage = PipelineStage.CompShader,
                        FinalAccesses = AccessFlags.None,
                    },
                }
            };
            graph.RegisterComputePass(cpass);


            var cpass_i = new RayPass("ray_intersect_pass")
            {
                IsAsync = true,
                DescriptorSetup = new DescriptorSetup()
                {
                    Descriptors = new DescriptorConfig[]{
                        new DescriptorConfig()  //scratch buffer
                        {
                            Count = 1,
                            Index = 1,
                            DescriptorType = DescriptorType.StorageBuffer,
                        },
                        new DescriptorConfig() //BuiltGeometry
                        {
                            Count = 1,
                            Index = 2,
                            DescriptorType = DescriptorType.StorageBuffer,
                        },
                        new DescriptorConfig() //Ray buffer
                        {
                            Count = 1,
                            Index = 3,
                            DescriptorType = DescriptorType.StorageBuffer,
                        },
                        new DescriptorConfig() //Hit buffer
                        {
                            Count = 1,
                            Index = 4,
                            DescriptorType = DescriptorType.StorageBuffer,
                        },
                    },
                    PushConstants = null,
                },
                Resources = new ResourceUsageEntry[]{
                    new BufferUsageEntry(){
                        StartStage = PipelineStage.CompShader,
                        StartAccesses = AccessFlags.None,
                        FinalStage = PipelineStage.CompShader,
                        FinalAccesses = AccessFlags.ShaderWrite,
                    },
                    new BufferUsageEntry(){
                        StartStage = PipelineStage.CompShader,
                        StartAccesses = AccessFlags.ShaderRead,
                        FinalStage = PipelineStage.CompShader,
                        FinalAccesses = AccessFlags.None,
                    },
                    new BufferUsageEntry(){
                        StartStage = PipelineStage.CompShader,
                        StartAccesses = AccessFlags.ShaderRead,
                        FinalStage = PipelineStage.CompShader,
                        FinalAccesses = AccessFlags.None,
                    },
                    new BufferUsageEntry(){
                        StartStage = PipelineStage.CompShader,
                        StartAccesses = AccessFlags.None,
                        FinalStage = PipelineStage.CompShader,
                        FinalAccesses = AccessFlags.ShaderWrite,
                    },
                }
            };
            graph.RegisterComputePass(cpass_i);

            graph.GatherDescriptors();
        }
    }
}
