﻿using Kokoro.Graphics;
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
        private ChunkStreamer streamer;
        private Dictionary<(int, int, int), Chunk> ChunkSet;

        public Vector3 Position { get; set; }

        public ChunkObject(ChunkStreamer streamer)
        {
            ChunkSet = new Dictionary<(int, int, int), Chunk>();
            this.streamer = streamer;
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

                if (!ChunkSet.ContainsKey(coord))
                {
                    ChunkSet[coord] = streamer.Allocate();
                    ChunkSet[coord].Owner = this;
                }

                ChunkSet[coord].EditLocalMesh(x_o, y_o, z_o, val);
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

            if (!ChunkSet.ContainsKey(coord))
            {
                ChunkSet[coord] = streamer.Allocate();
                ChunkSet[coord].Owner = this;
            }

            ChunkSet[coord].EditLocalMesh(x_o, y_o, z_o, val);
        }

        public void RebuildAll()
        {
            foreach (var c in ChunkSet)
            {
                var (x, y, z) = c.Key;
                c.Value.RebuildFullMesh(GetChunk(x, y + ChunkConstants.Side, z), GetChunk(x, y - ChunkConstants.Side, z), GetChunk(x, y, z + ChunkConstants.Side), GetChunk(x, y, z - ChunkConstants.Side), GetChunk(x - ChunkConstants.Side, y, z), GetChunk(x + ChunkConstants.Side, y, z));
            }
        }

        public Chunk GetChunk(int x, int y, int z)
        {
            var x_b = x & ~(ChunkConstants.Side - 1);
            var y_b = y & ~(ChunkConstants.Side - 1);
            var z_b = z & ~(ChunkConstants.Side - 1);

            var coord = (x_b, y_b, z_b);

            if (ChunkSet.ContainsKey(coord))
                return ChunkSet[coord];
            return null;
        }

        public override void Render(double time, Framebuffer fbuf, VREye eye)
        {
            foreach (var coord_chunk_pair in ChunkSet)
            {
                var coord = coord_chunk_pair.Key;
                var chunk = coord_chunk_pair.Value;

                //Do not submit draw if chunk itself isn't in the view frustum
                //Use a compute shader to cull everything
                //Generate vertices in full clusters, use extra bits in vertex data to encode normal
                //Much smaller clusters for culling
                streamer.RenderChunk(chunk, Position + new Vector3(coord.Item1, coord.Item2, coord.Item3));
            }
            //TODO get view position, submit draws from nearest to farthest
        }

        public override void Update(double time, World parent)
        {

        }
    }
}
