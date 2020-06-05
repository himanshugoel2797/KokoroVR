using Kokoro.Common;
using RadeonRaysSharp.Raw;
using System;
using System.Collections.Generic;
using System.Text;
using static RadeonRaysSharp.Raw.RadeonRays;

namespace Kokoro.Graphics
{
    public class RayGeometry : UniquelyNamedObject
    {
        private int devID;

        internal RRGeometryBuildInput GeometryBuildInput;
        internal ManagedPtr<RRGeometryBuildInput> GeometryBuildInput_ptr;
        internal RRTriangleMeshPrimitive TrianglePrimitive;
        internal ManagedPtr<RRTriangleMeshPrimitive> TrianglePrimitive_ptr;
        internal IntPtr scratchBufferPtr;
        internal IntPtr geomBufferPtr;

        public const uint VertexSize = 3 * sizeof(float);
        public uint VertexCount { get; private set; }
        public GpuBuffer ScratchBuffer { get; private set; }
        public GpuBuffer BuiltGeometryBuffer { get; private set; }

        public RayGeometry(string name, uint vertexCount) : base(name)
        {
            VertexCount = vertexCount;
        }

        public void SetupBuild(int deviceIndex, uint triangleCnt, GpuBuffer vertices, ulong vertices_off, GpuBuffer indices, ulong indices_off, bool shortIndices)
        {
            devID = deviceIndex;
            TrianglePrimitive = new RRTriangleMeshPrimitive()
            {
                index_type = shortIndices ? RRIndexType.RrIndexTypeUint16 : RRIndexType.RrIndexTypeUint32,
                triangle_count = triangleCnt,
                triangle_indices = indices.GetRayDevicePointer(indices_off),
                vertex_count = VertexCount,
                vertex_stride = VertexSize,
                vertices = vertices.GetRayDevicePointer(vertices_off)
            };
            TrianglePrimitive_ptr = TrianglePrimitive.Pointer();

            GeometryBuildInput = new RRGeometryBuildInput()
            {
                primitive_type = RRPrimitiveType.RrPrimitiveTypeTriangleMesh,
                primitive_count = 1,
                primitives = TrianglePrimitive_ptr,
            };
            GeometryBuildInput_ptr = GeometryBuildInput.Pointer();

            RRBuildOptions opts = new RRBuildOptions()
            {
                build_flags = 0,
                backend_specific_info = IntPtr.Zero,
            };
            var opts_ptr = opts.Pointer();

            //Allocate the necessary memory
            ManagedPtr<RRMemoryRequirements> geomMemReqs = new ManagedPtr<RRMemoryRequirements>();
            if (rrGetGeometryBuildMemoryRequirements(GraphicsDevice.DeviceInformation[deviceIndex].RaysContext, GeometryBuildInput_ptr, opts_ptr, geomMemReqs) != RRError.RrSuccess)
                throw new Exception("Failed to determine geometry memory requirements.");

            ScratchBuffer = new GpuBuffer(Name + "_scratch")
            {
                MemoryUsage = MemoryUsage.GpuOnly,
                Size = geomMemReqs.Value.temporary_build_buffer_size,
                Usage = BufferUsage.Storage | BufferUsage.TransferDst
            };
            ScratchBuffer.Build(deviceIndex);
            scratchBufferPtr = ScratchBuffer.GetRayDevicePointer(0);

            BuiltGeometryBuffer = new GpuBuffer(Name + "_geom")
            {
                MemoryUsage = MemoryUsage.GpuOnly,
                Size = geomMemReqs.Value.result_buffer_size,
                Usage = BufferUsage.Storage | BufferUsage.TransferDst
            };
            BuiltGeometryBuffer.Build(deviceIndex);
            geomBufferPtr = BuiltGeometryBuffer.GetRayDevicePointer(0);
        }
    }

    public class RayIntersections : UniquelyNamedObject
    {
        internal IntPtr rayBufferPtr;
        internal IntPtr resultBufferPtr;
        internal IntPtr scratchBufferPtr;
        internal IntPtr indirectRayCountPtr;

        public const uint RaySize = 8 * sizeof(float);
        public const uint HitSize = 4 * sizeof(float);

        public GpuBuffer ScratchBuffer { get; private set; }
        public StreamableBuffer HitBuffer { get; private set; }
        public StreamableBuffer RayBuffer { get; private set; }
        public uint MaxRayCount { get; private set; }
        public RayIntersections(string name, uint maxCount, int deviceIndex) : base(name)
        {
            MaxRayCount = maxCount;

            RayBuffer = new StreamableBuffer(name + "_rays", maxCount * RaySize, BufferUsage.Storage);
            rayBufferPtr = RayBuffer.LocalBuffer.GetRayDevicePointer(0);

            HitBuffer = new StreamableBuffer(name + "_hits", maxCount * HitSize, BufferUsage.Storage);
            resultBufferPtr = HitBuffer.LocalBuffer.GetRayDevicePointer(0);

            //Allocate the necessary memory
            unsafe
            {
                ulong scratchSz = 0;
                if (rrGetTraceMemoryRequirements(GraphicsDevice.DeviceInformation[deviceIndex].RaysContext, maxCount, &scratchSz) != RRError.RrSuccess)
                    throw new Exception("Failed to determine trace memory requirements.");

                ScratchBuffer = new GpuBuffer(name + "_scratch")
                {
                    MemoryUsage = MemoryUsage.GpuOnly,
                    Size = scratchSz,
                    Usage = BufferUsage.Storage | BufferUsage.TransferDst
                };
                ScratchBuffer.Build(deviceIndex);
                scratchBufferPtr = ScratchBuffer.GetRayDevicePointer(0);
            }
        }

        public void Update()
        {
            HitBuffer.Update();
            RayBuffer.Update();
        }

        public void RebuildGraph()
        {
            HitBuffer.RebuildGraph();
            RayBuffer.RebuildGraph();
        }
    }
}
