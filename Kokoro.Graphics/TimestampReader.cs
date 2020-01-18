using OpenTK.Graphics.OpenGL4;

namespace Kokoro.Graphics
{
    public class TimestampReader
    {
        int tstamp_id = -1;
        long timestamp;
        public TimestampReader()
        {
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
        }
    }
}