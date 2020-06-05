using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace KokoroVR2.MeshPipeline
{
    public class Compressonator
    {
        private string execPath;
        private int tid;
        public Compressonator(int tid)
        {
            this.tid = tid;
            execPath = Path.Combine(Environment.GetEnvironmentVariable("COMPRESSONATOR_ROOT"), "bin", "CLI", "CompressonatorCLI.exe");
        }

        private static string GetOutputPath(int tid, int lod)
        {
            return Path.Combine("mesh_pipeline_kokorovr2", $"{tid}_{lod}.gltf");
        }

        public string Exec(string inputFile, int lod_lv)
        {
            if (lod_lv == 0)
            {
                var o_file = GetOutputPath(tid, lod_lv);
                File.Copy(inputFile, o_file, true);
                return o_file;
            }
            else
            {
                var outputFile = GetOutputPath(tid, lod_lv);
                Process p = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = execPath,
                        Arguments = $"-log -meshopt -simplifyMeshLOD {lod_lv} -optVFetch 1 -optOverdrawACMRThres 1.03 -optVCacheSize 32 \"{inputFile}\" \"{outputFile}\""
                    }
                };
                p.Start();
                p.WaitForExit();
                return outputFile;
            }
        }
    }
}
