using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kokoro.Math;

namespace KokoroVR
{
    public class LocalPlayer : Player
    {

        public Matrix4 Pose { get; protected set; }
        public Vector3 Velocity { get; protected set; }
        public Vector3 AngularVelocity { get; protected set; }
        public override Vector3 Position { get; protected set; }
        public override Quaternion Orientation { get; protected set; }
        public override float Height { get; protected set; }

#if VR
        private VRClient client;
        public LocalPlayer(VRClient client)
        {
            this.client = client;
        }

        public override void GetControl(string name, out VRClient.AnalogData val)
        {
            val = client.GetAnalogData(name);
        }

        public override void GetControl(string name, out VRClient.DigitalData val)
        {
            val = client.GetDigitalData(name);
        }

        public override void GetControl(string name, out VRClient.PoseData val)
        {
            val = client.GetPoseData(name);
        }

        public override void Update(double time)
        {
            client.Update();
            var pose = client.GetPose();

            Pose = pose.PoseMatrix;
            Velocity = pose.Velocity;
            AngularVelocity = pose.AngularVelocity;
            Position = pose.Position;
            Orientation = pose.Orientation;

            if (Height == 0)
                Height = Position.Y;
        }
#else
        public LocalPlayer()
        {
            Pose = Matrix4.Identity;
        }

        public override void GetControl(string name, out VRClient.AnalogData val)
        {
            throw new NotImplementedException();
        }

        public override void GetControl(string name, out VRClient.DigitalData val)
        {
            throw new NotImplementedException();
        }

        public override void GetControl(string name, out VRClient.PoseData val)
        {
            throw new NotImplementedException();
        }

        public override void Update(double time)
        {

        }
#endif
    }
}
