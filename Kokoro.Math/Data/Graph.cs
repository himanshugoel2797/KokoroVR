using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kokoro.Math.Data
{
    public class Graph<T>
    {
        public Dictionary<T, List<T>> Nodes { get; private set; }
        public Dictionary<T, List<int>> Windings { get; private set; }

        public Graph()
        {
            Nodes = new Dictionary<T, List<T>>();
            Windings = new Dictionary<T, List<int>>();
        }

        public void AddVertex(T node)
        {
            if (!Nodes.ContainsKey(node))
            {
                Nodes[node] = new List<T>();
                Windings[node] = new List<int>();
            }
        }

        private void RemoveVertex(T node)
        {
            Nodes.Remove(node);
            Windings.Remove(node);
        }

        private void AddVertConnection(T node, T connection, int winding)
        {
            if (!Nodes[node].Contains(connection))
            {
                Windings[node].Add(winding);
                Nodes[node].Add(connection);
            }
            else
            {
                int idx = Nodes[node].IndexOf(connection);
                Windings[node][idx] = Windings[node][idx] + winding;
            }
        }

        private void RemoveVertConnection(T node, T connection)
        {
            int idx = Nodes[node].IndexOf(connection);
            Nodes[node].RemoveAt(idx);
            Windings[node].RemoveAt(idx);
        }

        public void AddConnection(T node, T connection)
        {
            AddVertex(node);
            AddVertex(connection);
            AddVertConnection(node, connection, +1);
            AddVertConnection(connection, node, -1);
        }

        public void RemoveConnection(T node, T connection)
        {
            RemoveVertConnection(node, connection);
            RemoveVertConnection(connection, node);
            if (Nodes[node].Count == 0) RemoveVertex(node);
            if (Nodes[connection].Count == 0) RemoveVertex(connection);
        }
    }
}
