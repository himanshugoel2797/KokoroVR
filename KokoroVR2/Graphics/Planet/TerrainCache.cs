using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using Kokoro.Common;

namespace KokoroVR2.Graphics.Planet
{
    public class TerrainCacheEntry : ICacheable
    {
        public string ParentName { get; }
        public LinkedListNode<int> CacheID { get; set; }

        public short[] HeightData { get; set; }
        public ulong NodeID { get; set; }
        public uint Level { get; set; }

        public TerrainCacheEntry()
        {
            HeightData = new short[TerrainCache.TileSide * TerrainCache.TileSide];
        }
    }

    public class TerrainCache
    {
        private const int CacheLen = 1 << 16;
        public const int TileSide = TerrainTileMesh.Side;
        private readonly FixedLenCache<TerrainCacheEntry> Cache;

        public ConcurrentDictionary<ulong, int> TileMap { get; private set; }

        public TerrainCache()
        {
            Cache = new FixedLenCache<TerrainCacheEntry>(CacheLen, (a) => new TerrainCacheEntry(), false);
            TileMap = new ConcurrentDictionary<ulong, int>();
        }

        public TerrainCacheEntry TryAllocate(bool evictOnly = false)
        {
            return Cache.TryAllocate(evictOnly);
        }

        public bool UpdateEntry(TerrainCacheEntry entry)
        {
            return Cache.UpdateEntry(entry);
        }

        public void RegisterEvictionHandler(string name, Action<TerrainCacheEntry> handler)
        {
            Cache.RegisterEvictionHandler(name, handler);
        }

        public void UnregisterEvictionHandler(string name)
        {
            Cache.UnregisterEvictionHandler(name);
        }

        public TerrainCacheEntry GetEntry(int idx)
        {
            return Cache.GetEntry(idx);
        }
    }
}
