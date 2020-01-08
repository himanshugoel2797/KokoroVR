using Kokoro.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.ShaderAnalyzer
{
    public enum GPUArch
    {
        Ellesmere,
        Carrizo,
        Fiji,
        Hawaii,
        gfx900,
        gfx902,
        gfx906,
        gfx1010,
        ArchCount
    }

    public struct ShaderInfo
    {
        public GPUArch Architecture { get; set; }
        public string[] ISA { get; set; }
        public string[] RegisterMap { get; set; }
    }

    public class AMDShaderAnalyzer
    {
        public static AMDShaderAnalyzer[] Analyze(string base_folder)
        {
            var files = Directory.EnumerateFiles(base_folder, "*.glsl_out", SearchOption.AllDirectories).ToArray();
            var analyzer = new AMDShaderAnalyzer[files.Length];

            for (int i = 0; i < analyzer.Length; i++)
            {
                analyzer[i] = new AMDShaderAnalyzer(files[i]);
                analyzer[i].InvokeAnalyzer();
            }

            return analyzer;
        }

        public string ShaderPath { get; private set; }
        public ShaderType ShaderType { get; private set; }
        public string[] Lines { get; private set; }
        public ShaderInfo[] Analysis { get; private set; }


        public AMDShaderAnalyzer(string file)
        {
            ShaderPath = file;
            Lines = File.ReadAllLines(file);
            ShaderType = (ShaderType)Enum.Parse(typeof(ShaderType), Lines[0].Trim().Substring(2));
            Analysis = new ShaderInfo[(int)GPUArch.ArchCount];
        }

        public void InvokeAnalyzer()
        {
            foreach (string file in Directory.EnumerateFiles(".", "*.txt"))
                File.Delete(file);
            foreach (string file in Directory.EnumerateFiles(".", "*.csv"))
                File.Delete(file);

            string sType_str = "";
            switch (ShaderType)
            {
                case ShaderType.VertexShader:
                    sType_str = "vert";
                    break;
                case ShaderType.TessControlShader:
                    sType_str = "tesc";
                    break;
                case ShaderType.TessEvaluationShader:
                    sType_str = "tese";
                    break;
                case ShaderType.GeometryShader:
                    sType_str = "geom";
                    break;
                case ShaderType.FragmentShader:
                    sType_str = "frag";
                    break;
                case ShaderType.ComputeShader:
                    sType_str = "comp";
                    break;
            }

            var exec_path = Path.Combine(Properties.Settings.Default.RGAPath, "rga.exe");
            var proc = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = true,
                    Arguments = $"-s opengl -c Ellesmere -c Carrizo -c Fiji -c Hawaii -c gfx900 -c gfx902 -c gfx906 -c gfx1010 --isa isa.txt --livereg regs.txt -a stats.csv --cfg cfg.dot --{sType_str} {ShaderPath}",
                    FileName = exec_path,
                    WorkingDirectory = Environment.CurrentDirectory
                }
            };
            proc.Start();
            proc.WaitForExit();

            for (int i = 0; i < (int)GPUArch.ArchCount; i++)
                try
                {
                    GPUArch cur_arch = (GPUArch)i;
                    Analysis[(int)cur_arch] = new ShaderInfo()
                    {
                        Architecture = cur_arch,
                        ISA = File.ReadAllLines($"{cur_arch}_isa_{sType_str}.txt"),
                        RegisterMap = File.ReadAllLines($"{cur_arch}_regs_{sType_str}.txt"),
                    };
                }
                catch (Exception) { }

            Console.WriteLine($"Processing: {ShaderPath}, {ShaderType}");
        }
    }
}
