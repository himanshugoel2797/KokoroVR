using Kokoro.Math;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KokoroVR2.Graphics
{
    [ProtoContract]
    public struct PBRMaterial
    {
        //Serialize/Deserialize
        [ProtoMember(1)]
        public float AlphaFactor;
        
        [ProtoMember(2)]
        public Vector3 EmissiveFactor;
        
        [ProtoMember(3)]
        public float RoughnessFactor;
        
        [ProtoMember(4)]
        public float MetalnessFactor;
        
        [ProtoMember(5)]
        public Vector3 AlbedoFactor;

        public void Save(string file)
        {
            using (var f = File.Open(file, FileMode.Create, FileAccess.Write))
            {
                Serializer.Serialize(f, this);
            }
        }

        public static PBRMaterial Load(string file)
        {
            using (var f = File.Open(file, FileMode.Open, FileAccess.Read))
            {
                return Serializer.Deserialize<PBRMaterial>(f);
            }
        }
    }
}
