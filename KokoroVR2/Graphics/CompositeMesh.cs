using Kokoro.Math;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KokoroVR2.Graphics
{
    [ProtoContract]
    public class MeshNode
    {
        [ProtoMember(1)]
        public Vector3 Offset;

        [ProtoMember(2)]
        public Quaternion Rotation;

        [ProtoMember(3)]
        public Vector3 Scale;

        [ProtoMember(4)]
        public MeshData[] Mesh;

        [ProtoMember(5)]
        public MeshNode[] Children;
    }

    [ProtoContract]
    public class CompositeMesh
    {
        [ProtoMember(1)]
        public MeshNode Root;

        [ProtoMember(2)]
        public PBRMaterial[] Materials;

        public void Save(string file)
        {
            using(var f = File.Open(file, FileMode.Create, FileAccess.Write))
            {
                Serializer.Serialize(f, this);
            }
        }

        public static CompositeMesh Load(string file)
        {
            using (var f = File.Open(file, FileMode.Open, FileAccess.Read))
            {
                return Serializer.Deserialize<CompositeMesh>(f);
            }
        }
    }
}
