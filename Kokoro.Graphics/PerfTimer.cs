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

    public class PerfTimer : IDisposable
    {
        int id = -1, tstamp_id = -1;
        QueryType queryType;
        long timestamp;
        public PerfTimer(QueryType qType)
        {
            queryType = qType;
        }

        public long Timestamp()
        {
            if (tstamp_id != -1)
            {
                GL.GetQueryObject(tstamp_id, GetQueryObjectParam.QueryResult, out timestamp);
                GL.DeleteQuery(tstamp_id);
                tstamp_id = -1;
            }

            return timestamp;
        }

        public void Start()
        {
            if (tstamp_id == -1) tstamp_id = GL.GenQuery();
            GL.QueryCounter(tstamp_id, QueryCounterTarget.Timestamp);
            if (id == -1) id = GL.GenQuery();
            GL.BeginQuery((QueryTarget)queryType, id);
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

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                //if (id != -1) GL.DeleteQuery(id);
                //if (tstamp_id != -1) GL.DeleteQuery(tstamp_id);

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~PerfTimer()
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