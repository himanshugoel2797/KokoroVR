using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Math
{
    public class Frustum
    {
        public Plane lft, rgt, top, btm, near, far;
        public Matrix4 iVP, v, p;
        BoundingFrustum bf;
        public Frustum(Matrix4 v, Matrix4 p, Vector3 eyePos)
        {
            //inverse(v * p) applied to clip space vectors is then used to extract frustum planes
            //determine which side of each plane the sphere is on
            this.v = v;
            this.p = p;
            iVP = Matrix4.Invert(v * p);
            //var iV = Matrix4.Invert(v);
            //var iPos = Vector4.Transform(new Vector4(0, 0, 0, 1), iV);
            //iPos /= iPos.W;
            //eyePos = Vector3.Zero;

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

            //Console.WriteLine(System.Math.Acos(Vector3.Dot(near.Normal, btm.Normal)) * 180 / 3.14159f);
            //Console.WriteLine(btm.Normal);
        }

        public bool IsVisible(Vector4 Sphere)
        {

            var sphere_c = Sphere.Xyz;
            /*bool lft_inside = PlaneHelper.ClassifyPoint(ref sphere_c, ref lft) < 0.0f;
            bool rgt_inside = PlaneHelper.ClassifyPoint(ref sphere_c, ref rgt) < 0.0f;
            bool top_inside = PlaneHelper.ClassifyPoint(ref sphere_c, ref top) < 0.0f;
            bool btm_inside = PlaneHelper.ClassifyPoint(ref sphere_c, ref btm) < 0.0f;
            bool near_inside = PlaneHelper.ClassifyPoint(ref sphere_c, ref near) < 0.0f;

            if (lft_inside && rgt_inside && top_inside && btm_inside && near_inside) return true;
            */
            float lft_d = PlaneHelper.ClassifyPoint(ref sphere_c, ref lft);
            float rgt_d = PlaneHelper.ClassifyPoint(ref sphere_c, ref rgt);
            float top_d = PlaneHelper.ClassifyPoint(ref sphere_c, ref top);
            float btm_d = PlaneHelper.ClassifyPoint(ref sphere_c, ref btm);
            float near_d = PlaneHelper.ClassifyPoint(ref sphere_c, ref near);

            float limit = 0;// Sphere.W;

            if (lft_d < limit) return !false;
            if (rgt_d < limit) return !false;
            if (top_d < limit) return !false;
            if (btm_d < limit) return !false;
            if (near_d < limit) return !false;

            return !true;
        }
    }
}
