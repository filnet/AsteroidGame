//#define DEBUG_POOL

using System;
using System.Collections.Generic;
using System.Linq;

namespace GameLibrary.Util
{
    public class ObjectPool<K, V>
    {
        public delegate V ObjectFactory(K key, V obj);

        private sealed class PoolEntry
        {
            internal int refCount;
            internal V obj;
        }

        private struct FreeEntry
        {
            internal K key;
            internal V obj;
        }

        private sealed class FreeEntryEqualityComparer : IEqualityComparer<FreeEntry>
        {
            private readonly IEqualityComparer<K> keyEqualityComparer;

            public FreeEntryEqualityComparer(IEqualityComparer<K> keyEqualityComparer)
            {
                this.keyEqualityComparer = keyEqualityComparer;
            }

            public bool Equals(FreeEntry entry1, FreeEntry entry2)
            {
                return keyEqualityComparer.Equals(entry1.key, entry2.key);
            }

            public int GetHashCode(FreeEntry entry)
            {
                return keyEqualityComparer.GetHashCode(entry.key);
            }
        }

        private readonly int maxCount = 32;

        private readonly Dictionary<K, PoolEntry> pool;
        private readonly HashSet<FreeEntry> freeSet;

        private readonly ObjectFactory objectFactory;

        public ObjectPool(ObjectFactory objectFactory, IEqualityComparer<K> keyEqualityComparer)
        {
            if (objectFactory == null) throw new ArgumentNullException("objectFactory");
            this.objectFactory = objectFactory;
            pool = new Dictionary<K, PoolEntry>(keyEqualityComparer);
            FreeEntryEqualityComparer freeEntryEqualityComparer = new FreeEntryEqualityComparer(keyEqualityComparer);
            freeSet = new HashSet<FreeEntry>(freeEntryEqualityComparer);
        }

        public V Take(K key)
        {
            PoolEntry entry;
            if (pool.TryGetValue(key, out entry))
            {
#if DEBUG_POOL
                Console.WriteLine("ObjectPool: Using entry for {0}", key);
#endif
                if (entry.refCount == 0)
                {
                    FreeEntry freeEntryToRemove;
                    freeEntryToRemove.key = key;
                    freeEntryToRemove.obj = entry.obj;
                    freeSet.Remove(freeEntryToRemove);
                }
                entry.refCount++;
                // needed because of the use of structs!
                //pool[key] = entry;
                return entry.obj;
            }
            if (pool.Count >= maxCount)
            {
                // max reached, start reusing entries
                // TODO should be lifo... not sure HashSet works like that
                FreeEntry freeEntry = freeSet.First();
#if DEBUG_POOL
                Console.WriteLine("ObjectPool: Reusing entry for {0}, old entry {1}", key, freeEntry.key);
#endif
                if (!freeSet.Remove(freeEntry))
                {
                    throw new InvalidOperationException("invalid pool state");
                }
                // remove and reuse pool entry
                if (!pool.TryGetValue(freeEntry.key, out entry))
                {
                    throw new InvalidOperationException("invalid pool state");
                }
                if (!pool.Remove(freeEntry.key))
                {
                    throw new InvalidOperationException("invalid pool state");
                }

                entry.obj = objectFactory(key, freeEntry.obj);
            }
            else
            {
#if DEBUG_POOL
                Console.WriteLine("ObjectPool: Adding entry for {0}", key);
#endif
                // create a new object
                entry = new PoolEntry();
                entry.obj = objectFactory(key, default(V));
            }
            entry.refCount = 1;
            pool.Add(key, entry);
#if DEBUG_POOL
            Console.WriteLine("ObjectPool: size = {0}, free size = {1}", pool.Count, freeSet.Count);
#endif
            return entry.obj;
        }

        public void Give(K key)
        {
#if DEBUG_POOL
            //Console.WriteLine("ObjectPool: Freeing {0}", key);
#endif
            PoolEntry entry;
            if (pool.TryGetValue(key, out entry))
            {
                entry.refCount--;
                if (entry.refCount == 0)
                {
                    FreeEntry freeEntry;
                    // key is kept for cleaning when reusing this entry
                    freeEntry.key = key;
                    freeEntry.obj = entry.obj;
                    freeSet.Add(freeEntry);
#if DEBUG_POOL
                    //Console.WriteLine("ObjectPool: Freed {0}", key);
#endif
                }
                // needed because of the use of structs!
                //pool[key] = entry;
            }
            else
            {
                throw new InvalidOperationException("invalid give");
            }
#if DEBUG_POOL
            Console.WriteLine("ObjectPool: size = {0}, free size = {1}", pool.Count, freeSet.Count);
#endif
        }
    }
}
