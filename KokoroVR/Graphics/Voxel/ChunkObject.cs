using Kokoro.Graphics;
using Kokoro.Graphics.Prefabs;
using Kokoro.Math;
using Kokoro.Math.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KokoroVR.Graphics.Voxel
{
    public class ChunkObject : Interactable   //Represents a composite chunk object, eventually we want to be able to break off disconnected chunks
    {
        private Octree<Chunk> ChunkTree;
        private List<int[]> coords;

        public VoxelGI GI { get; }
        public ChunkStreamer Streamer { get; }

        public ChunkObject(ChunkStreamer streamer)
        {
            GI = new VoxelGI(this);
            ChunkTree = new Octree<Chunk>(0, 16384);
            coords = new List<int[]>();
            this.Streamer = streamer;
        }

        public void BulkSet((int, int, int, byte)[] updates)
        {
            foreach (var (x, y, z, val) in updates)
            {
                var x_b = x & ~(ChunkConstants.Side - 1);
                var y_b = y & ~(ChunkConstants.Side - 1);
                var z_b = z & ~(ChunkConstants.Side - 1);

                var x_o = x - x_b;
                var y_o = y - y_b;
                var z_o = z - z_b;

                var coord = (x_b, y_b, z_b);

                if (!ChunkTree.Contains(x_b, y_b, z_b, ChunkConstants.Side))
                {
                    var tmp = Streamer.Allocate();
                    tmp.Owner = this;
                    ChunkTree.Add(tmp, x_b, y_b, z_b, ChunkConstants.Side);
                    coords.Add(new int[] { x_b, y_b, z_b });
                }

                ChunkTree[x_b, y_b, z_b, ChunkConstants.Side].EditLocalMesh(x_o, y_o, z_o, val);
            }
            RebuildAll();
        }

        public void Set(int x, int y, int z, byte val)
        {
            var x_b = x & ~(ChunkConstants.Side - 1);
            var y_b = y & ~(ChunkConstants.Side - 1);
            var z_b = z & ~(ChunkConstants.Side - 1);

            var x_o = x - x_b;
            var y_o = y - y_b;
            var z_o = z - z_b;

            var coord = (x_b, y_b, z_b);

            if (!ChunkTree.Contains(x_b, y_b, z_b, ChunkConstants.Side))
            {
                var tmp = Streamer.Allocate();
                tmp.Owner = this;
                ChunkTree.Add(tmp, x_b, y_b, z_b, ChunkConstants.Side);
                coords.Add(new int[] { x_b, y_b, z_b });
            }

            ChunkTree[x_b, y_b, z_b, ChunkConstants.Side].EditLocalMesh(x_o, y_o, z_o, val);
        }

        public void RebuildAll()
        {
            foreach (var c in coords)
            {
                int x = c[0];
                int y = c[1];
                int z = c[2];
                ChunkTree[x, y, z, ChunkConstants.Side].RebuildFullMesh(new Vector3(x, y, z), GetChunk(x, y + ChunkConstants.Side, z), GetChunk(x, y - ChunkConstants.Side, z), GetChunk(x, y, z + ChunkConstants.Side), GetChunk(x, y, z - ChunkConstants.Side), GetChunk(x - ChunkConstants.Side, y, z), GetChunk(x + ChunkConstants.Side, y, z));
            }
        }

        public Chunk GetChunk(int x, int y, int z)
        {
            var x_b = x & ~(ChunkConstants.Side - 1);
            var y_b = y & ~(ChunkConstants.Side - 1);
            var z_b = z & ~(ChunkConstants.Side - 1);

            if (ChunkTree.Contains(x_b, y_b, z_b, ChunkConstants.Side))
                return ChunkTree[x_b, y_b, z_b, ChunkConstants.Side];
            return null;
        }

        public override void Render(double time, Framebuffer fbuf, VREye eye)
        {
            /*foreach (var coord_chunk_pair in coords)
            {
                var chunk = ChunkTree[coord_chunk_pair[0], coord_chunk_pair[1], coord_chunk_pair[2], ChunkConstants.Side];

                //Do not submit draw if chunk itself isn't in the view frustum
                //Use a compute shader to cull everything
                //Generate vertices in full clusters, use extra bits in vertex data to encode normal
                //Much smaller clusters for culling
                streamer.RenderChunk(chunk, Position + new Vector3(coord_chunk_pair[0], coord_chunk_pair[1], coord_chunk_pair[2]));
            }*/
            var chunks = ChunkTree.GetVisibleChunks(Engine.Frustums[(int)eye]);
            foreach ((Chunk c, long[] comps) in chunks)
                Streamer.RenderChunk(c, new Vector3(comps[0], comps[1], comps[2]));
            /*
            foreach(var c in coords)
            {
                int x = c[0];
                int y = c[1];
                int z = c[2];
                Streamer.RenderChunk(ChunkTree[x, y, z, ChunkConstants.Side], new Vector3(x, y, z));
            }*/
        }

        public override void Update(double time, World parent)
        {

        }
    }
}
