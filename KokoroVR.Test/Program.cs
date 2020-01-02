using Kokoro.Graphics;
using Kokoro.Math;
using KokoroVR.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Test
{
    class Program
    {
        class StaticRenderable : Interactable
        {
            private Mesh mesh;
            private TextureHandle def_handle;
            private Vector3 rots;
            public StaticRenderable(Mesh m)
            {
                mesh = m;
                def_handle = Texture.Default.GetHandle(TextureSampler.Default);
                def_handle.SetResidency(Residency.Resident);
            }

            public override void Render(double time, Framebuffer fbuf, StaticMeshRenderer staticMesh, DynamicMeshRenderer dynamicMesh, Matrix4 p, Matrix4 v, VREye eye)
            {
                staticMesh.DrawC(mesh, Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rots.X)) * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rots.Y)) * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rots.Z)) * Matrix4.CreateTranslation(Vector3.UnitX * 2), def_handle);
            }

            public override void Update(double time, World parent)
            {
                rots.X += 0.01f;
                rots.Y += 0.01f;
                rots.Z += 0.01f;
            }
        }

        static void Main(string[] args)
        {
            Engine.Initialize(ExperienceKind.Standing);

            var w = new World("TestWorld", 10);
            w.Initializer = () =>
            {
                /*Engine.SetupControllers(@"manifests\actions.json", new VRActionSet[]
                    {
                    new VRActionSet("/actions/vrworld",
                        new VRAction("pickup", ActionHandleDirection.Input, ActionKind.Digital),
                        new VRAction("activate", ActionHandleDirection.Input, ActionKind.Digital),
                        new VRAction("analog_right", ActionHandleDirection.Input, ActionKind.Analog),
                        new VRAction("analog_left", ActionHandleDirection.Input, ActionKind.Analog),
                        new VRAction("hand_right", ActionHandleDirection.Input, ActionKind.Pose),
                        new VRAction("hand_left", ActionHandleDirection.Input, ActionKind.Pose),
                        new VRAction("menu_right", ActionHandleDirection.Input, ActionKind.Digital),
                        new VRAction("menu_left", ActionHandleDirection.Input, ActionKind.Digital),
                        new VRAction("haptic_right", ActionHandleDirection.Output, ActionKind.Haptic),
                        new VRAction("haptic_left", ActionHandleDirection.Output, ActionKind.Haptic))
                    });*/

                MeshGroup grp = new MeshGroup(MeshGroupVertexFormat.X32F_Y32F_Z32F, 40000, 40000);
                w.LightManager.AddLight(new Graphics.Lights.PointLight()
                {
                    Color = Vector3.UnitX,
                    Intensity = 10.0f,
                    Position = Vector3.UnitZ
                });

                w.LightManager.AddLight(new Graphics.Lights.DirectionalLight()
                {
                    Color = Vector3.UnitY,
                    Direction = -Vector3.UnitY,
                    Intensity = 10.0f
                });

                w.AddRenderable(new StaticRenderable(Kokoro.Graphics.Prefabs.SphereFactory.Create(grp)));
                //w.AddInterpreter(new Input.DefaultControlInterpreter("/actions/vrworld/in/hand_left", "/actions/vrworld/in/hand_right", grp));
            };
            Engine.AddWorld(w);
            Engine.SetActiveWorld("TestWorld");
            Engine.Start();
        }
    }
}
