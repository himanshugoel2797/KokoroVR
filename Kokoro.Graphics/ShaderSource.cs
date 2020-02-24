using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VulkanSharp.Raw.Vk;

namespace Kokoro.Graphics
{
    public class ShaderSource : IDisposable
    {
        private struct SpecializationInfo
        {
            public uint id;
            public uint offset;
            public ulong size;
        }

        #region Static Methods
        //#if DEBUG
        public const string ShaderPath = "I://Code/KokoroVR/Resources/Vulkan/Shaders";
        public const string ShaderPath2 = "Resources/Vulkan/Shaders";
        //#endif

        public static ShaderSource Load(ShaderType sType, string file)
        {
            return Load(sType, file, "");
        }

        public static ShaderSource Load(ShaderType sType, string file, string defines, params string[] libraryName)
        {
            if (!File.Exists(file))
            {
                if (File.Exists(Path.Combine(ShaderPath, file)))
                    file = Path.Combine(ShaderPath, file);
                else if (File.Exists(Path.Combine(ShaderPath2, file)))
                    file = Path.Combine(ShaderPath2, file);
                else if (File.Exists(Path.ChangeExtension(file, ".spv")))
                {
                    //Attempt to load the binary
                    return new ShaderSource(sType, file, "", "", true);
                }
            }
            //Build the binary if the source file is more recent
            var src_time = File.GetLastWriteTimeUtc(file);
            bool rebuild = !File.Exists(Path.ChangeExtension(file, ".spv"));
            if (!rebuild)
            {
                var bin_time = File.GetLastWriteTimeUtc(Path.ChangeExtension(file, ".spv"));
                if (bin_time <= src_time)
                    rebuild = true;
            }
            if (rebuild)
            {
                var src = File.ReadAllText(file);
                return new ShaderSource(sType, file, src, defines, false);
            }
            else
            {
                return new ShaderSource(sType, file, "", "", true);
            }
        }
        #endregion

        public ShaderType ShaderType => sType;

        internal IntPtr[] ids;
        internal ShaderType sType;
        private List<SpecializationInfo> specializationConsts;

        public ShaderSource(ShaderType sType, string filename, string src, string defines, bool loadBuilt)
        {
            if (!loadBuilt)
            {
                string preamble = $"#version 450 core\n#extension GL_ARB_separate_shader_objects : enable\n#define MAX_DRAWS_UBO {GraphicsDevice.MaxIndirectDrawsUBO}\n#define MAX_DRAWS_SSBO {GraphicsDevice.MaxIndirectDrawsSSBO}\n#define PI {System.Math.PI}\n#define EYECOUNT {GraphicsDevice.EyeCount}\n";

                string shaderSrc = preamble + defines;

                shaderSrc += src;
                File.WriteAllText(Path.ChangeExtension(filename, ".glsl_out"), $"//{sType}\n" + shaderSrc);
                //Compile shaders from source in debug mode if spirv output doesn't exist or the source has been updated

                //Trigger rebuild
                var shaderStageStr = sType switch
                {
                    ShaderType.ComputeShader => "comp",
                    ShaderType.FragmentShader => "frag",
                    ShaderType.GeometryShader => "geom",
                    ShaderType.TessControlShader => "tesc",
                    ShaderType.TessEvaluationShader => "tese",
                    ShaderType.VertexShader => "vert",
                    _ => throw new Exception("Unknown shader type")
                };
                Process p = Process.Start("glslc", $"--target-env=vulkan1.2 -fshader-stage={shaderStageStr} {Path.ChangeExtension(filename, ".glsl_out")} -o {Path.ChangeExtension(filename, ".spv")}");
                p.WaitForExit();
            }

            //GraphicsDevice.DebugMessage(Severity.Notification, $"Compiling: {filename} as {sType}");
            this.sType = sType;
            var spv = Path.ChangeExtension(filename, ".spv");
            var spv_b = File.ReadAllBytes(spv);
            var devices = GraphicsDevice.GetDevices();
            unsafe
            {
                ids = new IntPtr[devices.Length];

                fixed (IntPtr* ids_p = ids)
                fixed (byte* spv_p = spv_b)
                {
                    var smCreatInfo = new VkShaderModuleCreateInfo()
                    {
                        sType = VkStructureType.StructureTypeShaderModuleCreateInfo,
                        codeSize = (ulong)spv_b.Length,
                        pCode = (uint*)spv_p,
                    };
                    var smCreatInfo_ptr = smCreatInfo.Pointer();
                    for (int i = 0; i < devices.Length; i++)
                    {
                        if (vkCreateShaderModule(devices[i], smCreatInfo_ptr, null, ids_p + i) != VkResult.Success)
                            throw new Exception("Failed to create shader module!");

                    }
                }
            }

            specializationConsts = new List<SpecializationInfo>();
        }

        public void DefineSpecializationConst(uint id, uint offset, ulong sz)
        {
            specializationConsts.Add(new SpecializationInfo()
            {
                id = id,
                offset = offset,
                size = sz
            });
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    specializationConsts.Clear();
                }

                var devices = GraphicsDevice.GetDevices();
                for (int i = 0; i < devices.Length; i++)
                    vkDestroyShaderModule(devices[i], ids[i], null);
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        ~ShaderSource()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
