using Kokoro.Math;
using SharpNoise.Modules;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Collections;

namespace KokoroVR2.PlanetGen
{
    class Program
    {
        struct Particle
        {
            public float sedimentAmount;
            public Vector2 vel;
            public float xPos;
            public float yPos;
            public float capacity;
        }

        [ThreadStatic]
        static Random rng;

        const int ImgSide = (1 << 9) + 1;
        const int ImgMult = 1;
        const int TileSide = ImgSide * ImgMult;
        const int EquiRectSide = 1024;
        static void Main(string[] args)
        {
            float radius = 6000;
            double scale = 0.0002;
            double scale2 = 0.0002;

            var tiles = new float[6][];
            var tile_hardness = new float[6][];
            var tile_bedrock = new float[6][];
            var tile_bmps = new Bitmap[6];
            var equirect = new Bitmap(EquiRectSide * 2, EquiRectSide);

            var particleCnt = 200000;


            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = new float[TileSide * TileSide];
                tile_hardness[i] = new float[TileSide * TileSide];
                tile_bedrock[i] = new float[TileSide * TileSide];
                tile_bmps[i] = new Bitmap(ImgSide, ImgSide);
            }

            //Create continents
            var normals = new (Vector3, int, int, int)[]
            {
                (new Vector3(0, 1, 0), 0, 2, 1),
                (new Vector3(0, -1, 0), 0, 2, 1),
                (new Vector3(0, 0, 1), 0, 1, 2),
                (new Vector3(0, 0, -1), 0, 1, 2),
                (new Vector3(1, 0, 0), 1, 2, 0),
                (new Vector3(-1, 0, 0), 1, 2, 0),
            };

            Simplex simplex = new Simplex();
            RidgedMulti ridgedMulti = new RidgedMulti();
            ridgedMulti.Seed = 10;

            RidgedMulti ridgedMulti2 = new RidgedMulti();

            //for (int face = 0; face < tiles.Length; face++)
            Parallel.For(0, tiles.Length, (face) =>
            {
                for (int y = 0; y < TileSide; y++)
                    for (int x = 0; x < TileSide; x++)
                    {
                        var (norm, x_idx, z_idx, y_idx) = normals[face];
                        var face_pos = new Vector2(x / (float)(TileSide) - 0.5f, y / (float)TileSide - 0.5f);
                        face_pos *= radius * 2;

                        var pos_f = new float[3];

                        pos_f[x_idx] = face_pos.X;
                        pos_f[z_idx] = face_pos.Y;
                        var pos = new Vector3(pos_f);
                        pos += norm * radius;
                        pos.Normalize();
                        var sphere_pos = Vector3.ToSpherical(pos);
                        pos = pos * radius;

                        //Sample terrain noise
                        var density = Math.Abs(simplex.GetValue(pos.X * scale, pos.Y * scale, pos.Z * scale) * 0.5f + 0.5f);
                        var density0 = Math.Abs(ridgedMulti.GetValue(pos.X * scale2 * 100, pos.Y * scale2 * 100, pos.Z * scale2 * 100) * 0.5f + 0.5f);

                        var bedrock_height = Math.Abs(simplex.GetValue(pos.X * scale2, pos.Y * scale2, pos.Z * scale2) * 0.5f + 0.5f);

                        var v = Math.Abs(ridgedMulti2.GetValue(pos.X * scale2, pos.Y * scale2, pos.Z * scale2));// * 0.5f + 0.5f;
                        v += density;

                        tiles[face][y * TileSide + x] = (float)v;// (ushort)(v * 4096);
                        tile_hardness[face][y * TileSide + x] = (float)density0;
                        tile_bedrock[face][y * TileSide + x] = (float)bedrock_height;
                    }

                var tile_max = tiles[face].Max();
                var tile_min = tiles[face].Min();

                for (int y = 0; y < TileSide; y++)
                    for (int x = 0; x < TileSide; x++)
                    {
                        var v = (tiles[face][y * TileSide + x] - tile_min) / (tile_max - tile_min);
                        var c = Math.Max(Math.Min((int)(v * 255), 255), 0);
                        var color = Color.FromArgb(c, c, c);
                        tile_bmps[face].SetPixel(x / ImgMult, y / ImgMult, color);
                    }
                tile_bmps[face].Save($"face_{face}.png");
                using (FileStream fs = File.Open($"face_{face}.bin", FileMode.Create))
                using (BinaryWriter w = new BinaryWriter(fs))
                    foreach (double v in tiles[face])
                        w.Write((short)(v) * short.MaxValue / 2);
            }
            );

            equirect.Save("equirect.png");

