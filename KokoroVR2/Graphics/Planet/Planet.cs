using System;
using Kokoro.Common;

namespace KokoroVR2.Graphics.Planet
{
    public class Planet : UniquelyNamedObject
    {
        readonly TerrainFace[] face;
        public Planet(string name, string bufname, float radius, TerrainCache cache) : base(name)
        {
            face = new TerrainFace[6];
            for (int i = 0; i < 6; i++)
            {
                face[i] = new TerrainFace(name + "_" + i, (TerrainFaceIndex)i, radius, cache);
                face[i].heightDataBufferName = bufname;
            }
        }

        public void Update()
        {
            for (int i = 0; i < 6; i++)
                face[i].Update();
        }

        public void Render(string imgName, string depthName)
        {
            for (int i = 0; i < 6; i++)
                face[i].Render(imgName, depthName);
        }

        public void RebuildGraph()
        {
            for (int i = 0; i < 6; i++)
                face[i].RebuildGraph();
        }
    }
}
