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

    public class RenderQueue2
    {

        private readonly List<MeshData> MeshGroups;
        private readonly uint maxDrawCount = 0;
        private readonly bool transient;
        private readonly IndexType idxType;

        public bool ClearFramebufferBeforeSubmit { get; set; } = false;
        public StorageBuffer MultidrawParams { get; }

        public RenderQueue2(uint MaxDrawCount, IndexType t, bool transient)
        {
            this.transient = transient;
            this.idxType = t;
            MeshGroups = new List<MeshData>();

            if (MaxDrawCount < 4096)
                MaxDrawCount = 4096;

            maxDrawCount = MaxDrawCount;
            MultidrawParams = new StorageBuffer((MaxDrawCount * 5 + 4) * sizeof(uint), transient);
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
                        (int k, uint cnt) = sorted_draws[q];
                        if (idxType == IndexType.None)
                        {
                            data_ui[(idx * 4) + 1] = (uint)cnt;   //count
                            data_ui[(idx * 4) + 2] = (uint)mesh.InstanceCount;   //instanceCount
                            data_ui[(idx * 4) + 3] = (uint)(mesh.Mesh.AllocIndices[k] * mesh.Mesh.BlockSize);   //baseVertex
                            data_ui[(idx * 4) + 4] = (uint)mesh.BaseInstance;   //baseInstance
                        }
                        else
                        {
                            data_ui[(idx * 5) + 1] = (uint)(cnt / (idxType == IndexType.UShort ? 2 : 4));   //count
                            data_ui[(idx * 5) + 2] = (uint)mesh.InstanceCount;   //instanceCount
                            data_ui[(idx * 5) + 3] = (uint)((mesh.Mesh.AllocIndices[k] * mesh.Mesh.BlockSize) / (idxType == IndexType.UShort ? 2 : 4));   //firstIndex
                            data_ui[(idx * 5) + 4] = (uint)mesh.Mesh.AllocIndices[k];   //baseVertex
                            data_ui[(idx * 5) + 5] = (uint)mesh.BaseInstance;   //baseInstance
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
