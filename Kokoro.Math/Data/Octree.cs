﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Math.Data
{

    /// 
    /// Represents an octree spatial partioning system.
    /// 
    public class Octree<T> where T : class
    {
        public class OctreeData
        {
            public long WorldSide;
        }

        public T NodeValue { get; set; }

        //Children if needed
        public Octree<T>[] Children { get; set; }

        public Octree<T> Parent { get; protected set; }

        public OctreeData Data { get; protected set; }

        //Current octree level
        public int Level { get; set; }

        public const int ChildrenCount = 8;

        public Octree(int lvl, long side)
        {
            Data = new OctreeData()
            {
                WorldSide = side
            };

            Level = lvl;
        }

        private Octree(int lvl, OctreeData data)
        {
            Level = lvl;
            Data = data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ChildIndex(long X, long Y, long Z, long X_c, long Y_c, long Z_c)
        {
            return Convert.ToInt32(X >= X_c) | Convert.ToInt32(Y >= Y_c) << 1 | Convert.ToInt32(Z >= Z_c) << 2;
        }
        private T Get(long X, long Y, long Z, long x_c, long y_c, long z_c, long side)
        {
            if (side == Data.WorldSide >> Level)
                return NodeValue;

            long x_o = X;
            long y_o = Y;
            long z_o = Z;

            int idx = ChildIndex(X, Y, Z, x_c, y_c, z_c);

            if (Children == null)
                return null;

            if (Children[idx] == null)
                return null;

            long x_side = ((X >= x_c) ? 1 : -1) * Data.WorldSide >> (Level + 2);
            long y_side = ((Y >= y_c) ? 1 : -1) * Data.WorldSide >> (Level + 2);
            long z_side = ((Z >= z_c) ? 1 : -1) * Data.WorldSide >> (Level + 2);
            return Children[idx].Get(X, Y, Z, x_c + x_side, y_c + y_side, z_c + z_side, side);
        }

        public T Get(long X, long Y, long Z, long side)
        {
            return Get(X, Y, Z, 0, 0, 0, side);
        }

        public T this[long X, long Y, long Z, long side]
        {
            get
            {
                return Get(X, Y, Z, side);
            }
        }

        private bool Contains(long X, long Y, long Z, long x_c, long y_c, long z_c, long side)
        {
            if (side == Data.WorldSide >> Level)
                return NodeValue != null;

            long x_o = X;
            long y_o = Y;
            long z_o = Z;

            int idx = ChildIndex(X, Y, Z, x_c, y_c, z_c);

            if (Children == null)
                return false;

            if (Children[idx] == null)
                return false;

            long x_side = ((X >= x_c) ? 1 : -1) * Data.WorldSide >> (Level + 2);
            long y_side = ((Y >= y_c) ? 1 : -1) * Data.WorldSide >> (Level + 2);
            long z_side = ((Z >= z_c) ? 1 : -1) * Data.WorldSide >> (Level + 2);
            return Children[idx].Contains(X, Y, Z, x_c + x_side, y_c + y_side, z_c + z_side, side);
        }

        public bool Contains(long X, long Y, long Z, long side)
        {
            return Contains(X, Y, Z, 0, 0, 0, side);
        }

        private void Add(T obj, long X, long Y, long Z, long x_c, long y_c, long z_c, long side)
        {
            //If the side matches the side of this voxel, set the color and fill the entire voxel
            if (side == Data.WorldSide >> Level)
            {
                NodeValue = obj;
                Children = null;  //drop all the children to mark them as free
                return;
            }

            long x_o = X;
            long y_o = Y;
            long z_o = Z;

            int idx = ChildIndex(X, Y, Z, x_c, y_c, z_c);

            if (Children == null)
                Children = new Octree<T>[ChildrenCount];

            if (Children[idx] == null)
            {
                Children[idx] = new Octree<T>(Level + 1, Data)
                {
                    Children = null,
                    Parent = this,
                };
            }

            long x_side = ((X >= x_c) ? 1 : -1) * Data.WorldSide >> (Level + 2);
            long y_side = ((Y >= y_c) ? 1 : -1) * Data.WorldSide >> (Level + 2);
            long z_side = ((Z >= z_c) ? 1 : -1) * Data.WorldSide >> (Level + 2);

            Children[idx].Add(obj, X, Y, Z, x_c + x_side, y_c + y_side, z_c + z_side, side);
        }

        //Add a voxel, specify a side length and a location
        public void Add(T obj, long X, long Y, long Z, long side)
        {
            if (!Math.MathHelper.IsLog2((ulong)side))
                throw new ArgumentException("side must be a power of 2");

            if (X % side != 0)
                throw new ArgumentException("X must be a multiple of side");

            if (Y % side != 0)
                throw new ArgumentException("Y must be a multiple of side");

            if (Z % side != 0)
                throw new ArgumentException("Z must be a multiple of side");

            Add(obj, X, Y, Z, 0, 0, 0, side);
        }

        private void GetVisibleChunks(List<(T, long[])> chunks, Frustum f, long x_c, long y_c, long z_c)
        {
            long side = Data.WorldSide >> Level;
            if (f.IsVisible(new Vector3(x_c - side / 2, y_c - side / 2, z_c - side / 2), new Vector3(x_c + side / 2, y_c + side / 2, z_c + side / 2)))
            {
                if (NodeValue != null) chunks.Add((NodeValue, new long[] { x_c - (side >> 1), y_c - (side >> 1), z_c - (side >> 1) }));
                if (Children == null) return;
                for (int i = 0; i < Children.Length; i++)
                {
                    int X = ((i & 1) * 2 - 1);
                    int Y = ((i & 2) >> 1) * 2 - 1;
                    int Z = ((i & 4) >> 2) * 2 - 1;
                    long x_side = (X * Data.WorldSide >> (Level + 2));
                    long y_side = (Y * Data.WorldSide >> (Level + 2));
                    long z_side = (Z * Data.WorldSide >> (Level + 2));

                    if (Children[i] != null) Children[i].GetVisibleChunks(chunks, f, x_c + x_side, y_c + y_side, z_c + z_side);
                }
            }
        }

        public IEnumerable<(T, long[])> GetVisibleChunks(Frustum f)
        {
            var chunks = new List<(T, long[])>();
            GetVisibleChunks(chunks, f, 0, 0, 0);
            return chunks;
        }
    }
}
