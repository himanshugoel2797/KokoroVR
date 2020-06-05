using Kokoro.Math;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KokoroVR2.Graphics
{
    [ProtoContract]
    public struct MeshNode
    {
        [ProtoMember(1)]
        public Vector3 Offset;

        [ProtoMember(2)]
        public Quaternion Rotation;

        [ProtoMember(3)]
        public Vector3 Scale;

        [ProtoMember(4)]
        public int Parent;

        [ProtoMember(5)]
        public uint BaseIndex;

        [ProtoMember(6)]
        public uint BaseVertex;

        [ProtoMember(7)]
        public uint IndexCount;

        [ProtoMember(8)]
        public ushort MaterialID;

        [ProtoMember(9)]
        public ushort BoundsID;
    }

    [ProtoContract]
    public class MeshComponent
    {
        [ProtoMember(1)]
        public MeshNode[] Nodes;

        [ProtoMember(2)]
        public PBRMaterial[] Materials;

        [ProtoMember(3)]
        public byte[] VertexData;

        [ProtoMember(4)]
        public byte[] UVData;
        
        [ProtoMember(5)]
        public byte[] IndexData;

        [ProtoMember(6)]
        public byte[] NormalData;

        [ProtoMember(7)]
        public Vector3[] Bounds;

        public void Save(string file)
        {
            using(var f = File.Open(file, FileMode.Create, FileAccess.Write))
            {
                Serializer.Serialize(f, this);
            }
        }

        public static MeshComponent Load(string file)
        {
            using (var f = File.Open(file, FileMode.Open, FileAccess.Read))
            {
                return Serializer.Deserialize<MeshComponent>(f);
            }
        }
    }
}
