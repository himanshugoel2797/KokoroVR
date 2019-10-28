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
                        f_ptr[PointLight.Size * i + 0] = pointLights[i].Position.X;
                        f_ptr[PointLight.Size * i + 1] = pointLights[i].Position.Y;
                        f_ptr[PointLight.Size * i + 2] = pointLights[i].Position.Z;

                        f_ptr[PointLight.Size * i + 3] = pointLights[i].Radius;

                        f_ptr[PointLight.Size * i + 4] = pointLights[i].Color.X;
                        f_ptr[PointLight.Size * i + 5] = pointLights[i].Color.Y;
                        f_ptr[PointLight.Size * i + 6] = pointLights[i].Color.Z;

                        f_ptr[PointLight.Size * i + 7] = pointLights[i].Intensity;

                        pointLights[i].Dirty = false;
                    }
                pointLights_buffer.UpdateDone();
                pointLightCnt = pointLights.Count;

                b_ptr = spotLights_buffer.Update();
                f_ptr = (float*)b_ptr;
                for (int i = 0; i < spotLights.Count; i++)
                    if (spotLights[i].Dirty)
                    {
                        f_ptr[SpotLight.Size * i + 0] = spotLights[i].Position.X;
                        f_ptr[SpotLight.Size * i + 1] = spotLights[i].Position.Y;
                        f_ptr[SpotLight.Size * i + 2] = spotLights[i].Position.Z;

                        f_ptr[SpotLight.Size * i + 3] = spotLights[i].Radius;

                        f_ptr[SpotLight.Size * i + 4] = spotLights[i].Color.X;
                        f_ptr[SpotLight.Size * i + 5] = spotLights[i].Color.Y;
                        f_ptr[SpotLight.Size * i + 6] = spotLights[i].Color.Z;

                        f_ptr[SpotLight.Size * i + 7] = spotLights[i].Intensity;
                        f_ptr[SpotLight.Size * i + 8] = spotLights[i].Angle;

                        spotLights[i].Dirty = false;
                    }
                spotLights_buffer.UpdateDone();
                spotLightCnt = spotLights.Count;

                b_ptr = direcLights_buffer.Update();
                f_ptr = (float*)b_ptr;
                for (int i = 0; i < direcLights.Count; i++)
                    if (direcLights[i].Dirty)
                    {
                        f_ptr[PointLight.Size * i + 0] = direcLights[i].Direction.X;
                        f_ptr[PointLight.Size * i + 1] = direcLights[i].Direction.Y;
                        f_ptr[PointLight.Size * i + 2] = direcLights[i].Direction.Z;

                        f_ptr[PointLight.Size * i + 3] = direcLights[i].Color.X;
                        f_ptr[PointLight.Size * i + 4] = direcLights[i].Color.Y;
                        f_ptr[PointLight.Size * i + 5] = direcLights[i].Color.Z;

                        f_ptr[PointLight.Size * i + 6] = direcLights[i].Intensity;

                        direcLights[i].Dirty = false;
                    }
                direcLights_buffer.UpdateDone();
                direcLightCnt = direcLights.Count;
            }
        }
    }
}