            Console.WriteLine("Starting Erosion...");

            //int face = 0;
            var rng_base = new Random(0);
            //for (int face = 0; face < 6; face++)
            Parallel.For(0, tiles.Length, (face) =>
            {
                unsafe
                {
                    var particles = new Particle();
                    var loc = rng;
                    if (loc == null)
                    {
                        int seed;
                        lock (rng_base) seed = rng_base.Next();
                        loc = rng = new Random(seed);
                    }
                    for (int i = 0; i < particleCnt; i++)
                    {
                        particles.sedimentAmount = 0;
                        particles.vel = new Vector2((float)loc.NextDouble(), (float)loc.NextDouble());
                        particles.xPos = (float)(loc.NextDouble() * TileSide);
                        particles.yPos = (float)(loc.NextDouble() * TileSide);
                        particles.capacity = (float)(0.9f * loc.NextDouble());

                        while (particles.xPos < 0 || particles.xPos >= TileSide - 1.5f)
                            particles.xPos = (float)loc.NextDouble() * TileSide;

                        while (particles.yPos < 0 || particles.yPos >= TileSide - 1.5f)
                            particles.yPos = (float)loc.NextDouble() * TileSide;

                        float prev_mag = float.Epsilon;
                        int j;
                        int sample_cnt = 1000;
                        float top, lft, crn, cur;
                        for (j = 0; j < sample_cnt; j++)
                        {

                            cur = tiles[face][(int)particles.yPos * TileSide + (int)particles.xPos];
                            top = tiles[face][(int)particles.yPos * TileSide + (int)particles.xPos + 1];
                            lft = tiles[face][(int)(particles.yPos + 1) * TileSide + (int)particles.xPos];
                            crn = tiles[face][(int)(particles.yPos + 1) * TileSide + (int)particles.xPos + 1];

                            var bedrock = tile_bedrock[face][(int)particles.yPos * TileSide + (int)particles.xPos];
                            bool inBedrock = false;
                            if (bedrock >= cur)
                            {
                                cur = bedrock;
                                inBedrock = true;
                                tiles[face][(int)particles.yPos * TileSide + (int)particles.xPos] = bedrock;
                            }
                            var velx = (cur + lft - top - crn);
                            var vely = (cur + top - lft - crn);

                            var inertia = 1 - particles.sedimentAmount / (particles.capacity + 0.3);
                            particles.vel.X = (float)(particles.vel.X * (1 - inertia) + velx * inertia);
                            particles.vel.Y = (float)(particles.vel.Y * (1 - inertia) + vely * inertia);

                            if (particles.vel.Length < 1e-20)
                            {
                                particles.vel = new Vector2((float)loc.NextDouble(), (float)loc.NextDouble());
                                particles.vel.Normalize();
                                //particles.sedimentAmount = 0;
                                //break;
                            }

                            //particles.vel.Normalize();
                            float vel_mag = particles.vel.Length;
                            var vel_n = Vector2.Normalize(particles.vel);
                            var xpos = particles.xPos + vel_n.X;
                            var ypos = particles.yPos + vel_n.Y;

                            //if (!inBedrock)
                            {
                                //compute sediment amount
                                var hmin = Math.Min(cur, Math.Min(top, Math.Min(lft, crn)));
                                var hmax = Math.Max(cur, Math.Max(top, Math.Max(lft, crn)));
                                if ((int)ypos >= 0 && (int)ypos < TileSide && (int)xpos >= 0 && (int)xpos < TileSide)
                                {
                                    hmin = Math.Min(hmin, tiles[face][(int)ypos * TileSide + (int)xpos]);
                                    hmax = Math.Max(hmax, tiles[face][(int)ypos * TileSide + (int)xpos]);
                                }
                                var maxcap = hmax - hmin;

                                var erosion_mult = (loc.NextDouble() < tile_hardness[face][(int)particles.yPos * TileSide + (int)particles.xPos]) ? 0.5f : 1f;
                                erosion_mult = inBedrock ? 0.1f : erosion_mult;

                                var carried_amt = (vel_mag * erosion_mult);
                                carried_amt = MathF.Min(carried_amt, maxcap);
                                carried_amt = MathF.Min(carried_amt, cur);
                                //carried_amt = Math.Min(carried_amt, Math.Abs(top - cur));
                                //carried_amt = Math.Min(carried_amt, Math.Abs(lft - cur));
                                //carried_amt = Math.Min(carried_amt, Math.Abs(crn - cur));
                                carried_amt = MathF.Min(carried_amt, 0.0001f);

                                if (particles.sedimentAmount + carried_amt <= particles.capacity)
                                {
                                    particles.sedimentAmount += carried_amt;
                                    tiles[face][(int)particles.yPos * TileSide + (int)particles.xPos] -= carried_amt;
                                }
                                else
                                {
                                    var amnt = MathF.Min(particles.sedimentAmount * 0.1f, carried_amt);
                                    tiles[face][(int)particles.yPos * TileSide + (int)particles.xPos] += amnt;
                                    particles.sedimentAmount -= amnt;
                                }
                            }

                            particles.xPos = xpos;
                            particles.yPos = ypos;

                            if (particles.xPos < 0 || particles.xPos >= TileSide - 1.5f)
                            {
                                particles.sedimentAmount = 0;
                                break;
                            }

                            if (particles.yPos < 0 || particles.yPos >= TileSide - 1.5f)
                            {
                                particles.sedimentAmount = 0;
                                break;
                            }

                            prev_mag = vel_mag;
                        }
                        if (j >= sample_cnt)
                        {
                            tiles[face][(int)particles.yPos * TileSide + (int)particles.xPos] += particles.sedimentAmount;
                            particles.sedimentAmount = 0;

                            int cx = (int)particles.xPos;
                            int cy = (int)particles.yPos;
                            var filter = new double[][] {
                            new double[] { 16.0f, 8.0f, 16.0f },
                            new double[] { 8.0f, 4.0f, 8.0f },
                            new double[] { 16.0f, 8.0f, 16.0f },
                        };

                            int filter_range = 1;
                            var samples = new float[9];
                            for (int box = -filter_range; box <= filter_range; box++)
                                for (int boy = -filter_range; boy <= filter_range; boy++)
                                {
                                    int sx = box + cx;
                                    int sy = boy + cy;

                                    if (sx >= 1 && sx < TileSide - 1 && sy >= 1 && sy < TileSide - 1)
                                    {
                                        float sum = 0;
                                        for (int ox = -1; ox <= 1; ox++)
                                            for (int oy = -1; oy <= 1; oy++)
                                            {
                                                int x = ox + sx;
                                                int y = oy + sy;
                                                if (x >= 0 && x < TileSide && y >= 0 && y < TileSide)
                                                {
                                                    samples[(oy + 1) * 3 + (ox + 1)] = tiles[face][y * TileSide + x];
                                                    //sum += 1.0 / filter[oy + 1][ox + 1] * tiles[face][y * TileSide + x];
                                                }
                                            }
                                        //tiles[face][sy * TileSide + sx] = sum;
                                        tiles[face][sy * TileSide + sx] = samples.OrderBy(a => a).ElementAt(5);
                                    }
                                }
                        }
                    }

                    //for (int cx = 0; cx < TileSide; cx++)
                    //    for (int cy = 0; cy < TileSide; cy++)
                    {
                        //tiles[face][(cy + 1) * TileSide + cx] += particles.sedimentAmount;
                        //tiles[face][(int)(particles.yPos - 1) * TileSide + (int)particles.xPos] += particles.sedimentAmount;
                    }
                    //Scatter meteors - particle simulation for impact handling
                    //Scatter glaciers - used to seed rivers

                    var tile_max = tiles[face].Max();
                    var tile_min = tiles[face].Min();

                    for (int y = 0; y < TileSide; y++)
                        for (int x = 0; x < TileSide; x++)
                        {
                            var v = (tiles[face][y * TileSide + x] - tile_min) / (tile_max - tile_min);
                            var c = Math.Max(Math.Min((int)(v * 255), 255), 0);
                            var color = Color.FromArgb(c, c, c);
                            tile_bmps[face].SetPixel(x / ImgMult, y / ImgMult, color);
                        }
                    tile_bmps[face].Save($"face_eroded_{face}.png");
                    using (FileStream fs = File.Open($"face_eroded_{face}.bin", FileMode.Create))
                    using (BinaryWriter w = new BinaryWriter(fs))
                        foreach (double v in tiles[face])
                            w.Write((short)(v) * short.MaxValue / 2);
                }
            });
        }

        static Vector2 getGradient(ushort[] tile, ref Particle particle)
        {
            float velx = -tile[(int)particle.yPos * TileSide + (int)particle.xPos + 1] + tile[(int)particle.yPos * TileSide + (int)particle.xPos - 1];
            float vely = -tile[(int)(particle.yPos + 1) * TileSide + (int)particle.xPos] + tile[(int)(particle.yPos - 1) * TileSide + (int)particle.xPos];

            velx /= 2;
            vely /= 2;

            return new Vector2(velx, vely);
        }

        static void erode()
        {

        }

        static void deposit()
        {

        }

        static void depositAll()
        {

        }
    }
}
