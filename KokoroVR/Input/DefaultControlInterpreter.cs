using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kokoro.Graphics;
using Kokoro.Math;
using KokoroVR.Graphics;

namespace KokoroVR.Input
{
    public class DefaultControlInterpreter : ControlInterpreter
    {
        private string[] hndl;
        private Mesh[][] mesh;
        private Texture[][] tex;
        private TextureHandle[][] texHandles;
        private MeshGroup meshGroup;
        private VRClient.PoseData[] poses;

        public DefaultControlInterpreter(string left, string right, MeshGroup grp)
        {
            hndl = new string[] { left, right };
            mesh = new Mesh[2][];
            tex = new Texture[2][];
            texHandles = new TextureHandle[2][];
            meshGroup = grp;
            poses = new VRClient.PoseData[2];
        }

        public override void Render(double time, Framebuffer fbuf, StaticMeshRenderer staticMesh, DynamicMeshRenderer dynamicMesh, Matrix4 p, Matrix4 v, VRHand eye)
        {
            try
            {
                if (mesh[eye] == null || mesh[eye].Length == 0)
                {
                    Engine.HMDClient.GetControllerMesh(poses[eye].ActiveOrigin, meshGroup, out mesh[eye], out tex[eye]);
                    texHandles[eye] = new TextureHandle[tex[eye].Length];
                    for (int i = 0; i < tex[eye].Length; i++)
                    {
                        texHandles[eye][i] = tex[eye][i].GetHandle(TextureSampler.Default);
                        texHandles[eye][i].SetResidency(Residency.Resident);
                    }
                }

                var transforms = Engine.HMDClient.GetComponentTransforms(poses[eye].ActiveOrigin);
                for (int i = 0; i < transforms.Length; i++)
                    staticMesh.DrawC(mesh[eye][i], transforms[i] * poses[eye].PoseMatrix, texHandles[eye][i]);
            }
            catch (Exception) { }
        }

        public override void Update(double time, World parent)
        {
            //TODO Compute pointer ray and process it
            Engine.CurrentPlayer.GetControl(hndl[0], out poses[0]);
            Engine.CurrentPlayer.GetControl(hndl[1], out poses[1]);


        }
    }
}
