using System;
using System.Threading;

namespace KokoroVR2.MeshPipeline
{
    class Program
    {
        static void Main(string[] args)
        {
            ProcessMesh(@"I:\Blender\New Folder\0_2.glb", "0_2.kvm", 3);
        }

        static void ProcessMesh(string file, string dest, int lod_cnt)
        {
            var m = new ModelLoader(0);
            m.BaseUnpack(file, dest);

            var thds = new Thread[lod_cnt];
            for (int i = 0; i < thds.Length; i++)
            {
                thds[i] = new Thread((tid) =>
                {
                    ModelLoader m = new ModelLoader((int)tid);
                    m.Load(file, dest, (int)tid);
                });
                thds[i].Start(i);
            }
        }
    }
}
