using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public static class PerfTimer
    {
        static int id = -1;
        public static void Start()
        {
            if (id == -1) id = GL.GenQuery();
            GL.BeginQuery(QueryTarget.TimeElapsed, id);
        }

        public static long Stop()
        {
            GL.EndQuery(QueryTarget.TimeElapsed);

            int done = 0;
            while (done == 0)
                GL.GetQueryObject(id, GetQueryObjectParam.QueryResultAvailable, out done);

            GL.GetQueryObject(id, GetQueryObjectParam.QueryResult, out long rVal);
            GL.DeleteQuery(id);
            id = -1;

            return rVal;
        }
    }
}