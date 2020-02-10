using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kokoro.Common;
using Kokoro.Math;
using KokoroVR.Input;

namespace KokoroVR
{
    public class LocalPlayer : Player
    {

        public Matrix4 Pose { get; protected set; }
        public Vector3 Velocity { get; protected set; }
        public Vector3 AngularVelocity { get; protected set; }
        public override Vector3 Position { get; set; }
        public override Quaternion Orientation { get; set; }
        public override float Height { get; protected set; }

        public Vector3 Direction;
        public Vector3 Up;

        float leftrightRot = MathHelper.PiOver2;
        float updownRot = -MathHelper.Pi / 10.0f;
        public float rotationSpeed = 200f;
        public float moveSpeed = 50000f;
        Vector2 mousePos;
        Vector3 cameraRotatedUpVector;

        public const string UpBinding = "FirstPersonCamera.Up";
        public const string DownBinding = "FirstPersonCamera.Down";
        public const string LeftBinding = "FirstPersonCamera.Left";
        public const string RightBinding = "FirstPersonCamera.Right";
        public const string ForwardBinding = "FirstPersonCamera.Forward";
        public const string BackwardBinding = "FirstPersonCamera.Backward";
        public const string AccelerateBinding = "FirstPersonCamera.Accelerate";
        public const string DecelerateBinding = "FirstPersonCamera.Decelerate";

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

            if (!Engine.Keyboard.KeyMap.ContainsKey(ForwardBinding)) Engine.Keyboard.KeyMap.Add(ForwardBinding, Key.Up);
            if (!Engine.Keyboard.KeyMap.ContainsKey(BackwardBinding)) Engine.Keyboard.KeyMap.Add(BackwardBinding, Key.Down);
            if (!Engine.Keyboard.KeyMap.ContainsKey(LeftBinding)) Engine.Keyboard.KeyMap.Add(LeftBinding, Key.Left);
            if (!Engine.Keyboard.KeyMap.ContainsKey(RightBinding)) Engine.Keyboard.KeyMap.Add(RightBinding, Key.Right);
            if (!Engine.Keyboard.KeyMap.ContainsKey(UpBinding)) Engine.Keyboard.KeyMap.Add(UpBinding, Key.PageUp);
            if (!Engine.Keyboard.KeyMap.ContainsKey(DownBinding)) Engine.Keyboard.KeyMap.Add(DownBinding, Key.PageDown);
            if (!Engine.Keyboard.KeyMap.ContainsKey(AccelerateBinding)) Engine.Keyboard.KeyMap.Add(AccelerateBinding, Key.Home);
            if (!Engine.Keyboard.KeyMap.ContainsKey(DecelerateBinding)) Engine.Keyboard.KeyMap.Add(DecelerateBinding, Key.End);

            this.Position = -Vector3.UnitX;
            this.Direction = Vector3.UnitX;
            this.Up = Vector3.UnitY;
        }

        public override void GetControl(string name, out AnalogData val)
        {
            throw new NotImplementedException();
        }

        public override void GetControl(string name, out DigitalData val)
        {
            throw new NotImplementedException();
        }

        public override void GetControl(string name, out PoseData val)
        {
            throw new NotImplementedException();
        }

        private Matrix4 UpdateViewMatrix()
        {
            Matrix4 cameraRotation = Matrix4.CreateRotationX(updownRot) * Matrix4.CreateRotationY(leftrightRot);

            Vector3 cameraOriginalTarget = new Vector3(0, 0, 1);
            Vector3 cameraOriginalUpVector = new Vector3(0, 1, 0);

            Direction = Vector3.Transform(cameraOriginalTarget, cameraRotation);
            Vector3 cameraFinalTarget = Position + Direction;

            cameraRotatedUpVector = Vector3.Transform(cameraOriginalUpVector, cameraRotation);

            return Matrix4.LookAt(Position, cameraFinalTarget, cameraRotatedUpVector);
        }

        public override void Update(double time)
        {
            if (Mouse.ButtonsDown.Left)
            {
                if (System.Math.Abs(mousePos.X - Mouse.MousePos.X) > 0) leftrightRot -= (float)MathHelper.DegreesToRadians(rotationSpeed * (mousePos.X - Mouse.MousePos.X) * time / 10000f);
                if (System.Math.Abs(mousePos.Y - Mouse.MousePos.Y) > 0) updownRot -= (float)MathHelper.DegreesToRadians(rotationSpeed * (mousePos.Y - Mouse.MousePos.Y) * time / 10000f);
            }
            else
            {
                mousePos = Mouse.MousePos;
            }
            UpdateViewMatrix();
            Vector3 Right = Vector3.Cross(cameraRotatedUpVector, Direction);

            if (Engine.Keyboard.IsKeyDown(ForwardBinding))
            {
                Position += Direction * (float)(moveSpeed * time / 10000f);
            }
            else if (Engine.Keyboard.IsKeyDown(BackwardBinding))
            {
                Position -= Direction * (float)(moveSpeed * time / 10000f);
            }

            if (Engine.Keyboard.IsKeyDown(LeftBinding))
            {
                Position -= Right * (float)(moveSpeed * time / 10000f);
            }
            else if (Engine.Keyboard.IsKeyDown(RightBinding))
            {
                Position += Right * (float)(moveSpeed * time / 10000f);
            }

//#if DEBUG
            if (Engine.Keyboard.IsKeyDown(DownBinding))
            {
                Position -= cameraRotatedUpVector * (float)(moveSpeed * time / 10000f);
            }
            else if (Engine.Keyboard.IsKeyDown(UpBinding))
            {
                Position += cameraRotatedUpVector * (float)(moveSpeed * time / 10000f);
            }

            if (Engine.Keyboard.IsKeyDown(AccelerateBinding))
            {
                moveSpeed += 0.02f * moveSpeed;
            }
            else if (Engine.Keyboard.IsKeyDown(DecelerateBinding))
            {
                moveSpeed -= 0.02f * moveSpeed;
            }
//#endif
            //View = UpdateViewMatrix();
            Engine.View[0] = Matrix4.LookAt(Position, Position + Direction, cameraRotatedUpVector);
            Engine.Frustums[0] = new Frustum(Engine.View[0], Engine.Projection[0], Position);
        }
#endif
    }
}
