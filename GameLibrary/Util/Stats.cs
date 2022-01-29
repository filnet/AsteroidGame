using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GameLibrary.Util
{
    public class Stats : IDisposable
    {
        class Stat
        {
            internal readonly string name;

            internal int count;
            internal long total;
            internal long min;
            internal long max;

            private readonly object syncLock = new object();        

            internal Stat(string name)
            {
                this.name = name;
                count = 0;
                total = 0;
                min = long.MaxValue;
                max = long.MinValue;
            }

            internal void Add(long t)
            {
                lock (syncLock)
                {
                    count++;
                    total += t;
                    min = Math.Min(min, t);
                    max = Math.Max(max, t);
                }
            }

            public long Elapsed { get { return total;  } }

            internal void Log()
            {
                long frequency = Stopwatch.Frequency;
                float msPerTick = 1000 / (float)frequency;

                float minMs;
                float maxMs;
                float avgMs;
                float totalMs;
                int c;
                lock (syncLock)
                {
                    float avg = (total == 0) ? 0 : total / count;
                    minMs = min * msPerTick;
                    maxMs = max * msPerTick;
                    avgMs = avg * msPerTick;
                    totalMs = total * msPerTick;
                    c = count;
                }
                Console.WriteLine("{0} {1:F2}ms/{2:F2}ms/{3:F2}ms ({4} {5:F2}ms)",
                    name, minMs, avgMs, maxMs, c, totalMs);
            }
        }

        struct StackEntry
        {
            public Stat stat;
            public Stopwatch stopwatch;
        }

        private static readonly ConcurrentDictionary<string, Stat> entries = new ConcurrentDictionary<string, Stat>();
        private static readonly ConcurrentStack<Stopwatch> stopwatchPool = new ConcurrentStack<Stopwatch>();
        private static readonly ThreadLocal<Stack<StackEntry>> stackThreadLocal = new ThreadLocal<Stack<StackEntry>>(() =>
        {
            return new Stack<StackEntry>();
        });

        private static readonly Stats Instance = new Stats();

        private Stats() { }

        public void Dispose()
        {
            Stop();
        }

        public static Stats Use(string name)
        {
            Start(name);
            return Instance;
        }

        public static TimeSpan Elapsed(string name)
        {
            return new TimeSpan(Get(name).Elapsed);
        }

        private static Stat Get(string name)
        {
            if (!entries.TryGetValue(name, out Stat stat))
            {
                stat = new Stat(name);
                if (!entries.TryAdd(name, stat))
                {
                    // some other thread was faster...
                    stat = entries[name];
                }
            }
            return stat;
        }

        private static void Start(string name)
        {
            if (!stopwatchPool.TryPop(out Stopwatch stopwatch))
            {
                stopwatch = new Stopwatch();
            }

            Stack<StackEntry> stack = stackThreadLocal.Value;

            StackEntry entry;
            entry.stat = Get(name);            
            entry.stopwatch = stopwatch;
            stack.Push(entry);

            stopwatch.Restart();
        }

        private static void Stop()
        {
            Stack<StackEntry> stack = stackThreadLocal.Value;
            StackEntry entry = stack.Pop();
            entry.stopwatch.Stop();
            entry.stat.Add(entry.stopwatch.ElapsedTicks);
            // give back stopwatch (must be done when the stopwatch is not needed anymore)
            stopwatchPool.Push(entry.stopwatch);
        }

        public static void Log()
        {
            Log("");
        }

        public static void Log(string prefix)
        {
            foreach (Stat stat in entries.Values)
            {
                if (stat.name.StartsWith(prefix))
                {
                    stat.Log();
                }

            }
        }
    }

}
