using Kokoro.Graphics;
using Kokoro.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KokoroVR.Graphics.Voxel
{
    public enum FaceIndex
    {
        Top = 1,
        Bottom,
        Left,
        Right,
        Front,
        Back,
    }

    struct Run
    {
        public byte Start;
        public byte Stop;
        public byte Value;
        public byte Visibility;
    }

    public class Chunk
    {
        private const byte VisibleBit = 1;

        private object data_locker;

        internal int id;
        internal ChunkStreamer streamer;
        internal bool dirty, update_pending, empty;
        internal byte DefaultEdgeVisibility = 0;

        internal byte[] data;
        internal byte[] vis;
        internal Run[][] runs;

        internal byte[] faces;
        internal uint[] indices;
        internal Vector4[] bounds;
        internal byte[] norm_mask;

        public int VoxelCount { get; private set; }
        public ChunkObject Owner { get; internal set; }

        internal Chunk(ChunkStreamer streamer, int id)
        {
            data_locker = new object();
            data = new byte[ChunkConstants.Side * ChunkConstants.Side * ChunkConstants.Side];
            vis = new byte[ChunkConstants.Side * ChunkConstants.Side * ChunkConstants.Side];
            runs = new Run[ChunkConstants.Side * ChunkConstants.Side][];
            this.streamer = streamer;
            this.id = id;
            empty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetIndex(int x, int y, int z)
        {
            return (x & (ChunkConstants.Side - 1)) << (2 * ChunkConstants.SideLog) | (y & (ChunkConstants.Side - 1)) << ChunkConstants.SideLog | (z & (ChunkConstants.Side - 1));
        }

        private void FloodFillVisibility(byte[] vis, int x, int y, int z)
        {
            if (data[GetIndex(x, y, z)] != 0) return;

            vis[GetIndex(x, y, z)] = VisibleBit;
            Queue<(int, int, int)> nodes = new Queue<(int, int, int)>();
            nodes.Enqueue((x, y, z));

            int x_b = x;
            int y_b = y;
            int z_b = z;

            while (nodes.Count > 0)
            {
                (x, y, z) = nodes.Dequeue();

                if (x > 0)
                {
                    if (vis[GetIndex(x - 1, y, z)] != VisibleBit && data[GetIndex(x - 1, y, z)] == 0)
                    {
                        vis[GetIndex(x - 1, y, z)] = VisibleBit;
                        nodes.Enqueue((x - 1, y, z));
                    }
                }
                if (y > 0)
                {
                    if (vis[GetIndex(x, y - 1, z)] != VisibleBit && data[GetIndex(x, y - 1, z)] == 0)
                    {
                        vis[GetIndex(x, y - 1, z)] = VisibleBit;
                        nodes.Enqueue((x, y - 1, z));
                    }
                }
                if (z > 0)
                {
                    if (vis[GetIndex(x, y, z - 1)] != VisibleBit && data[GetIndex(x, y, z - 1)] == 0)
                    {
                        vis[GetIndex(x, y, z - 1)] = VisibleBit;
                        nodes.Enqueue((x, y, z - 1));
                    }
                }
                if (x < ChunkConstants.Side - 1)
                {
                    if (vis[GetIndex(x + 1, y, z)] != VisibleBit && data[GetIndex(x + 1, y, z)] == 0)
                    {
                        vis[GetIndex(x + 1, y, z)] = VisibleBit;
                        nodes.Enqueue((x + 1, y, z));
                    }
                }
                if (y < ChunkConstants.Side - 1)
                {
                    if (vis[GetIndex(x, y + 1, z)] != VisibleBit && data[GetIndex(x, y + 1, z)] == 0)
                    {
                        vis[GetIndex(x, y + 1, z)] = VisibleBit;
                        nodes.Enqueue((x, y + 1, z));
                    }
                }
                if (z < ChunkConstants.Side - 1)
                {
                    if (vis[GetIndex(x, y, z + 1)] != VisibleBit && data[GetIndex(x, y, z + 1)] == 0)
                    {
                        vis[GetIndex(x, y, z + 1)] = VisibleBit;
                        nodes.Enqueue((x, y, z + 1));
                    }
                }
            }
        }

        private void ComputeVisibility()
        {
            {
                int x = 0;
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        FloodFillVisibility(vis, x, y, z);
            }
            {
                int x = ChunkConstants.Side - 1;
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        FloodFillVisibility(vis, x, y, z);
            }
            {
                int x = 0;
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        FloodFillVisibility(vis, y, x, z);
            }
            {
                int x = ChunkConstants.Side - 1;
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        FloodFillVisibility(vis, y, x, z);
            }
            {
                int x = 0;
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        FloodFillVisibility(vis, z, y, x);
            }
            {
                int x = ChunkConstants.Side - 1;
                for (int y = 0; y <= ChunkConstants.Side - 1; y++)
                    for (int z = 0; z <= ChunkConstants.Side - 1; z++)
                        FloodFillVisibility(vis, z, y, x);
            }
        }

        private void ComputeFaceVisibility(Chunk[] neighbors)
        {
            var top_c = neighbors[0];
            var btm_c = neighbors[1];
            var frt_c = neighbors[2];
            var bck_c = neighbors[3];
            var lft_c = neighbors[4];
            var rgt_c = neighbors[5];

            for (int x = 0; x < ChunkConstants.Side; x++)
                for (int y = 0; y < ChunkConstants.Side; y++)
                    for (int z = 0; z < ChunkConstants.Side; z++)
                    {
                        byte top_v, btm_v, frt_v, bck_v, rgt_v, lft_v;
                        if (data[GetIndex(x, y, z)] == 0)
                            continue;

                        if (y < ChunkConstants.Side - 1) top_v = vis[GetIndex(x, y + 1, z)];
                        else if (top_c == null) top_v = DefaultEdgeVisibility;
                        else top_v = top_c.data[GetIndex(x, 0, z)] == 0 ? (byte)1 : (byte)0;

                        if (z < ChunkConstants.Side - 1) frt_v = vis[GetIndex(x, y, z + 1)];
                        else if (frt_c == null) frt_v = DefaultEdgeVisibility;
                        else frt_v = frt_c.data[GetIndex(x, y, 0)] == 0 ? (byte)1 : (byte)0;

                        if (x < ChunkConstants.Side - 1) rgt_v = vis[GetIndex(x + 1, y, z)];
                        else if (rgt_c == null) rgt_v = DefaultEdgeVisibility;
                        else rgt_v = rgt_c.data[GetIndex(0, y, z)] == 0 ? (byte)1 : (byte)0;

                        if (y > 0) btm_v = vis[GetIndex(x, y - 1, z)];
                        else if (btm_c == null) btm_v = DefaultEdgeVisibility;
                        else btm_v = btm_c.data[GetIndex(x, ChunkConstants.Side - 1, z)] == 0 ? (byte)1 : (byte)0;

                        if (z > 0) bck_v = vis[GetIndex(x, y, z - 1)];
                        else if (bck_c == null) bck_v = DefaultEdgeVisibility;
                        else bck_v = bck_c.data[GetIndex(x, y, ChunkConstants.Side - 1)] == 0 ? (byte)1 : (byte)0;

                        if (x > 0) lft_v = vis[GetIndex(x - 1, y, z)];
                        else if (lft_c == null) lft_v = DefaultEdgeVisibility;
                        else lft_v = lft_c.data[GetIndex(ChunkConstants.Side - 1, y, z)] == 0 ? (byte)1 : (byte)0;

                        top_v &= 1;
                        btm_v &= 1;
                        frt_v &= 1;
                        bck_v &= 1;
                        rgt_v &= 1;
                        lft_v &= 1;

                        vis[GetIndex(x, y, z)] |= (byte)((top_v << 1) | (btm_v << 2) | (frt_v << 3) | (bck_v << 4) | (rgt_v << 5) | (lft_v << 6));
                    }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessVoxel(byte x, byte y, byte z, byte run_start_idx, byte run_val, byte run_vis, byte run_len, Dictionary<uint, uint> indexDict, List<byte> faces, List<uint> indices, List<Vector4> cluster_bnds, List<byte> cluster_norms, ref byte minx, ref byte miny, ref byte minz, ref byte maxx, ref byte maxy, ref byte maxz, ref byte cur_norm_mask)
        {
            //Emit faces based on this run and its visibility
            if ((run_vis & (1 << 1)) != 0)
            {
                //Emit a patch for lighting
                //Group patches based on planes
                //Compute and store visibility angles which exit the chunk (16x16 angles)
                //Build a list of locally visible voxels - propogate the lighting on these
                //raycast exiting angles onto other chunks, store intersection positions - lighting can be injected into chunks based on these locations
                //0
                var tmp = new byte[]
                {
                    x, (byte)(y + 1), run_start_idx,
                    x, (byte)(y + 1), z,
                    (byte)(x + 1), (byte)(y + 1), z,
                    (byte)(x + 1), (byte)(y + 1), z,
                    (byte)(x + 1), (byte)(y + 1), run_start_idx,
                    x, (byte)(y + 1), run_start_idx,
                };
                EmitFace(run_val, (1 & 3) << 4 | (0 & 3) << 2 | (1 & 3), 0, tmp, indexDict, faces, indices, cluster_bnds, cluster_norms, ref minx, ref miny, ref minz, ref maxx, ref maxy, ref maxz, ref cur_norm_mask);
            }
            if ((run_vis & (1 << 2)) != 0)
            {
                //1
                var tmp = new byte[]
                {
                    (byte)(x + 1), y, z,
                    x, y, z,
                    x, y, run_start_idx,
                    x, y, run_start_idx,
                    (byte)(x + 1), y, run_start_idx,
                    (byte)(x + 1), y, z,
                };
                EmitFace(run_val, (1 & 3) << 4 | (2 & 3) << 2 | (1 & 3), 1, tmp, indexDict, faces, indices, cluster_bnds, cluster_norms, ref minx, ref miny, ref minz, ref maxx, ref maxy, ref maxz, ref cur_norm_mask);
            }

            if ((run_vis & (1 << 3)) != 0)
            {
                //2
                var tmp = new byte[]
                {
                    (byte)(x + 1), (byte)(y + 1), z,
                    x, (byte)(y + 1), z,
                    x, y, z,
                    x, y, z,
                    (byte)(x + 1), y, z,
                    (byte)(x + 1), (byte)(y + 1), z,
                };
                EmitFace(run_val, (1 & 3) << 4 | (1 & 3) << 2 | (0 & 3), 2, tmp, indexDict, faces, indices, cluster_bnds, cluster_norms, ref minx, ref miny, ref minz, ref maxx, ref maxy, ref maxz, ref cur_norm_mask);
            }

            if ((run_vis & (1 << 4)) != 0)
            {
                //3
                var tmp = new byte[]
                {
                    x, y, (byte)(z - 1),
                    x, (byte)(y + 1), (byte)(z - 1),
                    (byte)(x + 1), (byte)(y + 1), (byte)(z - 1),
                    (byte)(x + 1), (byte)(y + 1), (byte)(z - 1),
                    (byte)(x + 1), y, (byte)(z - 1),
                    x, y, (byte)(z - 1),
                };
                EmitFace(run_val, (1 & 3) << 4 | (1 & 3) << 2 | (2 & 3), 3, tmp, indexDict, faces, indices, cluster_bnds, cluster_norms, ref minx, ref miny, ref minz, ref maxx, ref maxy, ref maxz, ref cur_norm_mask);
            }

            if ((run_vis & (1 << 5)) != 0)
            {
                //4
                var tmp = new byte[]
                {
                    (byte)(x + 1), (byte)(y + 1), run_start_idx,
                    (byte)(x + 1), (byte)(y + 1), z,
                    (byte)(x + 1), y, z,
                    (byte)(x + 1), y, z,
                    (byte)(x + 1), y, run_start_idx,
                    (byte)(x + 1), (byte)(y + 1), run_start_idx,
                };
                EmitFace(run_val, (2 & 3) << 4 | (1 & 3) << 2 | (1 & 3), 4, tmp, indexDict, faces, indices, cluster_bnds, cluster_norms, ref minx, ref miny, ref minz, ref maxx, ref maxy, ref maxz, ref cur_norm_mask);
            }

            if ((run_vis & (1 << 6)) != 0)
            {
                //5
                var tmp = new byte[]
                {
                    x, y, z,
                    x, (byte)(y + 1), z,
                    x, (byte)(y + 1), run_start_idx,
                    x, (byte)(y + 1), run_start_idx,
                    x, y, run_start_idx,
                    x, y, z,
                };
                EmitFace(run_val, (0 & 3) << 4 | (1 & 3) << 2 | (1 & 3), 5, tmp, indexDict, faces, indices, cluster_bnds, cluster_norms, ref minx, ref miny, ref minz, ref maxx, ref maxy, ref maxz, ref cur_norm_mask);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeRun(byte x, byte y, Dictionary<uint, uint> indexDict, List<byte> faces, List<uint> indices, List<Vector4> cluster_bnds, List<byte> cluster_norms, ref byte minx, ref byte miny, ref byte minz, ref byte maxx, ref byte maxy, ref byte maxz, ref byte cur_norm_mask)
        {
            var runList = new List<Run>();
            byte run_start_idx = 0;
            byte run_val = data[GetIndex(x, y, 0)];
            byte run_vis = vis[GetIndex(x, y, 0)];
            byte run_len = 1;

            for (byte z = 1; z < ChunkConstants.Side; z++)
            {
                byte cur_val = data[GetIndex(x, y, z)];
                byte cur_vis = vis[GetIndex(x, y, z)];
                if (run_val == 0 | run_vis == 0)
                {
                    run_start_idx = z;
                    run_val = data[GetIndex(x, y, z)];
                    run_vis = vis[GetIndex(x, y, z)];
                    run_len = 1;
                }
                else if (cur_val == run_val && cur_vis == run_vis)
                {
                    run_len++;
                    continue;
                }
                else
                {
                    runList.Add(new Run()
                    {
                        Start = run_start_idx,
                        Stop = (byte)(run_start_idx + run_len),
                        Value = run_val,
                        Visibility = run_vis
                    });

                    ProcessVoxel(x, y, z, run_start_idx, run_val, run_vis, run_len, indexDict, faces, indices, cluster_bnds, cluster_norms, ref minx, ref miny, ref minz, ref maxx, ref maxy, ref maxz, ref cur_norm_mask);

                    run_start_idx = z;
                    run_val = cur_val;
                    run_vis = cur_vis;
                    run_len = 1;
                }
            }
            if (run_val != 0 && run_vis != 0)
            {
                runList.Add(new Run()
                {
                    Start = run_start_idx,
                    Stop = (byte)(run_start_idx + run_len),
                    Value = run_val,
                    Visibility = run_vis
                });
                ProcessVoxel(x, y, ChunkConstants.Side, run_start_idx, run_val, run_vis, run_len, indexDict, faces, indices, cluster_bnds, cluster_norms, ref minx, ref miny, ref minz, ref maxx, ref maxy, ref maxz, ref cur_norm_mask);
            }

            runs[x * ChunkConstants.Side + y] = runList.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EmitFace(byte cur, int norm, int normal_idx, byte[] tmp, Dictionary<uint, uint> indexDict, List<byte> faces, List<uint> indices, List<Vector4> cluster_bnds, List<byte> cluster_norms, ref byte minx, ref byte miny, ref byte minz, ref byte maxx, ref byte maxy, ref byte maxz, ref byte cur_norm_mask)
        {
            cur_norm_mask |= (byte)(1 << normal_idx);
            for (int i = 0; i < 6; i++)
            {
                minx = Math.Min(minx, tmp[i * 3 + 0]);
                miny = Math.Min(miny, tmp[i * 3 + 1]);
                minz = Math.Min(minz, tmp[i * 3 + 2]);

                maxx = Math.Max(maxx, tmp[i * 3 + 0]);
                maxy = Math.Max(maxy, tmp[i * 3 + 1]);
                maxz = Math.Max(maxz, tmp[i * 3 + 2]);

                var vec = (uint)tmp[i * 3 + 2] << 16 | (uint)tmp[i * 3 + 1] << 8 | tmp[i * 3];
                if (!indexDict.ContainsKey(vec))
                {
                    indices.Add((uint)norm << 24 | (uint)cur << 16 | (uint)(indexDict.Count & 0xffff));
                    indexDict[vec] = (ushort)indexDict.Count;
                    faces.Add(tmp[i * 3 + 0]);
                    faces.Add(tmp[i * 3 + 1]);
                    faces.Add(tmp[i * 3 + 2]);
                    faces.Add(0);
                }
                else
                {
                    indices.Add((uint)norm << 24 | (uint)cur << 16 | indexDict[vec]);
                }
            }

            if (indices.Count % ChunkConstants.BlockSize == 0)
            {
                //Compute bounds and set norm mask
                Vector3 c = new Vector3(maxx + minx, maxy + miny, maxz + minz) * 0.5f;
                float radsq = (new Vector3(maxx, maxy, maxz) - c).Length;

                cluster_bnds.Add(new Vector4(c, radsq));
                cluster_norms.Add(cur_norm_mask);

                cur_norm_mask = 0;
                maxx = maxy = maxz = byte.MinValue;
                minx = miny = minz = byte.MaxValue;
            }
        }

        public void RebuildFullMesh(params Chunk[] neighbors)
        {
            VoxelCount = 0;

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var faces = new List<byte>();
            var indices = new List<uint>();
            var cluster_bnds = new List<Vector4>();
            var cluster_norms = new List<byte>();
            var indexDict = new Dictionary<uint, uint>();
            byte cur_norm_mask = 0;
            byte minx = byte.MaxValue, miny = byte.MaxValue, minz = byte.MaxValue, maxx = byte.MinValue, maxy = byte.MinValue, maxz = byte.MinValue;

            Array.Clear(vis, 0, vis.Length);
            ComputeVisibility();
            ComputeFaceVisibility(neighbors);
            for (byte y = 0; y < ChunkConstants.Side; y++)
                for (byte x = 0; x < ChunkConstants.Side; x++)
                    ComputeRun(x, y, indexDict, faces, indices, cluster_bnds, cluster_norms, ref minx, ref miny, ref minz, ref maxx, ref maxy, ref maxz, ref cur_norm_mask);


            //TODO redo system to compute visibility map once, apply neighbor face visibility too, compute to sets of runs along X or Y axes per layer
            //TODO test rendering triangles with compute culling instead of cluster culling
            if (indices.Count % ChunkConstants.BlockSize != 0)
            {
                //Compute bounds and set norm mask
                Vector3 c = new Vector3(maxx + minx, maxy + miny, maxz + minz) * 0.5f;
                float radsq = (new Vector3(maxx, maxy, maxz) - c).Length;

                cluster_bnds.Add(new Vector4(c, radsq));
                cluster_norms.Add(cur_norm_mask);
            }

            this.faces = faces.ToArray();
            this.indices = indices.ToArray();
            this.bounds = cluster_bnds.ToArray();
            this.norm_mask = cluster_norms.ToArray();

            stopwatch.Stop();
            Console.WriteLine($"[{id}] Meshing time: {stopwatch.Elapsed.TotalMilliseconds}ms");

            /*
            string str = "";
            string indi = "";
            for (int i = 0; i < indices.Count; i++)
            {
                str += $"v {faces[(int)(indices[i] & 0xffff) * 4]} {faces[(int)(indices[i] & 0xffff) * 4 + 1]} {faces[(int)(indices[i] & 0xffff) * 4 + 2]} {faces[(int)(indices[i] & 0xffff) * 4 + 3]}\n";
                indi += $"f {i + 1} {i + 2} {i + 3}\n";
            }
            File.WriteAllText($"tmp{id}.obj", str + indi);*/
            empty = (this.faces.Length == 0);

            dirty = false;
            update_pending = true;
        }

        public void EditLocalMesh(int x, int y, int z, byte cur)
        {
            //Just edit the current value
            lock (data_locker)
            {
                var prev_data = data[GetIndex(x, y, z)];
                if (prev_data != cur) dirty = true;
                if (cur == 0 && prev_data != 0)
                    VoxelCount--;
                else if (cur != 0 && prev_data == 0)
                    VoxelCount++;

                data[GetIndex(x, y, z)] = cur;
            }
        }

        public void Update()
        {
            // if (dirty)
            //RebuildFullMesh();
        }
    }
}