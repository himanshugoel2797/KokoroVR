using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KokoroVR2.Graphics
{
    [ProtoContract]
    public class MeshData
    {
        [ProtoMember(1)]
        public uint VertexCount { get; set; }

        [ProtoMember(2)]
        public uint IndexCount { get; set; }

        [ProtoMember(3)]
        public byte[] VertexData { get; set; }

        [ProtoMember(4)]
        public byte[] UVData { get; set; }

        [ProtoMember(5)]
        public byte[] NormalData { get; set; }

        [ProtoMember(6)]
        public byte[] IndexData { get; set; }

        [ProtoMember(7)]
        public uint MaterialID { get; set; }

        [ProtoMember(8)]
        public string Name { get; set; }

        public void Save(string file)
        {
            using (var f = File.Open(file, FileMode.Create, FileAccess.Write))
            {
                Serializer.Serialize(f, this);
            }
        }

        public static MeshData Load(string file)
        {
            using (var f = File.Open(file, FileMode.Open, FileAccess.Read))
            {
                return Serializer.Deserialize<MeshData>(f);
            }
        }
    }
}
