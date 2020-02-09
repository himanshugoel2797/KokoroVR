using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace VoxelWaveSurfing
{
    public class DirectBitmap : IDisposable
    {
        public Bitmap Bitmap { get; private set; }
        public Int32[] Bits { get; private set; }
        public bool Disposed { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        protected GCHandle BitsHandle { get; private set; }
        private IntPtr BitPtr;
        private IntPtr DepthBuf;

        public DirectBitmap(int width, int height)
        {
            Width = width;
            Height = height;
            Bits = new Int32[width * height];
            BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            BitPtr = BitsHandle.AddrOfPinnedObject();
            Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());

            DepthBuf = Marshal.AllocHGlobal(width * height * sizeof(float));
        }

        public void Clear(Color col)
        {
            int c = col.ToArgb();
            unsafe
            {
                int* Bits = (int*)BitPtr;
                float* dpth = (float*)DepthBuf;
                for (int i = 0; i < Width * Height; i++)
                {
                    Bits[i] = c;
                    dpth[i] = 0.0f;
                }
            }
        }
        public void SetPixel(int x, int y, int col)
        {
            unsafe
            {
                int* Bits = (int*)BitPtr;
                int index = x + (y * Width);

                Bits[index] = col;
            }
        }

        public void SetPixel(int x, int y, Color colour)
        {
            int index = x + (y * Width);
            int col = colour.ToArgb();

            Bits[index] = col;
        }

        public Color GetPixel(int x, int y)
        {
            int index = x + (y * Width);
            int col = Bits[index];
            Color result = Color.FromArgb(col);

            return result;
        }

        public void DrawLineLow(float x0, float y0, float z0, float x1, float y1, float z1, Color col)
        {
            unsafe
            {
                int* Bits = (int*)BitPtr;
                float* Dpth = (float*)DepthBuf;
                float dx = x1 - x0;
                float dy = y1 - y0;
                //Clip bounds
                if (x0 < 0)
                {
                    y0 = (int)Math.Round(y0 + dy / dx * -x0);
                    x0 = 0;
                }
                if (x1 >= Width)
                {
                    y1 = (int)Math.Round(y1 + dy / dx * (x1 - (Width - 1)));
                    x1 = Width - 1;
                }
                if (y0 < 0)
                {
                    x0 = (int)Math.Round(x0 + dx / dy * -y0);
                    y0 = 0;
                }
                if (y1 >= Height)
                {
                    x1 = (int)Math.Round(x1 + dx / dy * (y1 - (Height - 1)));
                    y1 = Height - 1;
                }

                float yi = 1;
                if (dy < 0)
                {
                    yi = -1;
                    dy = -dy;
                }
                float D = 2 * dy - dx;
                float y = y0;
                float x_sgn = Math.Sign(dx);

                int c = col.ToArgb();
                Vector2 orig = new Vector2(x0, y0);
                Vector2 end = new Vector2(x1, y1);
                float len = (end - orig).LengthSquared;
                for (float x = x0; x <= x1; x += x_sgn)
                {
                    int rnd_x = (int)Math.Round(x);
                    int rnd_y = (int)Math.Round(y);
                    if (rnd_x < Width && rnd_x >= 0 && rnd_y < Height && rnd_y >= 0)
                    {
                        Vector2 curPos = new Vector2(x, y);
                        float n_d_interp = (curPos - orig).LengthSquared / len;
                        float n_d = n_d_interp * z0 + (1 - n_d_interp) * z1;

                        int idx = rnd_x + (rnd_y * Width);
                        if (Dpth[idx] <= n_d)
                        {
                            Dpth[idx] = n_d;
                            Bits[idx] = c;
                        }
                    }
                    if (D > 0)
                    {
                        if (yi > 0 && y >= y1) break;
                        if (yi < 0 && y <= y1) break;
                        y += yi;
                        D -= 2 * dx;
                    }
                    D += 2 * dy;
                }
            }
        }

        public void DrawLineHigh(float x0, float y0, float z0, float x1, float y1, float z1, Color col)
        {
            unsafe
            {
                int* Bits = (int*)BitPtr;
                float* Dpth = (float*)DepthBuf;
                float dx = x1 - x0;
                float dy = y1 - y0;
                //Clip bounds
                if (x0 < 0)
                {
                    y0 = (int)Math.Round(y0 + dy / dx * -x0);
                    x0 = 0;
                }
                if (x1 >= Width)
                {
                    y1 = (int)Math.Round(y1 + dy / dx * (x1 - (Width - 1)));
                    x1 = Width - 1;
                }
                if (y0 < 0)
                {
                    x0 = (int)Math.Round(x0 + dx / dy * -y0);
                    y0 = 0;
                }
                if (y1 >= Height)
                {
                    x1 = (int)Math.Round(x1 + dx / dy * (y1 - (Height - 1)));
                    y1 = Height - 1;
                }

                float xi = 1;
                if (dx < 0)
                {
                    xi = -1;
                    dx = -dx;
                }
                float D = 2 * dx - dy;
                float x = x0;
                float y_sgn = Math.Sign(dy);


                int c = col.ToArgb();
                Vector2 orig = new Vector2(x0, y0);
                Vector2 end = new Vector2(x1, y1);
                float len = (end - orig).LengthSquared;
                for (float y = y0; y <= y1; y += y_sgn)
                {
                    int rnd_x = (int)Math.Round(x);
                    int rnd_y = (int)Math.Round(y);
                    if (rnd_x < Width && rnd_x >= 0 && rnd_y < Height && rnd_y >= 0)
                    {
                        Vector2 curPos = new Vector2(x, y);
                        float n_d_interp = (curPos - orig).LengthSquared / len;
                        float n_d = n_d_interp * z0 + (1 - n_d_interp) * z1;

                        int idx = rnd_x + (rnd_y * Width);
                        if (Dpth[idx] <= n_d)
                        {
                            Dpth[idx] = n_d;
                            Bits[idx] = c;
                        }
                    }
                    if (D > 0)
                    {
                        if (xi > 0 && x >= x1) break;
                        if (xi < 0 && x <= x1) break;
                        x += xi;
                        D -= 2 * dy;
                    }
                    D += 2 * dx;
                }
            }
        }


        public void DrawLine(float x0, float y0, float z0, float x1, float y1, float z1, Color col)
        {
            if (Math.Abs(y1 - y0) < Math.Abs(x1 - x0))
            {
                if (x0 > x1)
                    DrawLineLow(x1, y1, z1, x0, y0, z0, col);
                else
                    DrawLineLow(x0, y0, z0, x1, y1, z1, col);
            }
            else
            {
                if (y0 > y1)
                    DrawLineHigh(x1, y1, z1, x0, y0, z0, col);
                else
                    DrawLineHigh(x0, y0, z0, x1, y1, z1, col);
            }
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Bitmap.Dispose();
            BitsHandle.Free();
        }
    }

    public class WaveSurfer
    {
        public DirectBitmap Image { get; private set; }
        public Vector3 Position { get; set; }
        public Vector3 Direction { get; set; }
        public Vector3 Up { get; set; }

        private VoxelData data;
        private int fov = 90, w, h;

        struct VisibleSpan
        {
            public float Start;
            public float Stop;
            public float X;
            public float StopX;
            public float Y;
            public float ZStart;
            public float ZStop;
        }

        public WaveSurfer(int w, int h, VoxelData data)
        {
            Image = new DirectBitmap(w, h);
            this.data = data;
            this.w = w;
            this.h = h;

            Position = new Vector3(1024 / VoxelData.Scale, 1024 / VoxelData.Scale, 90);
            Direction = new Vector3(1, 0, 0);
            Up = new Vector3(0, 0, 1);
        }

        public void Draw()
        {
            Image.Clear(Color.Gray);

            float avgSteps = 0;
            float height_mult = (float)Math.Tan(MathHelper.DegreesToRadians(fov * 0.5f));
            //int col = Color.Black.ToArgb();

            unsafe
            {
                Matrix4 projMat = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(fov), w / (float)h, 0.001f);
                Matrix4 viewMat = Matrix4.LookAt(Position, Position + Direction, Up);
                var vp = viewMat * projMat;
                var ivp = Matrix4.Invert(vp);

                //Compute the corners of the frustum
                //Determine AABB and adjust pixel range

                //Compute world position for each vertical 'ray'
                for (int ix = -w * 2; ix < w * 2; ix++)
                {
                    int raySteps = 0;
                    float fx = ix / (w * 0.5f);

                    Vector3 rayDir = new Vector3(fx, 0, 0.001f);
                    var rayDir_4 = Vector4.Transform(new Vector4(rayDir, 1), ivp);
                    rayDir_4 /= rayDir_4.W;
                    rayDir = rayDir_4.Xyz - Position;

                    VoxelSpan[] spans = null;
                    float z = 1f;
                    rayDir *= z;

                    Vector3 rayPos = rayDir + Position;
                    raySteps++;

                    do
                    {
                        float col_h = (raySteps * z * height_mult);
                        col_h = col_h / Vector3.Dot(Vector3.UnitZ, Up);
                        var topPos = rayPos + Up * col_h;
                        var btmPos = rayPos - Up * col_h;

                        spans = data.Get(rayPos.X, rayPos.Y);
                        if (spans != null)
                        {
                            if ((btmPos.Z >= spans[spans.Length - 1].Stop) | (topPos.Z < spans[0].Start))
                            {
                                rayPos += rayDir;
                                raySteps++;
                                continue;
                            }

                            //For now trace columns with brute force samples, later use a proper line drawing algorithm instead
                            //Apply this span from top to bottom with depth test
                            //Check if the Z-coordinate falls within a span
                            //Determine visible bounds for all spans
                            for (int j = 0; j < spans.Length; j++)
                                if (topPos.Z >= spans[j].Start && btmPos.Z < spans[j].Stop)
                                {
                                    Vector4 stStart = new Vector4(rayPos.X, rayPos.Y, Math.Min(topPos.Z, spans[j].Start), 1);
                                    Vector4 stStop = new Vector4(rayPos.X, rayPos.Y, Math.Max(btmPos.Z, spans[j].Stop), 1);

                                    var stStart_ = Vector4.Transform(stStart, vp);
                                    var stStop_ = Vector4.Transform(stStop, vp);

                                    var stStart_w = stStart_ / stStart_.W;
                                    var stStop_w = stStop_ / stStop_.W;

                                    var startX = (stStart_w.X * w / 2 + w / 2);
                                    var startY = h - (stStart_w.Y * h / 2 + h / 2);

                                    var stopX = (stStop_w.X * w / 2 + w / 2);
                                    var stopY = h - (stStop_w.Y * h / 2 + h / 2);

                                    float dist = (float)Math.Sqrt((rayPos.X - Position.X) * (rayPos.X - Position.X) + (rayPos.Y - Position.Y) * (rayPos.Y - Position.Y));
                                    int iDist = (int)dist % 256;
                                    var col = Color.FromArgb(iDist, iDist, iDist);

                                    bool lineVis = true;
                                    if (startX < 0 && stopX < 0)
                                        lineVis = false;
                                    if (startX >= w && stopX >= w)
                                        lineVis = false;
                                    if (startY < 0 && stopY < 0)
                                        lineVis = false;
                                    if (startY >= h && stopY >= h)
                                        lineVis = false;

                                    if (lineVis)
                                        Image.DrawLine(startX, startY, stStart_w.Z, stopX, stopY, stStop_w.Z, col);
                                }
                        }
                        rayPos += rayDir;
                        raySteps++;
                    } while (rayPos.X < VoxelData.WorldSide && rayPos.Y < VoxelData.WorldSide && rayPos.X >= 0 && rayPos.Y >= 0);

                    avgSteps += raySteps;
                }
            }

            Console.WriteLine("Average Steps: " + avgSteps / w);
        }
    }
}
