using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Common
{
    public class OrderedHashSet<T> : IList<T> where T : IComparable<T>
    {
        private Dictionary<int, T> entries;

        public T this[int index] { get => entries[index]; set { entries[index] = value; } }

        public int Count { get => entries.Count; }

        public bool IsReadOnly => false;

        public OrderedHashSet()
        {
            entries = new Dictionary<int, T>();
        }

        public void Add(T item)
        {
            entries.Add(entries.Count, item);
        }

        public void Clear()
        {
            entries.Clear();
        }

        public bool Contains(T item)
        {
            return entries.ContainsValue(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = arrayIndex; i < Math.Min(array.Length, Count); i++)
                array[i] = entries[i];
        }

        public IEnumerator<T> GetEnumerator()
        {
            return entries.Values.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return entries.First(a => a.Value.CompareTo(item) == 0).Key;
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            entries.Remove(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return entries.GetEnumerator();
        }
    }
}
