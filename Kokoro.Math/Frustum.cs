using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Math
{
    public class Frustum
    {
        Plane lft, rgt, top, btm, near;

        public Vector3 EyePosition { get; private set; }
        public Frustum(Matrix4 v, Matrix4 p, Vector3 eyePos)
        {
            //inverse(v * p) applied to clip space vectors is then used to extract frustum planes
            //determine which side of each plane the sphere is on
            var iVP = Matrix4.Invert(v * p);

            var ntl = Vector4.Transform(new Vector4(-1, 1, 1f, 1), iVP);
            var ntr = Vector4.Transform(new Vector4(1, 1, 1f, 1), iVP);
            var nbl = Vector4.Transform(new Vector4(-1, -1, 1f, 1), iVP);
            var nbr = Vector4.Transform(new Vector4(1, -1, 1f, 1), iVP);

            var ftl = Vector4.Transform(new Vector4(-1, 1, 0.00001f, 1), iVP);
            var ftr = Vector4.Transform(new Vector4(1, 1, 0.00001f, 1), iVP);
            var fbl = Vector4.Transform(new Vector4(-1, -1, 0.00001f, 1), iVP);
            var fbr = Vector4.Transform(new Vector4(1, -1, 0.00001f, 1), iVP);

            ntl /= ntl.W;
            ntr /= ntr.W;
            nbl /= nbl.W;
            nbr /= nbr.W;

            ftl /= ftl.W;
            ftr /= ftr.W;
            fbl /= fbl.W;
            fbr /= fbr.W;

            //Compute planes
            near = new Plane(nbl.Xyz, nbr.Xyz, ntl.Xyz);
            lft = new Plane(nbl.Xyz, ntl.Xyz, fbl.Xyz);
            rgt = new Plane(ntr.Xyz, nbr.Xyz, ftr.Xyz);
            top = new Plane(ntl.Xyz, ntr.Xyz, ftl.Xyz);
            btm = new Plane(nbr.Xyz, nbl.Xyz, fbr.Xyz);

            EyePosition = eyePos;
        }

        public bool IsVisible(Vector4 Sphere)
        {

            var sphere_c = Sphere.Xyz;

            float lft_d = PlaneHelper.ClassifyPoint(ref sphere_c, ref lft);
            float rgt_d = PlaneHelper.ClassifyPoint(ref sphere_c, ref rgt);
            float top_d = PlaneHelper.ClassifyPoint(ref sphere_c, ref top);
            float btm_d = PlaneHelper.ClassifyPoint(ref sphere_c, ref btm);
            float near_d = PlaneHelper.ClassifyPoint(ref sphere_c, ref near);

            float limit = Sphere.W;

            if (lft_d > limit) return false;
            if (rgt_d > limit) return false;
            if (top_d > limit) return false;
            if (btm_d > limit) return false;
            if (near_d > limit) return false;

            return !false;
        }

        private int CalcPlaneBox(Vector3 min, Vector3 max, Plane p)
        {
            Vector3 vmi = Vector3.Zero;

            Vector3 a = new Vector3(min.X, min.Y, min.Z);
            Vector3 b = new Vector3(max.X, min.Y, min.Z);
            Vector3 c = new Vector3(min.X, max.Y, min.Z);
            Vector3 d = new Vector3(max.X, max.Y, min.Z);
            Vector3 e = new Vector3(min.X, min.Y, max.Z);
            Vector3 f = new Vector3(max.X, min.Y, max.Z);
            Vector3 g = new Vector3(min.X, max.Y, max.Z);
            Vector3 h = new Vector3(max.X, max.Y, max.Z);

            int o = 0;
            o += (PlaneHelper.ClassifyPoint(ref a, ref p) < 0.0f) ? 1 : 0;
            o += (PlaneHelper.ClassifyPoint(ref b, ref p) < 0.0f) ? 1 : 0;
            o += (PlaneHelper.ClassifyPoint(ref c, ref p) < 0.0f) ? 1 : 0;
            o += (PlaneHelper.ClassifyPoint(ref d, ref p) < 0.0f) ? 1 : 0;
            o += (PlaneHelper.ClassifyPoint(ref e, ref p) < 0.0f) ? 1 : 0;
            o += (PlaneHelper.ClassifyPoint(ref f, ref p) < 0.0f) ? 1 : 0;
            o += (PlaneHelper.ClassifyPoint(ref g, ref p) < 0.0f) ? 1 : 0;
            o += (PlaneHelper.ClassifyPoint(ref h, ref p) < 0.0f) ? 1 : 0;
            if (o == 8) return 0;

            return 1;
        }

        public bool IsVisible(Vector3 min, Vector3 max)
        {
            if (CalcPlaneBox(min, max, lft) == 0)
                return false;
            if (CalcPlaneBox(min, max, rgt) == 0)
                return false;
            if (CalcPlaneBox(min, max, top) == 0)
                return false;
            if (CalcPlaneBox(min, max, btm) == 0)
                return false;
            if (CalcPlaneBox(min, max, near) == 0)
                return false;
            return true;
        }
    }
}
