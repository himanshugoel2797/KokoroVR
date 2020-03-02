using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Text;

namespace KokoroVR2
{
    public abstract class Player
    {
        public abstract Vector3 Position { get; set; }
        public abstract Quaternion Orientation { get; set; }
        public abstract float Height { get; protected set; }
        public abstract Vector3 PrevPosition { get; protected set; }

        public abstract void Update(double time);
    }
}
