using Kokoro.Graphics;
using KokoroVR.Graphics.Lights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Graphics
{
    public class LightManager
    {
        int maxPointLights, maxSpotLights, maxDirecLights;
        private List<PointLight> pointLights;
        private List<SpotLight> spotLights;
        private List<DirectionalLight> direcLights;

        internal ShaderStorageBuffer pointLights_buffer;
        internal ShaderStorageBuffer spotLights_buffer;
        internal ShaderStorageBuffer direcLights_buffer;
        internal int pointLightCnt, spotLightCnt, direcLightCnt;

        public LightManager(int pointLightCapacity, int spotLightCapacity, int direcLightCapacity)
        {
            maxPointLights = pointLightCapacity;
            maxSpotLights = spotLightCapacity;
            maxDirecLights = direcLightCapacity;
            pointLights = new List<PointLight>(pointLightCapacity);
            spotLights = new List<SpotLight>(spotLightCapacity);
            direcLights = new List<DirectionalLight>(direcLightCapacity);

            pointLights_buffer = new ShaderStorageBuffer(PointLight.Size * pointLightCapacity, false);
            spotLights_buffer = new ShaderStorageBuffer(SpotLight.Size * spotLightCapacity, false);
            direcLights_buffer = new ShaderStorageBuffer(DirectionalLight.Size * direcLightCapacity, false);
        }

        public void AddLight(PointLight light)
        {
            if (pointLights.Count == maxPointLights)
                throw new Exception("Pointlight buffer full.");
            pointLights.Add(light);
        }

        public void RemoveLight(PointLight light)
        {
            pointLights.Remove(light);
        }

        public void AddLight(SpotLight light)
        {
            if (spotLights.Count == maxSpotLights)
                throw new Exception("Spotlight buffer full.");
            spotLights.Add(light);
        }

        public void RemoveLight(SpotLight light)
        {
            spotLights.Remove(light);
        }

        public void AddLight(DirectionalLight light)
        {
            if (direcLights.Count == maxDirecLights)
                throw new Exception("Directional light buffer full.");
            direcLights.Add(light);
        }

        public void RemoveLight(DirectionalLight light)
        {
            direcLights.Remove(light);
        }

        public void Render()
        {
            //Render out shadows for casting lights
        }

        public void Update()
        {
            unsafe
            {
                byte* b_ptr;
                float* f_ptr;

                b_ptr = pointLights_buffer.Update();
                f_ptr = (float*)b_ptr;
                for (int i = 0; i < pointLights.Count; i++)
                    if (pointLights[i].Dirty)
                    {
                        f_ptr[(PointLight.Size * i) / sizeof(float) + 0] = pointLights[i].Position.X;
                        f_ptr[(PointLight.Size * i) / sizeof(float) + 1] = pointLights[i].Position.Y;
                        f_ptr[(PointLight.Size * i) / sizeof(float) + 2] = pointLights[i].Position.Z;

                        f_ptr[(PointLight.Size * i) / sizeof(float) + 3] = pointLights[i].Intensity;

                        f_ptr[(PointLight.Size * i) / sizeof(float) + 4] = pointLights[i].Color.X;
                        f_ptr[(PointLight.Size * i) / sizeof(float) + 5] = pointLights[i].Color.Y;
                        f_ptr[(PointLight.Size * i) / sizeof(float) + 6] = pointLights[i].Color.Z;

                        pointLights[i].Dirty = false;
                    }
                pointLights_buffer.UpdateDone();
                pointLightCnt = pointLights.Count;

                b_ptr = spotLights_buffer.Update();
                f_ptr = (float*)b_ptr;
                for (int i = 0; i < spotLights.Count; i++)
                    if (spotLights[i].Dirty)
                    {
                        f_ptr[(SpotLight.Size * i) / sizeof(float) + 0] = spotLights[i].Position.X;
                        f_ptr[(SpotLight.Size * i) / sizeof(float) + 1] = spotLights[i].Position.Y;
                        f_ptr[(SpotLight.Size * i) / sizeof(float) + 2] = spotLights[i].Position.Z;

                        f_ptr[(SpotLight.Size * i) / sizeof(float) + 3] = spotLights[i].Intensity;

                        f_ptr[(SpotLight.Size * i) / sizeof(float) + 4] = spotLights[i].Direction.X;
                        f_ptr[(SpotLight.Size * i) / sizeof(float) + 5] = spotLights[i].Direction.Y;
                        f_ptr[(SpotLight.Size * i) / sizeof(float) + 6] = spotLights[i].Direction.Z;

                        f_ptr[(SpotLight.Size * i) / sizeof(float) + 7] = (float)Math.Cos(spotLights[i].Angle);

                        f_ptr[(SpotLight.Size * i) / sizeof(float) + 8] = spotLights[i].Color.X;
                        f_ptr[(SpotLight.Size * i) / sizeof(float) + 9] = spotLights[i].Color.Y;
                        f_ptr[(SpotLight.Size * i) / sizeof(float) + 10] = spotLights[i].Color.Z;

                        spotLights[i].Dirty = false;
                    }
                spotLights_buffer.UpdateDone();
                spotLightCnt = spotLights.Count;

                b_ptr = direcLights_buffer.Update();
                f_ptr = (float*)b_ptr;
                for (int i = 0; i < direcLights.Count; i++)
                    if (direcLights[i].Dirty)
                    {
                        f_ptr[(DirectionalLight.Size * i) / sizeof(float) + 0] = direcLights[i].Direction.X;
                        f_ptr[(DirectionalLight.Size * i) / sizeof(float) + 1] = direcLights[i].Direction.Y;
                        f_ptr[(DirectionalLight.Size * i) / sizeof(float) + 2] = direcLights[i].Direction.Z;

                        f_ptr[(DirectionalLight.Size * i) / sizeof(float) + 3] = direcLights[i].Intensity;

                        f_ptr[(DirectionalLight.Size * i) / sizeof(float) + 4] = direcLights[i].Color.X;
                        f_ptr[(DirectionalLight.Size * i) / sizeof(float) + 5] = direcLights[i].Color.Y;
                        f_ptr[(DirectionalLight.Size * i) / sizeof(float) + 6] = direcLights[i].Color.Z;


                        direcLights[i].Dirty = false;
                    }
                direcLights_buffer.UpdateDone();
                direcLightCnt = direcLights.Count;
            }
        }
    }
}
