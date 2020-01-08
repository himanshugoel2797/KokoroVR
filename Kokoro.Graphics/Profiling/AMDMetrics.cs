using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics.Profiling
{
    class AMDMetrics
    {
        //Detect gpu architecture
        //choose associated architecture
        //timers - 

        static AMDMetrics()
        {

            StreamWriter cntrs = new StreamWriter("cntrs.txt");

            GL.Amd.GetPerfMonitorGroups(out int perfmonGrpCnt, 0, (int[])null);
            int[] perfmon_grps = new int[perfmonGrpCnt];
            GL.Amd.GetPerfMonitorGroups(out perfmonGrpCnt, perfmon_grps.Length, perfmon_grps);
            foreach (int i in perfmon_grps)
            {
                GL.Amd.GetPerfMonitorGroupString(i, 200, out int grp_name_len, out string grp_name);
                cntrs.WriteLine(grp_name);

                GL.Amd.GetPerfMonitorCounters(i, out int cntr_num, out int max_active_cntrs, 0, null);
                int[] cntr_ids = new int[cntr_num];
                GL.Amd.GetPerfMonitorCounters(i, out cntr_num, out max_active_cntrs, cntr_ids.Length, cntr_ids);

                foreach (int j in cntr_ids)
                {
                    GL.Amd.GetPerfMonitorCounterString(i, j, 200, out int cntr_name_len, out string cntr_name);
                    cntrs.WriteLine("\t" + cntr_name);
                }
            }
        }
    }
}
