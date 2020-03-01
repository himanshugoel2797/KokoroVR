using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public enum PrimitiveType
    {
        Triangle = VkPrimitiveTopology.PrimitiveTopologyTriangleList,
        TriangleStrip = VkPrimitiveTopology.PrimitiveTopologyTriangleStrip,
        Line = VkPrimitiveTopology.PrimitiveTopologyLineList,
        LineStrip = VkPrimitiveTopology.PrimitiveTopologyLineStrip,
        Point = VkPrimitiveTopology.PrimitiveTopologyPointList,
    }

    public enum CullMode
    {
        None = VkCullModeFlags.CullModeNone,
        Front = VkCullModeFlags.CullModeFrontBit,
        Back = VkCullModeFlags.CullModeBackBit
    }

    public enum DepthTest
    {
        Greater = VkCompareOp.CompareOpGreater,
        GreaterEqual = VkCompareOp.CompareOpGreaterOrEqual,
        Less = VkCompareOp.CompareOpLess,
        LessEqual = VkCompareOp.CompareOpLessOrEqual,
        Equal = VkCompareOp.CompareOpEqual,
        NotEqual = VkCompareOp.CompareOpNotEqual,
        Always = VkCompareOp.CompareOpAlways
    }

    public enum PipelineStage
    {
        Top = VkPipelineStageFlags.PipelineStageTopOfPipeBit,
        DrawIndirect = VkPipelineStageFlags.PipelineStageDrawIndirectBit,
        VertInput = VkPipelineStageFlags.PipelineStageVertexInputBit,
        VertShader = VkPipelineStageFlags.PipelineStageVertexShaderBit,
        TessCtrlShader = VkPipelineStageFlags.PipelineStageTessellationControlShaderBit,
        TessEvalShader = VkPipelineStageFlags.PipelineStageTessellationEvaluationShaderBit,
        GeomShader = VkPipelineStageFlags.PipelineStageGeometryShaderBit,
        FragShader = VkPipelineStageFlags.PipelineStageFragmentShaderBit,
        EarlyFragTests = VkPipelineStageFlags.PipelineStageEarlyFragmentTestsBit,
        LateFragTests = VkPipelineStageFlags.PipelineStageLateFragmentTestsBit,
        ColorAttachOut = VkPipelineStageFlags.PipelineStageColorAttachmentOutputBit,
        Transfer = VkPipelineStageFlags.PipelineStageTransferBit,
        CompShader = VkPipelineStageFlags.PipelineStageComputeShaderBit,
        Bottom = VkPipelineStageFlags.PipelineStageBottomOfPipeBit,
    }

    public class PipelineLayout : IDisposable
    {
        public string Name { get; set; }
        public DescriptorSet[] Descriptors { get; set; }

        internal IntPtr hndl;
        private int devID;
        private bool locked;

        public PipelineLayout()
        {
        }

        public void Rebuild()
        {
            if (locked)
            {
                Dispose(false);
                locked = false;
                Build(devID);
            }
            else
                throw new Exception("PipelineLayout has not been built.");
        }

        public void Build(int deviceIndex)
        {
            if (!locked)
            {
                unsafe
                {
                    devID = deviceIndex;

                    var descLayouts = stackalloc IntPtr[Descriptors == null ? 0 : Descriptors.Length];
                    if (Descriptors != null) 
                        for (int i = 0; i < Descriptors.Length; i++) descLayouts[i] = Descriptors[i].Pool.layout_hndl;
                    var pipelineLayoutInfo = new VkPipelineLayoutCreateInfo()
                    {
                        sType = VkStructureType.StructureTypePipelineLayoutCreateInfo,
                        setLayoutCount = Descriptors == null ? 0 : (uint)Descriptors.Length, //TODO: add descriptor support
                        pSetLayouts = descLayouts,
                        pushConstantRangeCount = 0, //TODO: setup push constants
                        pPushConstantRanges = IntPtr.Zero,
                    };

                    IntPtr pipelineLayout_l = IntPtr.Zero;
                    if (vkCreatePipelineLayout(GraphicsDevice.GetDeviceInfo(deviceIndex).Device, pipelineLayoutInfo.Pointer(), null, &pipelineLayout_l) != VkResult.Success)
                        throw new Exception("Failed to create pipeline layout.");
                    hndl = pipelineLayout_l;

                    if (GraphicsDevice.EnableValidation)
                    {
                        var objName = new VkDebugUtilsObjectNameInfoEXT()
                        {
                            sType = VkStructureType.StructureTypeDebugUtilsObjectNameInfoExt,
                            pObjectName = Name,
                            objectType = VkObjectType.ObjectTypePipelineLayout,
                            objectHandle = (ulong)hndl
                        };
                        GraphicsDevice.SetDebugUtilsObjectNameEXT(GraphicsDevice.GetDeviceInfo(deviceIndex).Device, objName.Pointer());
                    }
                }
                locked = true;
            }
            else
                throw new Exception("PipelineLayout is locked.");
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                if (locked)
                {
                    var device = GraphicsDevice.GetDeviceInfo(devID).Device;
                    vkDestroyPipelineLayout(device, hndl, null);
                }

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~PipelineLayout()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
