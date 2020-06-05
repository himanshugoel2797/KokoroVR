using System;
using System.Collections.Generic;
using System.Text;

namespace KokoroVR2.Graphics.Voxel
{
    public abstract class VoxelChunk
    {
        public virtual bool IsLoaded { get; set; }
        public virtual bool IsSetup { get; set; }
        public virtual bool IsMeshDirty { get; set; }

        public abstract void Load();
        public abstract void Setup();
        public abstract void GenerateMesh();
        public abstract bool ShouldRender();

    }
}
