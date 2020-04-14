using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Text;

namespace KokoroVR
{
    public enum EVREye
    {
        Eye_Left = 0,
        Eye_Right = 1
    }

    public class VRHand
    {
        public static readonly VRHand Left = new VRHand(0);
        public static readonly VRHand Right = new VRHand(1);

        public int Value { get; }

        private VRHand(int i)
        {
            Value = i;
        }

        public static VRHand Get(int i)
        {
            if (i == Left.Value)
                return Left;
            else if (i == Right.Value)
                return Right;
            throw new ArgumentException();
        }

        public override bool Equals(object obj)
        {
            var hand = obj as VRHand;
            return hand != null &&
                   Value == hand.Value;
        }

        public override int GetHashCode()
        {
            return -1937169414 + Value.GetHashCode();
        }

        public static implicit operator EVREye(VRHand hand)
        {
            return (hand == Left) ? EVREye.Eye_Left : EVREye.Eye_Right;
        }

        public static implicit operator int(VRHand hand)
        {
            return hand.Value;
        }

        public static bool operator ==(VRHand a, VRHand b)
        {
            return a.Value == b.Value;
        }

        public static bool operator !=(VRHand a, VRHand b)
        {
            return a.Value != b.Value;
        }
    }

    public enum VREye
    {
        Left = EVREye.Eye_Left,
        Right = EVREye.Eye_Right
    }

    public enum ActionHandleDirection
    {
        Input = 0,
        Output = 1,
    }

    public enum ActionKind
    {
        Unknown,
        Digital,
        Analog,
        Pose,
        Haptic
    }

    public enum ExperienceKind
    {
        Standing,
        Seated,
    }

    public class VRActionSet
    {
        public string Name { get; }
        public VRAction[] Actions { get; }

        public VRActionSet(string name, params VRAction[] actions)
        {
            Name = name;
            Actions = actions;
            for (int i = 0; i < actions.Length; i++)
                actions[i].Name = name + actions[i].Name;
        }
    }

    public class VRAction
    {
        public string Name { get; internal set; }
        public ActionHandleDirection Direction { get; }
        public ActionKind Kind { get; }

        public VRAction(string name, ActionHandleDirection direction, ActionKind kind)
        {
            Name = (direction == ActionHandleDirection.Input ? "/in/" : "/out/") + name;
            Direction = direction;
            Kind = kind;
        }
    }

    public struct AnalogData
    {
        public Vector3 Position { get; private set; }
        public Vector3 Delta { get; private set; }
        public float TimeOffset { get; private set; }

        internal AnalogData(Vector3 pos, Vector3 delta, float timeOff)
        {
            Position = pos;
            Delta = delta;
            TimeOffset = timeOff;
        }
    }

    public struct DigitalData
    {
        public bool State { get; private set; }
        public bool Changed { get; private set; }
        public float TimeOffset { get; private set; }

        internal DigitalData(bool state, bool changed, float timeOff)
        {
            State = state;
            Changed = changed;
            TimeOffset = timeOff;
        }
    }

    public struct PoseData
    {
        public Matrix4 PoseMatrix { get; private set; }
        public Vector3 Position { get; private set; }
        public Quaternion Orientation { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Vector3 AngularVelocity { get; private set; }
        public ulong ActiveOrigin { get; private set; }

        public float TimeOffset { get; private set; }

        /*internal PoseData(HmdMatrix34_t mat, bool hmd, float timeOff, HmdVector3_t vel, HmdVector3_t angular_vel, ulong origin)
        {
            PoseMatrix = new Matrix4(mat.m0, mat.m4, mat.m8, 0,
               mat.m1, mat.m5, mat.m9, 0,
               mat.m2, mat.m6, mat.m10, 0,
               mat.m3, mat.m7, mat.m11, 1);
            if (hmd) PoseMatrix = Matrix4.Invert(PoseMatrix);

            Position = PoseMatrix.Row3.Xyz;
            Orientation = new Quaternion(new Matrix3(
                PoseMatrix.M11, PoseMatrix.M12, PoseMatrix.M13,
                PoseMatrix.M21, PoseMatrix.M22, PoseMatrix.M23,
                PoseMatrix.M31, PoseMatrix.M32, PoseMatrix.M33
                ));

            Velocity = new Vector3(vel.v0, vel.v1, vel.v2);
            AngularVelocity = new Vector3(angular_vel.v0, angular_vel.v1, angular_vel.v2);

            ActiveOrigin = origin;
            TimeOffset = timeOff;
        }*/
    }
}
