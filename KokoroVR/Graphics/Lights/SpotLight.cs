﻿using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Graphics.Lights
{
    public class SpotLight
    {
        public Vector3 Position { get => position; set { if (value != position) Dirty = true; position = value; } }
        public Vector3 Direction { get => direction; set { if (value != direction) Dirty = true; direction = value; } }
        public Vector3 Color { get => color; set { if (value != color) Dirty = true; color = value; } }
        public float Intensity { get => intensity; set { if (value != intensity) Dirty = true; intensity = value; } }
        public float Angle { get => angle; set { if (value != angle) Dirty = true; angle = value; } }
        
        internal bool Dirty = true;
        private Vector3 position;
        private Vector3 color;
        private Vector3 direction;
        private float intensity;
        private float angle;

        public const float Threshold = 0.001f;
        public const int Size = (3 * 4) + (3 * 4) + (3 * 4) + 4 + 4 + 4;
    }
}
