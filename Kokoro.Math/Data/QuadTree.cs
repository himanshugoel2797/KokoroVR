﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Math.Data
{
    public class QuadTree<T>
    {
        public QuadTree<T> TopLeft { get; private set; }
        public QuadTree<T> TopRight { get; private set; }
        public QuadTree<T> BottomLeft { get; private set; }
        public QuadTree<T> BottomRight { get; private set; }

        public Vector2 Min { get; private set; }
        public Vector2 Max { get; private set; }
        public T Value { get; set; }

        public int Level { get; private set; }

        public bool IsLeaf { get; private set; }

        public QuadTree(Vector2 min, Vector2 max, int lvl)
        {
            this.Max = max;
            this.Min = min;
            this.Level = lvl;
            IsLeaf = true;
        }

        public QuadTree<T> this[int idx]
        {
            get =>
                idx switch
                {
                    0 => TopLeft,
                    1 => TopRight,
                    2 => BottomLeft,
                    3 => BottomRight,
                    _ => throw new IndexOutOfRangeException()
                };
        }

        public void Insert(Vector2 min, Vector2 max, T val)
        {
            var c = (Min + Max) * 0.5f;
            if (max == Max && min == Min)
            {
                Value = val;
                return;
            }

            if (IsLeaf)
                Split();

            if (min.X >= c.X)
            {
                if (min.Y >= c.Y)
                {
                    TopRight.Insert(min, max, val);
                }
                else
                {
                    BottomRight.Insert(min, max, val);
                }
            }
            else
            {
                if (min.Y >= c.Y)
                {
                    TopLeft.Insert(min, max, val);
                }
                else
                {
                    BottomLeft.Insert(min, max, val);
                }
            }
        }

        public void Split()
        {
            Vector2 ml = new Vector2(Min.X, (Max.Y - Min.Y) * 0.5f + Min.Y);
            Vector2 tm = new Vector2((Max.X - Min.X) * 0.5f + Min.X, Max.Y);

            Vector2 mr = new Vector2(Max.X, (Max.Y - Min.Y) * 0.5f + Min.Y);
            Vector2 bm = new Vector2((Max.X - Min.X) * 0.5f + Min.X, Min.Y);

            Vector2 c = new Vector2((Max.X - Min.X) * 0.5f + Min.X, (Max.Y - Min.Y) * 0.5f + Min.Y);

            IsLeaf = false;
            TopLeft = new QuadTree<T>(ml, tm, Level + 1);
            TopRight = new QuadTree<T>(c, Max, Level + 1);
            BottomLeft = new QuadTree<T>(Min, c, Level + 1);
            BottomRight = new QuadTree<T>(bm, mr, Level + 1);
        }
    }
}
