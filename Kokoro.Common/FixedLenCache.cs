using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kokoro.Common
{
    public interface ICacheable
    {
        string ParentName { get; }
        LinkedListNode<int> CacheID { get; set; }
    }

    public class FixedLenCache<T> where T : ICacheable
    {
        private bool _mandatoryUpdate;
        private SemaphoreSlim semaphore;
        private LinkedList<int> indices;
        private LinkedList<int> freeIndices;
        private T[] entries;
        private Dictionary<string, Action<T>> evictionHandlers;
        private Func<FixedLenCache<T>, T> constructorFunc;

        public FixedLenCache(int len, Func<FixedLenCache<T>, T> constructor, bool mandatoryUpdate)
        {
            _mandatoryUpdate = mandatoryUpdate;
            constructorFunc = constructor;
            semaphore = new SemaphoreSlim(1);
            entries = new T[len];
            evictionHandlers = new Dictionary<string, Action<T>>();
            indices = new LinkedList<int>();
            freeIndices = new LinkedList<int>();
            for (int i = 0; i < entries.Length; i++)
                freeIndices.AddLast(i);
        }

        public T TryAllocate(bool evictOnly = false)
        {
            semaphore.Wait();
            try
            {
                int cur_idx;
                if (freeIndices.Count == 0 | evictOnly)
                {
                    cur_idx = indices.First.Value;
                    indices.RemoveFirst();
                }
                else
                {
                    cur_idx = freeIndices.First.Value;
                    freeIndices.RemoveFirst();
                }

                if (entries[cur_idx] != null)
                {
                    if (evictionHandlers.ContainsKey(entries[cur_idx].ParentName))
                        evictionHandlers[entries[cur_idx].ParentName](entries[cur_idx]);
                    entries[cur_idx].CacheID = null;
                }

                entries[cur_idx] = constructorFunc(this);
                var cur_node = indices.AddLast(cur_idx);
                entries[cur_idx].CacheID = cur_node;
                return entries[cur_idx];
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void RegisterEvictionHandler(string name, Action<T> handler)
        {
            evictionHandlers[name] = handler;
        }

        public void UnregisterEvictionHandler(string name)
        {
            if (evictionHandlers.ContainsKey(name)) evictionHandlers.Remove(name);
        }
        public bool UpdateEntry(T entry)
        {
            if (entry.CacheID == null)
                return true;
            if (semaphore.Wait(_mandatoryUpdate ? -1 : 0))
            {
                try
                {
                    indices.Remove(entry.CacheID);
                    entry.CacheID = indices.AddLast(entry.CacheID.Value);
                }
                finally
                {
                    semaphore.Release();
                }
                return true;
            }
            return false;
        }

        public T GetEntry(int idx)
        {
            semaphore.Wait();
            try
            {
                indices.Remove(entries[idx].CacheID);
                entries[idx].CacheID = indices.AddLast(idx);
                return entries[idx];
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
