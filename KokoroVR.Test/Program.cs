using Kokoro.Graphics;
using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Engine.Initialize(ExperienceKind.Standing);

            var w = new World("TestWorld", 10);
            w.Initializer = () =>
            {
                Engine.SetupControllers(@"manifests\actions.json", new VRActionSet[]
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
                    });

                MeshGroup grp = new MeshGroup(MeshGroupVertexFormat.X32F_Y32F_Z32F, 40000, 40000);
                /*w.LightManager.AddLight(new Graphics.Lights.PointLight()
                {
                    Color = Vector3.One,
                    Intensity = 10.0f,
                    Position = Vector3.Zero
                });*/

                w.LightManager.AddLight(new Graphics.Lights.DirectionalLight()
                {
                    Color = Vector3.One,
                    Direction = Vector3.UnitX,
                    Intensity = 10.0f
                });

                w.AddInterpreter(new Input.DefaultControlInterpreter("/actions/vrworld/in/hand_left", "/actions/vrworld/in/hand_right", grp));
            };
            Engine.AddWorld(w);
            Engine.SetActiveWorld("TestWorld");
            Engine.Start();
        }
    }
}
