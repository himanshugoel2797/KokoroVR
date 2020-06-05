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

        class HeightMap
        {
            public float[] tile;
            public float[] hardness;
            public float[] water_level;
            public float[] sediment_amnt;
            public int Side;
            public object locker;
        }

        [ThreadStatic]
        static Random rng;

        const int ImgSide = (1 << 8);
        const int ImgMult = 1;
        const int TileSide = ImgSide * ImgMult;
        const int EquiRectSide = 1024;
        const int FaceCount = 6;
        static void Main(string[] args)
        {
            float radius = 6000;
            double scale = 0.0002;
            double scale2 = 0.0002;

            var tile_water_bmps = new Bitmap[FaceCount];
            var tile_bmps = new Bitmap[FaceCount];
            var equirect = new Bitmap(EquiRectSide * 2, EquiRectSide);
            var heightmaps = new HeightMap[FaceCount];
            var dst_heightmaps = new HeightMap[FaceCount];

            for (int i = 0; i < FaceCount; i++)
            {
                heightmaps[i] = new HeightMap()
                {
                    Side = TileSide,
                    hardness = new float[TileSide * TileSide],
                    sediment_amnt = new float[TileSide * TileSide],
                    tile = new float[TileSide * TileSide],
                    water_level = new float[TileSide * TileSide],
                    locker = new object(),
                };

                dst_heightmaps[i] = new HeightMap()
                {
                    Side = TileSide,
                    hardness = new float[TileSide * TileSide],
                    sediment_amnt = new float[TileSide * TileSide],
                    tile = new float[TileSide * TileSide],
                    water_level = new float[TileSide * TileSide],
                    locker = new object()
                };

                tile_bmps[i] = new Bitmap(ImgSide, ImgSide);
                tile_water_bmps[i] = new Bitmap(ImgSide, ImgSide);
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
            Parallel.For(0, FaceCount, (face) =>
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

                        var seed_water_lvl = Math.Abs(simplex.GetValue(pos.X * scale2, pos.Y * scale2, pos.Z * scale2) * 0.5f + 0.5f) * 0.01f;

                        var v = Math.Abs(ridgedMulti2.GetValue(pos.X * scale2, pos.Y * scale2, pos.Z * scale2));// * 0.5f + 0.5f;
                        v += density;

                        heightmaps[face].tile[y * TileSide + x] = (float)v;// (ushort)(v * 4096);
                        heightmaps[face].hardness[y * TileSide + x] = (float)density0;
                        heightmaps[face].water_level[y * TileSide + x] = (float)seed_water_lvl;

                        dst_heightmaps[face].tile[y * TileSide + x] = (float)v;// (ushort)(v * 4096);
                        dst_heightmaps[face].hardness[y * TileSide + x] = (float)density0;
                        dst_heightmaps[face].water_level[y * TileSide + x] = (float)seed_water_lvl;
                    }

                var tile_max = heightmaps[face].tile.Max();
                var tile_min = heightmaps[face].tile.Min();
                var water_max = heightmaps[face].water_level.Max();
                var water_min = heightmaps[face].water_level.Min();

                for (int y = 0; y < TileSide; y++)
                    for (int x = 0; x < TileSide; x++)
                    {
                        var v = (heightmaps[face].tile[y * TileSide + x] - tile_min) / (tile_max - tile_min);
                        var w = (heightmaps[face].water_level[y * TileSide + x] - water_min) / (water_max - water_min);
                        var c = Math.Max(Math.Min((int)(v * 255), 255), 0);
                        var c_w = Math.Max(Math.Min((int)(w * 255), 255), 0);
                        var color = Color.FromArgb(c, c_w, c);
                        tile_bmps[face].SetPixel(x / ImgMult, y / ImgMult, color);
                    }
                tile_bmps[face].Save($"face_{face}.png");
                using (FileStream fs = File.Open($"face_{face}.bin", FileMode.Create))
                using (BinaryWriter w = new BinaryWriter(fs))
                    foreach (double v in heightmaps[face].tile)
                        w.Write((short)(v) * short.MaxValue / 2);
            }
            );

            equirect.Save("equirect.png");

            Console.WriteLine("Starting Erosion...");

            for (int face = 0; face < FaceCount; face++)
            {
                var src = heightmaps[face];
                var dst = dst_heightmaps[face];
                for (int i = 0; i < 512; i++)
                {
                    water_iteration(src, dst);

                    var tmp = dst;
                    dst = src;
                    src = tmp;
                }

                var tile_max = heightmaps[face].tile.Max();
                var tile_min = heightmaps[face].tile.Min();
                var water_max = heightmaps[face].water_level.Max();
                var water_min = heightmaps[face].water_level.Min();

                for (int y = 0; y < TileSide; y++)
                    for (int x = 0; x < TileSide; x++)
                    {
                        var v = (src.tile[y * TileSide + x] - water_min - tile_min) / (tile_max + water_max - tile_min - water_min);
                        var w = (src.tile[y * TileSide + x] + src.water_level[y * TileSide + x] - water_min - tile_min) / (water_max + tile_max - water_min - tile_min);
                        var c = Math.Max(Math.Min((int)(v * 255), 255), 0);
                        var c_w = Math.Max(Math.Min((int)(w * 255), 255), 0);
                        var color = Color.FromArgb(c, c_w, c);
                        tile_water_bmps[face].SetPixel(x / ImgMult, y / ImgMult, color);
                    }
                tile_water_bmps[face].Save($"face_water_{face}.png");
                Console.WriteLine($"Face {face} Done.");
            }
        }

        static void water_iteration(HeightMap src, HeightMap dst)
        {
            Random rng_base = new Random(0);
            //Parallel.For(1, src.Side - 1, (y) =>
            Action<int> line_sim = (y) =>
            {

                var hardness_tile = new float[9];
                var sediment_tile = new float[9];
                var tile_tile = new float[9];
                var water_lvl_tile = new float[9];

                var loc = rng;
                if (loc == null)
                {
                    int seed;
                    lock (rng_base) seed = rng_base.Next();
                    loc = rng = new Random(seed);
                }

                for (int x = 0; x < src.Side; x++)
                {

                    //read the moore neighborhood
                    float lowestHeight = float.MaxValue;
                    int lowestHeight_idx = -1;

                    float highestHeight = float.MinValue;
                    int highestHeight_idx = -1;
                    {
                        int q = 0;
                        for (int sy = -1; sy < 2; sy++)
                            for (int sx = -1; sx < 2; sx++)
                            {
                                if (sy + y >= 0 && sy + y < TileSide)
                                    if (sx + x >= 0 && sx + x < TileSide)
                                    {
                                        hardness_tile[q] = src.hardness[(y + sy) * src.Side + (x + sx)];
                                        sediment_tile[q] = src.sediment_amnt[(y + sy) * src.Side + (x + sx)];
                                        tile_tile[q] = src.tile[(y + sy) * src.Side + (x + sx)];
                                        water_lvl_tile[q] = src.water_level[(y + sy) * src.Side + (x + sx)];

                                        //find lowest height cell
                                        if (tile_tile[q] + water_lvl_tile[q] < lowestHeight)
                                        {
                                            lowestHeight = tile_tile[q] + water_lvl_tile[q];
                                            lowestHeight_idx = q;
                                        }

                                        //find highest height cell
                                        if (tile_tile[q] + water_lvl_tile[q] > highestHeight)
                                        {
                                            highestHeight = tile_tile[q] + water_lvl_tile[q];
                                            highestHeight_idx = q;
                                        }
                                    }
                                q++;
                            }
                    }

                    //compute water flow
                    var water_flow = ((tile_tile[4] + water_lvl_tile[4]) - (tile_tile[lowestHeight_idx] + water_lvl_tile[lowestHeight_idx])) * 0.5f;

                    //update water level
                    if (water_flow < water_lvl_tile[4])
                    {
                        water_lvl_tile[4] -= water_flow;
                        water_lvl_tile[lowestHeight_idx] += water_flow;
                    }
                    else
                    {
                        var cur_lvl = water_lvl_tile[4];
                        water_lvl_tile[4] = 0;
                        water_lvl_tile[lowestHeight_idx] += cur_lvl;
                    }

                    //write the dst buffer
                    {
                        int q = 0;
                        for (int sy = -1; sy < 2; sy++)
                            for (int sx = -1; sx < 2; sx++)
                            {
                                if (sy + y >= 0 && sy + y < TileSide)
                                    if (sx + x >= 0 && sx + x < TileSide)
                                    {
                                        dst.hardness[(y + sy) * dst.Side + (x + sx)] = hardness_tile[q];
                                        if (q == 4) dst.tile[(y + sy) * dst.Side + (x + sx)] = tile_tile[q];
                                        dst.sediment_amnt[(y + sy) * dst.Side + (x + sx)] = sediment_tile[q];
                                        dst.water_level[(y + sy) * dst.Side + (x + sx)] = water_lvl_tile[q];
                                    }
                                q++;
                            }
                    }
                }
            };

            var particleCnt = 100;
            Parallel.For(0, FaceCount, (face) =>
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
                    particles.capacity = (float)(0.9 * loc.NextDouble());

                    while (true)
                    {
                        if (particles.xPos >= 0 && particles.xPos < TileSide - 1.5f && particles.yPos >= 0 && particles.yPos < TileSide - 1.5f && src.water_level[(int)particles.yPos * TileSide + (int)particles.xPos] != 0) break;

                        particles.xPos = (float)loc.NextDouble() * TileSide;
                        particles.yPos = (float)loc.NextDouble() * TileSide;
                    }

                    float prev_mag = float.Epsilon;
                    int j;
                    int sample_cnt = 100;
                    float top, lft, crn, cur;
                    for (j = 0; j < sample_cnt; j++)
                    {

                        cur = src.tile[(int)particles.yPos * TileSide + (int)particles.xPos];
                        top = src.tile[(int)particles.yPos * TileSide + (int)particles.xPos + 1];
                        lft = src.tile[(int)(particles.yPos + 1) * TileSide + (int)particles.xPos];
                        crn = src.tile[(int)(particles.yPos + 1) * TileSide + (int)particles.xPos + 1];

                        var velx = (cur + lft - top - crn);
                        var vely = (cur + top - lft - crn);

                        var inertia = 1 - particles.sedimentAmount / (particles.capacity + 0.3);
                        particles.vel.X = (float)(particles.vel.X * (1 - inertia) + velx * inertia);
                        particles.vel.Y = (float)(particles.vel.Y * (1 - inertia) + vely * inertia);

                        if (particles.vel.Length < 1e-20)
                        {
                            particles.vel = new Vector2((float)loc.NextDouble(), (float)loc.NextDouble());
                            particles.vel.Normalize();
                        }

                        float vel_mag = particles.vel.Length;
                        var vel_n = Vector2.Normalize(particles.vel);
                        var xpos = particles.xPos + vel_n.X;
                        var ypos = particles.yPos + vel_n.Y;

                        //compute sediment amount
                        var hmin = Math.Min(cur, Math.Min(top, Math.Min(lft, crn)));
                        var hmax = Math.Max(cur, Math.Max(top, Math.Max(lft, crn)));
                        if ((int)ypos >= 0 && (int)ypos < TileSide && (int)xpos >= 0 && (int)xpos < TileSide)
                        {
                            hmin = Math.Min(hmin, src.tile[(int)ypos * TileSide + (int)xpos]);
                            hmax = Math.Max(hmax, src.tile[(int)ypos * TileSide + (int)xpos]);
                        }
                        var maxcap = hmax - hmin;

                        var erosion_mult = (loc.NextDouble() < src.hardness[(int)particles.yPos * TileSide + (int)particles.xPos]) ? 0.5f : 1f;

                        var carried_amt = (vel_mag * erosion_mult);
                        carried_amt = Math.Min(carried_amt, maxcap);
                        carried_amt = Math.Min(carried_amt, cur);
                        carried_amt = Math.Min(carried_amt, 0.0001f);

                        if (particles.sedimentAmount + carried_amt <= particles.capacity)
                        {
                            particles.sedimentAmount += carried_amt;
                            src.tile[(int)particles.yPos * TileSide + (int)particles.xPos] -= carried_amt;
                        }
                        else
                        {
                            var amnt = MathF.Min(particles.sedimentAmount * 0.1f, carried_amt);
                            src.tile[(int)particles.yPos * TileSide + (int)particles.xPos] += amnt;
                            particles.sedimentAmount -= amnt;
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
                        src.tile[(int)particles.yPos * TileSide + (int)particles.xPos] += particles.sedimentAmount;
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
                                    double sum = 0;
                                    for (int ox = -1; ox <= 1; ox++)
                                        for (int oy = -1; oy <= 1; oy++)
                                        {
                                            int x = ox + sx;
                                            int y = oy + sy;
                                            if (x >= 0 && x < TileSide && y >= 0 && y < TileSide)
                                            {
                                                samples[(oy + 1) * 3 + (ox + 1)] = dst.tile[y * TileSide + x];
                                                //sum += 1.0 / filter[oy + 1][ox + 1] * tiles[face][y * TileSide + x];
                                            }
                                        }
                                    //tiles[face][sy * TileSide + sx] = sum;
                                    src.tile[sy * TileSide + sx] = samples.OrderBy(a => a).ElementAt(5);
                                }
                            }
                    }
                }

                //for (int cx = 0; cx < TileSide; cx++)
                //    for (int cy = 0; cy < TileSide; cy++)
                {
                    //tiles[face][(cy + 1) * TileSide + cx] += particles[i].sedimentAmount;
                    //tiles[face][(int)(particles[i].yPos - 1) * TileSide + (int)particles[i].xPos] += particles[i].sedimentAmount;
                }
                //Scatter meteors - particle simulation for impact handling
                //Scatter glaciers - used to seed rivers
            });

            for (int y = 0; y < src.Side; y++)
                line_sim(y);

            //process every 3 columns in parallel, avoids overlaps
            //Parallel.For(0, src.Side / 3, (y) => line_sim(y * 3));
            //Parallel.For(0, src.Side / 3, (y) => line_sim(y * 3 + 1));
            //Parallel.For(0, src.Side / 3, (y) => line_sim(y * 3 + 2));
        }
    }
}
