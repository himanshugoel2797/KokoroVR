﻿using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public class RenderQueue2
    {
        public struct DrawData
        {
            public MeshData[] Meshes;
            public RenderState State;
        }

        public struct MeshData
        {
            public IMesh2 Mesh;
            public int InstanceCount;
            public int BaseInstance;
        }

        private Dictionary<RenderState, (List<MeshData>, uint)> MeshGroups;

        private ShaderStorageBuffer multiDrawParams;
        private bool isRecording = false;
        private uint maxDrawCount = 0;

        private bool transient;

        public bool ClearFramebufferBeforeSubmit { get; set; } = false;

        public RenderQueue2(uint MaxDrawCount, bool transient)
        {
            this.transient = transient;
            MeshGroups = new Dictionary<RenderState, (List<MeshData>, uint)>();

            if (MaxDrawCount < 4096)
                MaxDrawCount = 4096;

            maxDrawCount = MaxDrawCount;
            multiDrawParams = new ShaderStorageBuffer((MaxDrawCount * 5 + 1) * sizeof(uint), transient);
        }

        public void Clear()
        {
            MeshGroups.Clear();
        }

        public void ClearAndBeginRecording()
        {
            if (isRecording) throw new Exception("Already recording.");

            //Clear the buffer
            MeshGroups.Clear();

            isRecording = true;
        }

        public void BeginRecording()
        {
            if (isRecording) throw new Exception("Already recording.");
            isRecording = true;
        }

        public void RecordDraws(DrawData[] draws)
        {
            for (int i = 0; i < draws.Length; i++)
                RecordDraw(draws[i]);
        }

        public void RecordDraw(DrawData draw)
        {
            //Group the meshes by state changes
            //Mesh groups can be switched on the fly now, so no need to group them, instead submit them to a compute shader for further culling.
            var renderState = draw.State;
            if (!MeshGroups.ContainsKey(renderState)) MeshGroups[renderState] = (new List<MeshData>(), 0);
            MeshGroups[renderState].Item1.AddRange(draw.Meshes);
        }

        public void EndRecording(Frustum f)
        {
            if (!isRecording) throw new Exception("Not Recording.");

            //Also, perform triple buffering to avoid synchronization if the queue has been hinted as being dynamic


            //Take all the recorded draws in the list and push them into a multidrawindirect buffer for fast draw dispatch
            //Iterate through the list of buckets and build the list of draws to submit
            unsafe
            {
                byte* data = multiDrawParams.Update();

                //Start computing and writing all the data
                uint* data_ui = (uint*)data;

                var bkts = MeshGroups.Keys.ToArray();
                for (int i = 0; i < bkts.Length; i++)
                {
                    var bkt = bkts[i];
                    float* data_ui_fp = (float*)data_ui;

                    MeshGroups[bkt] = (MeshGroups[bkt].Item1, (uint)((ulong)data_ui - (ulong)data));
                    
                    //Index 0 contains the draw count, so all the draw commands themselves are at an offset of 1
                    int idx = 0;
                    for (int j = 0; j < MeshGroups[bkt].Item1.Count; j++)
                    {
                        var mesh = MeshGroups[bkt].Item1[j];

                        if (mesh.Mesh == null)
                            continue;

                        //break into and submit blocks
                        long cnt = mesh.Mesh.Length;
                        for (int k = 0; k < mesh.Mesh.AllocIndices.Length; k++)
                        {
                            if (bkt.IndexBuffer == null)
                            {
                                data_ui[(idx * 4) + 1] = (uint)(System.Math.Min(cnt, mesh.Mesh.BlockSize) / (bkt.IndexBuffer.IsShort ? 2 : 4));   //count
                                data_ui[(idx * 4) + 2] = (uint)mesh.InstanceCount;   //instanceCount
                                data_ui[(idx * 4) + 3] = (uint)(mesh.Mesh.AllocIndices[k] * mesh.Mesh.BlockSize);   //baseVertex
                                data_ui[(idx * 4) + 4] = (uint)mesh.BaseInstance;   //baseInstance
                            }
                            else
                            {
                                data_ui[(idx * 5) + 1] = (uint)(System.Math.Min(cnt, mesh.Mesh.BlockSize) / (bkt.IndexBuffer.IsShort ? 2 : 4));   //count
                                data_ui[(idx * 5) + 2] = (uint)mesh.InstanceCount;   //instanceCount
                                data_ui[(idx * 5) + 3] = (uint)((mesh.Mesh.AllocIndices[k] * mesh.Mesh.BlockSize) / (bkt.IndexBuffer.IsShort ? 2 : 4));   //firstIndex
                                data_ui[(idx * 5) + 4] = (uint)mesh.Mesh.AllocIndices[k];   //baseVertex
                                data_ui[(idx * 5) + 5] = (uint)mesh.BaseInstance;   //baseInstance
                            }

                            if (mesh.Mesh.IsVisible(f, k))
                                idx++;

                            cnt -= mesh.Mesh.BlockSize;
                        }
                    }
                    data_ui[0] = (uint)idx;

                    Console.WriteLine(idx);
                    //Move the data pointer forward
                    data_ui += (1 + 8 * MeshGroups[bkt].Item1.Count);
                }
            }

            //Push the updates
            multiDrawParams.UpdateDone();

            isRecording = false;
        }

        public void Submit()
        {
            if (isRecording)
                throw new Exception("Stop recording before submitting!");

            if (!transient)
                while (!multiDrawParams.IsReady) ;    //Wait for the multidraw buffer to finish updating  

            //Submit the multidraw calls
            var bkts = MeshGroups.Keys.ToArray();
            for (int i = 0; i < bkts.Length; i++)
            {
                GraphicsDevice.SetRenderState(bkts[i]);
                //Bind the current 
                if (ClearFramebufferBeforeSubmit)
                    GraphicsDevice.Clear();

                var (meshes, offset) = MeshGroups[bkts[i]];

                GraphicsDevice.SetMultiDrawParameterBuffer(multiDrawParams);
                GraphicsDevice.SetParameterBuffer(multiDrawParams);

                if (bkts[i].IndexBuffer != null)
                    GraphicsDevice.MultiDrawIndirectCount(PrimitiveType.Triangles, multiDrawParams.Offset + offset + sizeof(uint), multiDrawParams.Offset + offset, maxDrawCount, true, bkts[i].IndexBuffer.IsShort, 5 * sizeof(uint));
                else
                    GraphicsDevice.MultiDrawIndirectCount(PrimitiveType.Triangles, multiDrawParams.Offset + offset + sizeof(uint), multiDrawParams.Offset + offset, maxDrawCount, false, false, 5 * sizeof(uint));

                //Ensure the buffers aren't in use before next update
                RenderState state = bkts[i];
                for (int k = 0; state.ShaderStorageBufferBindings != null && k < state.ShaderStorageBufferBindings.Length; k++)
                {
                    state.ShaderStorageBufferBindings[k].UpdateDone();
                }
                for (int k = 0; state.UniformBufferBindings != null && k < state.UniformBufferBindings.Length; k++)
                {
                    state.UniformBufferBindings[k].UpdateDone();
                }
            }
        }

    }
}
