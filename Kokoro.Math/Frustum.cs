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
        public Frustum(Matrix4 v, Matrix4 p, Vector3 eyePos)
        {
            //inverse(v * p) applied to clip space vectors is then used to extract frustum planes
            //determine which side of each plane the sphere is on
            var iVP = Matrix4.Invert(v * p);
            
            var ntl = Vector4.Transform(new Vector4(-1, 1, 0.1f, 1), iVP);
            var ntr = Vector4.Transform(new Vector4(1, 1, 0.1f, 1), iVP);
            var nbl = Vector4.Transform(new Vector4(-1, -1, 0.1f, 1), iVP);
            var nbr = Vector4.Transform(new Vector4(1, -1, 0.1f, 1), iVP);

            ntl /= ntl.W;
            ntr /= ntr.W;
            nbl /= nbl.W;
            nbr /= nbr.W;

            //Compute planes
            near = new Plane(nbl.Xyz, nbr.Xyz, ntr.Xyz);
            lft = new Plane(eyePos, ntl.Xyz, nbl.Xyz);
            rgt = new Plane(eyePos, nbr.Xyz, ntr.Xyz);
            top = new Plane(ntl.Xyz, eyePos, ntr.Xyz);
            btm = new Plane(eyePos, nbl.Xyz, nbr.Xyz);
        }

        public bool IsVisible(Vector4 Sphere)
        {

            var sphere_c = Sphere.Xyz;

            float lft_d = PlaneHelper.ClassifyPoint(ref sphere_c, ref lft);
            float rgt_d = PlaneHelper.ClassifyPoint(ref sphere_c, ref rgt);
            float top_d = PlaneHelper.ClassifyPoint(ref sphere_c, ref top);
            float btm_d = PlaneHelper.ClassifyPoint(ref sphere_c, ref btm);
            float near_d = PlaneHelper.ClassifyPoint(ref sphere_c, ref near);

            float limit = 0;// Sphere.W;

            if (lft_d < limit) return true;
            if (rgt_d < limit) return true;
            if (top_d < limit) return true;
            if (btm_d < limit) return true;
            if (near_d < limit) return true;

            return false;
        }
    }
}
