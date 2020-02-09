using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VoxelWaveSurfing
{
    public partial class Form1 : Form
    {
        WaveSurfer2 surfer;
        VoxelData data;
        public Form1()
        {
            InitializeComponent();
            data = new VoxelData();
            surfer = new WaveSurfer2(1920 / 8, 1080 / 8, data);
            //512,512 - 19425
            //1024,1024 - 37262
            timer1.Enabled = true;
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            //e.Graphics.DrawImage(surfer.Image, Point.Empty);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Stopwatch s = Stopwatch.StartNew();
            surfer.Draw();
            s.Stop();
            pictureBox1.Image = surfer.Image.Bitmap;
            this.Text = "Time: " + s.Elapsed.TotalMilliseconds + "ms";
        }

        float ud_rot = 1.94604f;// MathHelper.PiOver2;
        float lr_rot = -MathHelper.Pi / 10.0f;
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            Vector3 right = Vector3.Cross(surfer.Up, surfer.Direction);

            bool update = false;
            float rotRate = 0.5f;
            float rate = 1.0f;
            if (e.KeyCode == Keys.R)
            {
                surfer.Position += surfer.Up * rate;
                update = true;
            }
            if (e.KeyCode == Keys.F)
            {
                surfer.Position -= surfer.Up * rate;
                update = true;
            }
            if (e.KeyCode == Keys.W)
            {
                surfer.Position += surfer.Direction * rate;
                update = true;
            }
            if (e.KeyCode == Keys.S)
            {
                surfer.Position -= surfer.Direction * rate;
                update = true;
            }
            if (e.KeyCode == Keys.A)
            {
                surfer.Position += right * rate;
                update = true;
            }
            if (e.KeyCode == Keys.D)
            {
                surfer.Position -= right * rate;
                update = true;
            }
            if (e.KeyCode == Keys.Q)
            {
                lr_rot -= MathHelper.DegreesToRadians(rotRate);
                update = true;
            }
            if (e.KeyCode == Keys.E)
            {
                lr_rot += MathHelper.DegreesToRadians(rotRate);
                update = true;
            }
            if (e.KeyCode == Keys.T)
            {
                ud_rot -= MathHelper.DegreesToRadians(rotRate);
                update = true;
            }
            if (e.KeyCode == Keys.G)
            {
                ud_rot += MathHelper.DegreesToRadians(rotRate);
                update = true;
            }
            if (update)
            {
                Matrix4 camRot = Matrix4.CreateRotationX(lr_rot) * Matrix4.CreateRotationY(ud_rot);

                Vector3 og_targ = new Vector3(0, 0, 1);
                Vector3 og_up = new Vector3(-1, 0, 0);

                surfer.Direction = Vector3.Transform(og_targ, camRot);
                surfer.Up = Vector3.Transform(og_up, camRot);

                /*Stopwatch s = Stopwatch.StartNew();
                surfer.Draw();
                s.Stop();
                pictureBox1.Image = surfer.Image.Bitmap;
                this.Text = "Time: " + s.Elapsed.TotalMilliseconds + "ms";*/
                Console.WriteLine(surfer.Position);
            }
        }
    }
}
