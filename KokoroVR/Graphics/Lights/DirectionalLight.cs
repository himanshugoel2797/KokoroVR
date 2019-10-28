using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Graphics.Lights
{
    public class DirectionalLight
    {
        public Vector3 Direction { get => direction; set { if (value != direction) Dirty = true; direction = value; } }
        public Vector3 Color { get => color; set { if (value != color) Dirty = true; color = value; } }
        public float Intensity { get => intensity; set { if (value != intensity) Dirty = true; intensity = value; } }
        
        internal bool Dirty = true;
        private Vector3 direction;
        private Vector3 color;
        private float intensity;

        public const int Size = (3 * 4) + (3 * 4) + 4 + 4;
    }
}
