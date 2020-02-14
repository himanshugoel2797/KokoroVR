using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public enum IndexType
    {
        None,
        UShort,
        UInt,
    }

    public class RenderQueue
    {
        public const int Stride = 16 * sizeof(uint);
        public const int DrawCountOffset = 0;
        public const uint InfoOffset = 4 * sizeof(uint);

        private readonly List<MeshData> MeshGroups;
        private readonly bool transient;
        private readonly IndexType idxType;

        public StorageBuffer MultidrawParams { get; }
        public int MaxDrawCount { get; }

        public RenderQueue(int MaxDrawCount, IndexType t, bool transient)
        {
            this.transient = transient;
            this.idxType = t;
            MeshGroups = new List<MeshData>();

            if (MaxDrawCount < 4096)
                MaxDrawCount = 4096;

            this.MaxDrawCount = MaxDrawCount;
            MultidrawParams = new StorageBuffer(MaxDrawCount * Stride + InfoOffset, transient);
        }

        public void Clear()
        {
            MeshGroups.Clear();
        }

        public void RecordDraws(DrawData[] draws)
        {
            for (int i = 0; i < draws.Length; i++)
                RecordDraw(draws[i]);
        }

        public void RecordDraw(DrawData draw)
        {
            MeshGroups.AddRange(draw.Meshes);
        }

        public void Build(Frustum f, Vector3 eye)
        {
            unsafe
            {
                byte* data = MultidrawParams.Update();

                //Start computing and writing all the data
                uint* data_ui = (uint*)data;
                float* data_ui_fp = (float*)data_ui;

                //Index 0 contains the draw count, so all the draw commands themselves are at an offset of 1
                int idx = 0;
                for (int j = 0; j < MeshGroups.Count; j++)
                {
                    var mesh = MeshGroups[j];

                    if (mesh.Mesh == null)
                        continue;

                    var sorted_draws = mesh.Mesh.Sort(f, eye);

                    //break into and submit blocks
                    for (int q = 0; q < sorted_draws.Length; q++)
                    {
                        (int k, uint cnt, Vector3 min, Vector3 max) = sorted_draws[q];
                        if (idxType == IndexType.None)
                        {
                            data_ui[(idx * 16) + 4] = (uint)cnt;   //count
                            data_ui[(idx * 16) + 5] = (uint)mesh.InstanceCount;   //instanceCount
                            data_ui[(idx * 16) + 6] = (uint)(mesh.Mesh.AllocIndices[k] * mesh.Mesh.BlockSize);   //baseVertex
                            data_ui[(idx * 16) + 7] = (uint)mesh.BaseInstance;   //baseInstance
                            throw new NotImplementedException();
                        }
                        else
                        {
                            data_ui[(idx * 16) + 4] = (uint)(cnt / (idxType == IndexType.UShort ? 2 : 4));   //count
                            data_ui[(idx * 16) + 5] = (uint)mesh.InstanceCount;   //instanceCount
                            data_ui[(idx * 16) + 6] = (uint)((mesh.Mesh.AllocIndices[k] * mesh.Mesh.BlockSize) / (idxType == IndexType.UShort ? 2 : 4));   //firstIndex
                            data_ui[(idx * 16) + 7] = 0;   //baseVertex
                            data_ui[(idx * 16) + 8] = (uint)mesh.BaseInstance;   //baseInstance
                            data_ui[(idx * 16) + 9] = (uint)mesh.Mesh.AllocIndices[k];
                            data_ui[(idx * 16) + 10] = (uint)mesh.Mesh.BufferIdx;
                            data_ui[(idx * 16) + 11] = mesh.Mesh.BaseVertex;
                            
                            ((float*)data_ui)[(idx * 16) + 12] = min.X;
                            ((float*)data_ui)[(idx * 16) + 13] = min.Y;
                            ((float*)data_ui)[(idx * 16) + 14] = min.Z;
                            
                            ((float*)data_ui)[(idx * 16) + 16] = max.X;
                            ((float*)data_ui)[(idx * 16) + 17] = max.Y;
                            ((float*)data_ui)[(idx * 16) + 18] = max.Z;
                        }

                        idx++;
                    }
                }
                data_ui[0] = (uint)idx;
            }

            //Push the updates
            MultidrawParams.UpdateDone();
        }
    }
}
