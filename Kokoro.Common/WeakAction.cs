﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Common
{
    public class WeakAction
    {
        private List<WeakReference> weakRef;

        public WeakAction()
        {
            weakRef = new List<WeakReference>();
        }

        public void Add(Action a)
        {
            weakRef.Add(new WeakReference(a));
        }

        public static implicit operator WeakAction(Action a)
        {
            var w = new WeakAction();
            w.Add(a);
            return w;
        }

        public static WeakAction operator +(WeakAction a, WeakAction b)
        {
            WeakAction n = new WeakAction();
            n.weakRef.AddRange(a.weakRef);
            n.weakRef.AddRange(b.weakRef);
            return n;
        }

        public void Invoke()
        {
            for (int i = 0; i < weakRef.Count; i++)
            {
                if (weakRef[i].IsAlive)
                {
                    (weakRef[i].Target as Action)?.Invoke();
                }
            }
        }
    }
}