using System;
using Kokoro.Math;
using Kokoro.Graphics;
using Kokoro.Common;

namespace KokoroVR2.Graphics.Planet
{
    public enum TerrainFaceIndex
    {
        Top = 0,
        Front = 1,
        Left = 2,
        Bottom = 3,
        Back = 4,
        Right = 5,
    }

    public class TerrainFace : UniquelyNamedObject
    {
        StreamableBuffer vertexBuffer;
        GpuBufferView vertBufferView;
        StreamableBuffer indexBuffer;

        public TerrainFace(string name, TerrainFaceIndex faceIndex, float radius) : base(name)
        {
            TerrainTileMesh.Create(1, TerrainTileEdge.None, out var verts, out var indices);
            vertexBuffer = new StreamableBuffer(name + "_verts", (ulong)verts.Length, BufferUsage.UniformTexel);
            indexBuffer = new StreamableBuffer(name + "_indices", (ulong)indices.Length, BufferUsage.Index);

            unsafe
            {
                var b_ptr = vertexBuffer.BeginBufferUpdate();
                for (int i = 0; i < verts.Length; i++)
                    b_ptr[i] = verts[i];
                vertexBuffer.EndBufferUpdate();

                var i_ptr = (ushort*)indexBuffer.BeginBufferUpdate();
                for (int i = 0; i < indices.Length; i++)
                    i_ptr[i] = indices[i];
                indexBuffer.EndBufferUpdate();
            }
        }

        public void RebuildGraph()
        {
            vertexBuffer.RebuildGraph();
            indexBuffer.RebuildGraph();
        }

        public void Update()
        {
            vertexBuffer.Update();
            indexBuffer.Update();
        }
    }
}
