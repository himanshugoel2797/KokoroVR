using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using System.IO;

namespace Kokoro.Graphics
{
    public class ShaderSource : IDisposable
    {
        #region Static Methods
        //#if DEBUG
        public const string ShaderPath = @"I:\Code\KokoroVR\Resources\OpenGL";
        //#endif

        public static ShaderSource Load(ShaderType sType, string file)
        {
            if (!File.Exists(file))
            {
                if (File.Exists(Path.Combine(ShaderPath, file)))
                    file = Path.Combine(ShaderPath, file);
            }
            var src = File.ReadAllText(file);
            return new ShaderSource(sType, file, src, "");
        }

        public static ShaderSource Load(ShaderType sType, string file, string defines, params string[] libraryName)
        {
            if (!File.Exists(file))
            {
                if (File.Exists(Path.Combine(ShaderPath, file)))
                    file = Path.Combine(ShaderPath, file);
            }
            var src = File.ReadAllText(file);
            return new ShaderSource(sType, file, src, defines, libraryName);
        }
        #endregion

        internal int id;
        internal ShaderType sType;

        public ShaderSource(Kokoro.Graphics.ShaderType sType, string filename, string src, string defines, params string[] libraryName)
        {
            string preamble = $"#version 460 core\n#extension GL_ARB_gpu_shader_int64 : require\n#extension GL_AMD_gpu_shader_half_float : require\n#extension GL_AMD_gpu_shader_half_float_fetch : require\n#extension GL_AMD_gpu_shader_int16 : require\n#extension GL_ARB_shader_ballot : require\n#extension GL_ARB_bindless_texture : require\n#extension GL_AMD_vertex_shader_viewport_index : require\n#extension GL_ARB_shader_draw_parameters : require\n #define MAX_DRAWS_UBO {GraphicsDevice.MaxIndirectDrawsUBO}\n #define MAX_DRAWS_SSBO {GraphicsDevice.MaxIndirectDrawsSSBO}\n #define PI {System.Math.PI}\n";

            string shaderSrc = preamble + defines;

            if (libraryName != null)
            {
                var libs = Graphics.ShaderLibrary.GetLibraries(libraryName);
                for (int i = 0; i < libs.Length; i++)
                    for (int j = 0; j < libs[i].Sources.Count; j++)
                        shaderSrc += libs[i].Sources[j] + "\n";

            }
            shaderSrc += src;
            File.WriteAllText(Path.ChangeExtension(filename, ".glsl_out"), $"//{sType}\n" + shaderSrc);

            id = GL.CreateShader((OpenTK.Graphics.OpenGL4.ShaderType)sType);
            GL.ShaderSource(id, shaderSrc);
            GL.CompileShader(id);

            this.sType = sType;

            GL.GetShader(id, ShaderParameter.CompileStatus, out int result);
            if (result == 0)
            {
                //Fetch the error log
                GL.GetShaderInfoLog(id, out string errorLog);

                GL.DeleteShader(id);

                Console.WriteLine(errorLog);
                throw new Exception("Shader Compilation Exception : " + errorLog);
            }
            GraphicsDevice.Cleanup.Add(Dispose);
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
                }

                if (id != 0) GraphicsDevice.QueueForDeletion(id, GLObjectType.Shader);
                id = 0;
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