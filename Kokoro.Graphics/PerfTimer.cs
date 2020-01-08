using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Graphics
{
    public enum QueryType
    {
        Time = 0x88BF,
        VerticesSubmitted = 0x82EE,
        PrimitivesSubmitted = 0x82EF,
        VertexShaderInvocations = 0x82F0,
        TessControlShaderPatches = 0x82F1,
        TessEvalShaderInvocations = 0x82F2,
        GeometryShaderInvocations = 0x887F,
        GeometryShaderPrimitivesEmitted = 0x82F3,
        FragmentShaderInvocations = 0x82F4,
        ComputeShaderInvocations = 0x82F5,
        ClippingInputPrimitives = 0x82F6,
        ClippingOutputPrimitives = 0x82F7,
    }

    public class PerfTimer
    {
        int id = -1;
        QueryType queryType;
        long timestamp;
        public PerfTimer(QueryType qType)
        {
            queryType = qType;
        }

        public long Timestamp()
        {
            return timestamp;
        }

        public void Start()
        {
            if (id == -1) id = GL.GenQuery();
            GL.BeginQuery((QueryTarget)queryType, id);
            GL.GetQuery(QueryTarget.Timestamp, GetQueryParam.CurrentQuery, out int val);
            timestamp = val;
        }

        public void Stop()
        {
            GL.EndQuery((QueryTarget)queryType);
        }

        public bool IsReady()
        {
            GL.GetQueryObject(id, GetQueryObjectParam.QueryResultAvailable, out int done);
            if (done != 0)
                return true;
            return false;
        }

        public long Read()
        {
            GL.GetQueryObject(id, GetQueryObjectParam.QueryResult, out long rVal);
            GL.DeleteQuery(id);
            id = -1;

            return rVal;
        }
    }
}