﻿using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KokoroVR.VRClient;

namespace KokoroVR
{
    public abstract class Player
    {
        public abstract Vector3 Position { get; internal set; }
        public abstract Quaternion Orientation { get; internal set; }
        public abstract float Height { get; protected set; }

        public abstract void GetControl(string name, out AnalogData val);
        public abstract void GetControl(string name, out DigitalData val);
        public abstract void GetControl(string name, out PoseData val);

        public abstract void Update(double time);
    }
}
