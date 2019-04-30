//#define DEBUG_POOL

using System;
using System.Collections.Generic;
using System.Linq;

namespace GameLibrary.Util
{
    public class ObjectPool<K, V>
    {
        struct PoolEntry
        {
            internal int refCount;
            internal V obj;
        }

        struct FreeEntry
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

        public delegate V ObjectFactory(K key);
        public delegate void ObjectMutator(K key, V obj);

        private readonly ObjectFactory objectFactory;
        private readonly ObjectMutator objectMutator;

        public ObjectPool(ObjectFactory objectFactory, ObjectMutator objectMutator, IEqualityComparer<K> keyEqualityComparer)
        {
            if (objectFactory == null) throw new ArgumentNullException("objectFactory");
            if (objectMutator == null) throw new ArgumentNullException("objectMutator");
            this.objectFactory = objectFactory;
            this.objectMutator = objectMutator;
            pool = new Dictionary<K, PoolEntry>(keyEqualityComparer);
            FreeEntryEqualityComparer freeEntryEqualityComparer = new FreeEntryEqualityComparer(keyEqualityComparer);
            freeSet = new HashSet<FreeEntry>(freeEntryEqualityComparer);
        }

        public V Take(K key)
        {
            FreeEntry freeEntryToRemove;
            PoolEntry entry;
            if (pool.TryGetValue(key, out entry))
            {
#if DEBUG_POOL
                Console.WriteLine("ObjectPool: Using entry for {0}", key);
#endif
                if (entry.refCount == 0)
                {
                    freeEntryToRemove.key = key;
                    freeEntryToRemove.obj = entry.obj;
                    freeSet.Remove(freeEntryToRemove);
                }
                entry.refCount++;
                pool[key] = entry;
                return entry.obj;
            }
            freeEntryToRemove.key = key;
            freeEntryToRemove.obj = entry.obj;
            if (freeSet.Remove(freeEntryToRemove))
            {
                // reuseing pooled object
                entry.obj = freeEntryToRemove.obj;
#if DEBUG_POOL
                Console.WriteLine("ObjectPool: Reusing entry for {0}", key);
#endif
            }
            else
            {
                if (pool.Count >= maxCount)
                {
                    // max reached, start reusing entries
                    FreeEntry freeEntry = freeSet.First();
#if DEBUG_POOL
                    Console.WriteLine("ObjectPool: Reusing entry for {0}, old entry {1}", key, freeEntry.key);
#endif
                    if (!freeSet.Remove(freeEntry))
                    {
                        throw new InvalidOperationException("invalid pool state");
                    }
                    // need to remove pool entry ?
                    pool.Remove(freeEntry.key);

                    entry.obj = freeEntry.obj;
                    objectMutator(key, entry.obj);
                }
                else
                {
                    // create a new object
                    entry.obj = objectFactory(key);
#if DEBUG_POOL
                    Console.WriteLine("ObjectPool: Adding entry for {0}", key);
#endif
                }
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
                    pool[key] = entry;
#if DEBUG_POOL
                    //Console.WriteLine("ObjectPool: Freed {0}", key);
#endif
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
}
