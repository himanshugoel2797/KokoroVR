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

        public static ShaderSource LoadV(string file, int vertW, int propW, int indexW)
        {
            if (!File.Exists(file))
            {
                if (File.Exists(Path.Combine(ShaderPath, file)))
                    file = Path.Combine(ShaderPath, file);
            }

            var src = File.ReadAllText(file);

            string pull_decls = "";
            string sub_code = "";
            if (indexW == 0)
            {
                pull_decls += "uniform int _blk_size;";
                if (vertW == 32)
                    pull_decls += $"layout(rgba32f, bindless_image) restrict readonly uniform imageBuffer vs_pos_d;\n";
                else
                    pull_decls += $"layout(rgba{vertW}i, bindless_image) restrict readonly uniform iimageBuffer vs_pos_d;\n";

                if (propW != 0)
                    pull_decls += $"layout(rgba{propW}f, bindless_image) restrict readonly uniform imageBuffer vs_props;\n";

                sub_code += "int _idx = int(gl_VertexID / _blk_size);";

                if (vertW == 32)
                    sub_code += "vec4 vs_pos = imageLoad(vs_pos_d, gl_VertexID);\n";
                else
                    sub_code += "ivec4 vs_pos = imageLoad(vs_pos_d, gl_VertexID);\n";
                if (propW != 0)
                    sub_code += "vec4 vs_prop_tmp = imageLoad(vs_props, gl_VertexID);\n vec2 vs_normal = vs_prop_tmp.xy;\n vec2 vs_uv = vs_prop_tmp.zw;\n";

                src = pull_decls + src;
                src = src.Replace("FETCH_CODE_BLOCK", sub_code);
            }
            else
            {
                pull_decls += "uniform int _blk_size;";
                if (vertW == 32)
                    pull_decls += $"layout(rgba32f, bindless_image) restrict readonly uniform imageBuffer vs_pos_d;\n";
                else
                    pull_decls += $"layout(rgba{vertW}i, bindless_image) restrict readonly uniform iimageBuffer vs_pos_d;\n";

                if (propW != 0)
                    pull_decls += $"layout(rgba{propW}f, bindless_image) restrict readonly uniform imageBuffer vs_props;\n";

                pull_decls += $"layout(r{indexW}ui, bindless_image) restrict readonly uniform uimageBuffer vs_indices;\n";

                sub_code += "int _idx = (gl_VertexID - gl_BaseVertex) + gl_BaseVertex * _blk_size;";
                sub_code += "int vs_index = imageLoad(vs_indices, _idx).r;\n";
                if (vertW == 32)
                    sub_code += "vec4 vs_pos = imageLoad(vs_pos_d, vs_index);\n";
                else
                    sub_code += "ivec4 vs_pos = imageLoad(vs_pos_d, vs_index);\n";
                if (propW != 0)
                    sub_code += "vec4 vs_prop_tmp = imageLoad(vs_props, vs_index);\n vec2 vs_normal = vs_prop_tmp.xy;\n vec2 vs_uv = vs_prop_tmp.zw;\n";

                src = pull_decls + src;
                src = src.Replace("FETCH_CODE_BLOCK", sub_code);
            }

            return new ShaderSource(ShaderType.VertexShader, src, "");
        }

        public static ShaderSource Load(ShaderType sType, string file)
        {
            if (!File.Exists(file))
            {
                if (File.Exists(Path.Combine(ShaderPath, file)))
                    file = Path.Combine(ShaderPath, file);
            }
            return new ShaderSource(sType, File.ReadAllText(file), "");
        }

        public static ShaderSource Load(ShaderType sType, string file, string defines, params string[] libraryName)
        {
            if (!File.Exists(file))
            {
                if (File.Exists(Path.Combine(ShaderPath, file)))
                    file = Path.Combine(ShaderPath, file);
            }
            return new ShaderSource(sType, File.ReadAllText(file), defines, libraryName);
        }
        #endregion

        internal int id;
        internal ShaderType sType;

        public ShaderSource(Kokoro.Graphics.ShaderType sType, string src, string defines, params string[] libraryName)
        {
            string preamble = $"#version 460 core\n#extension GL_ARB_bindless_texture : require\n#extension GL_AMD_vertex_shader_viewport_index : require\n#extension GL_ARB_shader_draw_parameters : require\n #define MAX_DRAWS_UBO {GraphicsDevice.MaxIndirectDrawsUBO}\n #define MAX_DRAWS_SSBO {GraphicsDevice.MaxIndirectDrawsSSBO}\n #define PI {System.Math.PI}\n";

            string shaderSrc = preamble + defines;

            if (libraryName != null)
            {
                var libs = Graphics.ShaderLibrary.GetLibraries(libraryName);
                for (int i = 0; i < libs.Length; i++)
                    for (int j = 0; j < libs[i].Sources.Count; j++)
                        shaderSrc += libs[i].Sources[j] + "\n";

            }
            shaderSrc += src;

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