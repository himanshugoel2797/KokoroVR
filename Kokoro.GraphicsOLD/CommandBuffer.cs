using Kokoro.Graphics.Profiling;
using Kokoro.Math;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public class CommandBuffer : IDisposable
    {
        enum CommandType
        {
            TexUpload,
            BufUpload,
            Clear,
            Invalidate,
            SetState,
            Draw,
            DrawIndexed,
            MultiDrawIndirect,
            MultiDrawIndirectIndexed,
            SwapBuffer,
            Dispatch,
            DispatchIndirect,
            Barrier,
        }

        struct CommandEntry
        {
            public CommandType Type { get; set; }
            public Fence SyncFence { get; set; }

            //Clear
            public bool ClearColorBuffer { get; set; }
            public bool ClearDepthBuffer { get; set; }

            //Texture Upload
            public GpuBuffer Buffer { get; set; }
            public Texture Texture { get; set; }
            public int WriteLevel { get; set; }
            public Vector3 Offset { get; set; }
            public Vector3 Size { get; set; }
            public PixelFormat Format { get; set; }
            public PixelType PixType { get; set; }

            //Buffer Upload
            public GpuBuffer Destination { get; set; }

            //Render State
            public RenderState State { get; set; }

            //Draw
            public PrimitiveType PrimitiveType { get; set; }
            public int First { get; set; }
            public int Count { get; set; }
            public int InstanceCount { get; set; }
            public int BaseInstance { get; set; }

            //DrawIndexed
            public bool IndicesAreShort { get; set; }
            public long IndexOffset { get; set; }

            //Indirect
            public long IndirectOffset { get; set; }
            public int IndirectStride { get; set; }
            public long DrawCountOffset { get; set; }
            public int MaxDrawCount { get; set; }

            //Barrier
            public BarrierType BarrierType { get; set; }
        }

        private readonly List<CommandEntry> Commands;

        public CommandBuffer()
        {
            Commands = new List<CommandEntry>();
        }

        //texture upload
        public void UploadTexture(GpuBuffer buffer, Texture tex, int write_lv, Vector3 offset, Vector3 size, PixelFormat fmt, PixelType type, Fence f)
        {
            Commands.Add(new CommandEntry()
            {
                Type = CommandType.TexUpload,
                SyncFence = f,
                Buffer = buffer,
                Texture = tex,
                WriteLevel = write_lv,
                Offset = offset,
                Size = size,
                Format = fmt,
                PixType = type
            });
        }

        //clear
        public void Clear(bool color, bool depth)
        {
            Commands.Add(new CommandEntry()
            {
                Type = CommandType.Clear,
                ClearColorBuffer = color,
                ClearDepthBuffer = depth
            });
        }

        public void Invalidate()
        {
            Commands.Add(new CommandEntry()
            {
                Type = CommandType.Invalidate
            });
        }

        //set render state
        public void SetRenderState(RenderState state)
        {
            Commands.Add(new CommandEntry()
            {
                Type = CommandType.SetState,
                State = state
            });
        }

        //draw
        public void Draw(PrimitiveType primType, int first, int cnt, int instCnt, int baseInst)
        {
            Commands.Add(new CommandEntry()
            {
                Type = CommandType.Draw,
                PrimitiveType = primType,
                First = first,
                Count = cnt,
                InstanceCount = instCnt,
                BaseInstance = baseInst,
            });
        }

        public void DrawIndexed(PrimitiveType primType, int first, int cnt, bool indexShort, long indexOff, int instCnt, int baseInst)
        {
            Commands.Add(new CommandEntry()
            {
                Type = CommandType.DrawIndexed,
                PrimitiveType = primType,
                First = first,
                Count = cnt,
                IndicesAreShort = indexShort,
                IndexOffset = indexOff,
                InstanceCount = instCnt,
                BaseInstance = baseInst,
            });
        }

        public void MultiDrawIndirect(PrimitiveType primType, GpuBuffer draws, long indirOffset, long drawCntOffset, int maxDrawCnt, int indirStride)
        {
            Commands.Add(new CommandEntry()
            {
                Type = CommandType.MultiDrawIndirect,
                Buffer = draws,
                PrimitiveType = primType,
                IndirectOffset = indirOffset,
                DrawCountOffset = drawCntOffset,
                MaxDrawCount = maxDrawCnt,
                IndirectStride = indirStride,
            });
        }

        public void MultiDrawIndirectIndexed(PrimitiveType primType, GpuBuffer draws, bool indexShort, long indirOffset, long drawCntOffset, int maxDrawCnt, int indirStride)
        {
            Commands.Add(new CommandEntry()
            {
                Type = CommandType.MultiDrawIndirectIndexed,
                Buffer = draws,
                PrimitiveType = primType,
                IndicesAreShort = indexShort,
                IndirectOffset = indirOffset,
                DrawCountOffset = drawCntOffset,
                MaxDrawCount = maxDrawCnt,
                IndirectStride = indirStride,
            });
        }

        //swapbuffer
        public void SwapBuffers()
        {
            Commands.Add(new CommandEntry()
            {
                Type = CommandType.SwapBuffer
            });
        }

        //dispatch compute
        public void Dispatch(int x, int y, int z)
        {
            Commands.Add(new CommandEntry()
            {
                Type = CommandType.Dispatch,
                Size = new Vector3(x, y, z)
            });
        }

        public void DispatchIndirect(GpuBuffer buffer, long offset)
        {
            Commands.Add(new CommandEntry()
            {
                Type = CommandType.DispatchIndirect,
                Buffer = buffer,
                IndirectOffset = offset
            });
        }

        //reset
        public void Reset()
        {
            Commands.Clear();
        }

        //Barrier
        public void Barrier(BarrierType bType)
        {
            Commands.Add(new CommandEntry()
            {
                Type = CommandType.Barrier,
                BarrierType = bType,
            });
        }

        public void Submit()
        {
            //TODO: make this execution task be submitted to the main thread, so construction can happen on a separate thread
            foreach (CommandEntry c in Commands)
                switch (c.Type)
                {
                    case CommandType.Clear:
                        {
                            GL.Clear((c.ClearColorBuffer ? ClearBufferMask.ColorBufferBit : 0) | (c.ClearDepthBuffer ? ClearBufferMask.DepthBufferBit : 0));
                        }
                        break;
                    case CommandType.Invalidate:
                        {
                            GraphicsDevice.Framebuffer.Invalidate();
                        }
                        break;
                    case CommandType.TexUpload:
                        {
                            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, (int)c.Buffer);
                            switch (c.Texture.Target)
                            {
                                case TextureTarget.Texture1D:
                                    GL.TextureSubImage1D((int)c.Texture, c.WriteLevel, (int)c.Offset.X, (int)c.Size.X, (OpenTK.Graphics.OpenGL4.PixelFormat)c.Format, (OpenTK.Graphics.OpenGL4.PixelType)c.PixType, IntPtr.Zero);
                                    break;
                                case TextureTarget.Texture1DArray:
                                case TextureTarget.Texture2D:
                                case TextureTarget.TextureCubeMap:
                                    GL.TextureSubImage2D((int)c.Texture, c.WriteLevel, (int)c.Offset.X, (int)c.Offset.Y, (int)c.Size.X, (int)c.Size.Y, (OpenTK.Graphics.OpenGL4.PixelFormat)c.Format, (OpenTK.Graphics.OpenGL4.PixelType)c.PixType, IntPtr.Zero);
                                    break;
                                case TextureTarget.Texture2DArray:
                                case TextureTarget.Texture3D:
                                case TextureTarget.TextureCubeMapArray:
                                    GL.TextureSubImage3D((int)c.Texture, c.WriteLevel, (int)c.Offset.X, (int)c.Offset.Y, (int)c.Offset.Z, (int)c.Size.X, (int)c.Size.Y, (int)c.Size.Z, (OpenTK.Graphics.OpenGL4.PixelFormat)c.Format, (OpenTK.Graphics.OpenGL4.PixelType)c.PixType, IntPtr.Zero);
                                    break;
                            }
                            c.SyncFence?.PlaceFence();
                        }
                        break;
                    case CommandType.SwapBuffer:
                        {
                            GraphicsDevice.SwapBuffers();
                        }
                        break;
                    case CommandType.SetState:
                        {
                            GraphicsDevice.SetRenderState(c.State);
                        }
                        break;
                    case CommandType.Draw:
                        {
#if DEBUG
                            GenericMetrics.StartMeasurement();
                            PerfAPI.BeginDraw();
#endif
                            GL.DrawArraysInstancedBaseInstance((OpenTK.Graphics.OpenGL4.PrimitiveType)c.PrimitiveType, c.First, c.Count, c.InstanceCount, c.BaseInstance);
#if DEBUG
                            PerfAPI.EndSample();
                            GenericMetrics.StopMeasurement();
#endif
                        }
                        break;
                    case CommandType.DrawIndexed:
                        {
#if DEBUG
                            GenericMetrics.StartMeasurement();
                            PerfAPI.BeginDrawIndexed();
#endif
                            GL.DrawElementsInstancedBaseVertexBaseInstance((OpenTK.Graphics.OpenGL4.PrimitiveType)c.PrimitiveType, c.Count, c.IndicesAreShort ? DrawElementsType.UnsignedShort : DrawElementsType.UnsignedInt, (IntPtr)c.IndexOffset, c.InstanceCount, c.First, c.BaseInstance);
#if DEBUG
                            PerfAPI.EndSample();
                            GenericMetrics.StopMeasurement();
#endif
                        }
                        break;
                    case CommandType.MultiDrawIndirect:
                        {
#if DEBUG
                            GenericMetrics.StartMeasurement();
                            PerfAPI.BeginMultiDrawIndirectCount();
#endif
                            GL.BindBuffer(BufferTarget.DrawIndirectBuffer, (int)c.Buffer);
                            GL.BindBuffer((BufferTarget)ArbIndirectParameters.ParameterBufferArb, (int)c.Buffer);
                            GL.MultiDrawArraysIndirectCount((OpenTK.Graphics.OpenGL4.PrimitiveType)c.PrimitiveType, (IntPtr)c.IndirectOffset, (IntPtr)c.DrawCountOffset, c.MaxDrawCount, c.IndirectStride);
#if DEBUG
                            PerfAPI.EndSample();
                            GenericMetrics.StopMeasurement();
#endif
                        }
                        break;
                    case CommandType.MultiDrawIndirectIndexed:
                        {
#if DEBUG
                            GenericMetrics.StartMeasurement();
                            PerfAPI.BeginMultiDrawIndirectIndexedCount();
#endif
                            GL.BindBuffer(BufferTarget.DrawIndirectBuffer, (int)c.Buffer);
                            GL.BindBuffer((BufferTarget)ArbIndirectParameters.ParameterBufferArb, (int)c.Buffer);
                            GL.MultiDrawElementsIndirectCount((OpenTK.Graphics.OpenGL4.PrimitiveType)c.PrimitiveType, c.IndicesAreShort ? All.UnsignedShort : All.UnsignedInt, (IntPtr)c.IndirectOffset, (IntPtr)c.DrawCountOffset, c.MaxDrawCount, c.IndirectStride);
#if DEBUG
                            PerfAPI.EndSample();
                            GenericMetrics.StopMeasurement();
#endif
                        }
                        break;
                    case CommandType.Dispatch:
                        {
#if DEBUG
                            PerfAPI.BeginCompute();
#endif
                            GL.DispatchCompute((int)c.Size.X, (int)c.Size.Y, (int)c.Size.Z);
#if DEBUG
                            PerfAPI.EndSample();
#endif
                        }
                        break;
                    case CommandType.DispatchIndirect:
                        {
#if DEBUG
                            PerfAPI.BeginComputeIndirect();
#endif
                            GL.BindBuffer(BufferTarget.DispatchIndirectBuffer, (int)c.Buffer);
                            GL.DispatchComputeIndirect((IntPtr)c.IndirectOffset);
#if DEBUG
                            PerfAPI.EndSample();
#endif
                        }
                        break;
                    case CommandType.Barrier:
                        {
                            MemoryBarrierFlags flags = 0;
                            flags |= (c.BarrierType & BarrierType.ElementArray) != 0 ? MemoryBarrierFlags.ElementArrayBarrierBit : 0;
                            flags |= (c.BarrierType & BarrierType.UniformBuffer) != 0 ? MemoryBarrierFlags.UniformBarrierBit : 0;
                            flags |= (c.BarrierType & BarrierType.TextureFetch) != 0 ? MemoryBarrierFlags.TextureFetchBarrierBit : 0;
                            flags |= (c.BarrierType & BarrierType.ShaderImageAccess) != 0 ? MemoryBarrierFlags.ShaderImageAccessBarrierBit : 0;
                            flags |= (c.BarrierType & BarrierType.Command) != 0 ? MemoryBarrierFlags.CommandBarrierBit : 0;
                            flags |= (c.BarrierType & BarrierType.PixelBuffer) != 0 ? MemoryBarrierFlags.PixelBufferBarrierBit : 0;
                            flags |= (c.BarrierType & BarrierType.TextureUpdate) != 0 ? MemoryBarrierFlags.TextureUpdateBarrierBit : 0;
                            flags |= (c.BarrierType & BarrierType.BufferUpdate) != 0 ? MemoryBarrierFlags.BufferUpdateBarrierBit : 0;
                            flags |= (c.BarrierType & BarrierType.Framebuffer) != 0 ? MemoryBarrierFlags.FramebufferBarrierBit : 0;
                            flags |= (c.BarrierType & BarrierType.StorageBuffer) != 0 ? MemoryBarrierFlags.ShaderStorageBarrierBit : 0;

                            GL.MemoryBarrier(flags);
                        }
                        break;
                }
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

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~CommandBuffer()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
