using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GPUPerfAPI.NET
{
    public enum UsageType
    {
        Ratio,
        Percentage,
        Cycles,
        Milliseconds,
        Bytes,
        Items,
        Kilobytes,
        Nanoseconds
    }

    public enum DataType
    {
        Ulong64,
        Double64
    }

    public class Binding
    {
        const string DllName = "Kokoro.AMDGPUPerf.dll";


        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern bool InitializeGPA();

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern void DestroyGPA();

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern void GLOpenContextGPA(IntPtr hndl);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern void GLCloseContextGPA();

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int GetSupportSampleTypesGPA(out ulong bits);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int GetNumCountersGPA(out uint cnt);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int GetCounterNameGPA(uint cnt, StringBuilder name);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int GetCounterIndexGPA([MarshalAs(UnmanagedType.AnsiBStr)] string name, uint cnt);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int GetCounterGroupGPA(uint cnt, StringBuilder name);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int GetCounterDescriptionGPA(uint cnt, StringBuilder name);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int GetCounterDataTypeGPA(uint cnt, out DataType flags);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int GetCounterUsageTypeGPA(uint cnt, out UsageType result);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int GetCounterSampleTypeGPA(uint cnt, out uint result);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int CreateSessionGPA(out ulong session);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int DeleteSessionGPA(ulong session);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int BeginSessionGPA(ulong session);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int EndSessionGPA(ulong session);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int EnableCounterByNameGPA(ulong session, [MarshalAs(UnmanagedType.AnsiBStr)] string name);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int DisableCounterByNameGPA(ulong session, [MarshalAs(UnmanagedType.AnsiBStr)] string name);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int EnableAllCountersGPA(ulong session);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int DisableAllCountersGPA(ulong session);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int GetPassCountGPA(ulong session, out uint cnt);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int GetNumEnabledCountersGPA(ulong session, out uint cnt);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int GetEnabledIndexGPA(ulong session, uint enabled_num, out uint cnt);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int IsCounterEnabledGPA(ulong session, uint idx);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int GLBeginCommandListGPA(ulong session, uint pass_idx, out ulong cmd_list_id);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int GLEndCommandListGPA(ulong cmd_list_id);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int BeginSampleGPA(uint sample_id, ulong cmd_list_id);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int EndSampleGPA(ulong cmd_list_id);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int GetSampleCountGPA(ulong session, out uint sample_cnt);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int IsPassCompleteGPA(ulong session, uint idx);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int IsSessionCompleteGPA(ulong session);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int GetSampleResultSizeGPA(ulong session, uint id, out ulong resultSz);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        public static extern int GetSampleResultGPA(ulong session, uint id, ulong sample_res_size, ulong[] sample_res);
    }
}
