using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPUPerfAPI.NET
{
    public class Context
    {
        public static void Initialize()
        {
            var res = Binding.InitializeGPA();
        }

        public Context(IntPtr hndl)
        {
            Binding.GLOpenContextGPA(hndl);
        }

        public Session CreateSession()
        {
            Binding.CreateSessionGPA(out var sessionId);
            return new Session(sessionId);
        }
    }
}
