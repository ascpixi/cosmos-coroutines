using Cosmos.Core;
using Cosmos.Core.Memory;
using Cosmos.Debug.Kernel;
using Cosmos.HAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cosmos.System.Coroutines
{
    /// <summary>
    /// Represents a pool of coroutines that can be executed in a single thread.
    /// </summary>
    public class CoroutinePool
    {
        readonly List<Coroutine> coroutines = new();
        readonly Queue<Coroutine> coroutinesToRemove = new();
        bool started = false;
        bool performHeapCollection = false; // do not perform heap collection by default
        bool shouldCollectOnNextCycle;
        ulong heapCollectionIntervalNs = 250000000; // 250ms
        PIT.PITTimer? heapCollectionTimer;

        /// <summary>
        /// The main global coroutine pool.
        /// </summary>
        public static CoroutinePool Main { get; set; } = new()
        {
            // if the main coroutine pool will be started, do call Heap.Collect() in semi-regular intervals
            PerformHeapCollection = true
        };

        /// <summary>
        /// Fired every time a full coroutine execution cycle is performed.
        /// </summary>
        public event Action? OnCoroutineCycle;

        /// <summary>
        /// Whether the pool should perform heap collection after coroutine cycles.
        /// </summary>
        // Recommended when the pool is ran on the main Cosmos thread. Can be safely
        // disabled if you are handling heap memory collection yourself.
        public bool PerformHeapCollection {
            get => performHeapCollection;
            set {
                performHeapCollection = value;

                if(started) {
                    if (performHeapCollection) {
                        CreateHeapCollectionTimer();
                    } else {
                        DestroyHeapCollectionTimer();
                    }
                } 
            }
        }

        /// <summary>
        /// The interval, in nanoseconds, in which heap collection should be performed.
        /// Heap collection will always be performed after a full coroutine cycle is performed.
        /// </summary>
        public ulong HeapCollectionInterval {
            get => heapCollectionIntervalNs;
            set {
                if(value != heapCollectionIntervalNs) {
                    heapCollectionIntervalNs = value;

                    if(started) {
                        CreateHeapCollectionTimer(); // create a new timer with the correct interval
                    }
                }
            }
        }

        private void CreateHeapCollectionTimer()
        {
            if(heapCollectionTimer != null) {
                DestroyHeapCollectionTimer();
            }

            heapCollectionTimer = new PIT.PITTimer(NotifyHeapCollectionTimer, HeapCollectionInterval, true);
            HAL.Global.PIT.RegisterTimer(heapCollectionTimer);
        }

        private void DestroyHeapCollectionTimer()
        {
            if(heapCollectionTimer != null) {
                HAL.Global.PIT.UnregisterTimer(heapCollectionTimer.TimerID);
                heapCollectionTimer.Dispose();
                heapCollectionTimer = null;
            }
        }

        private void NotifyHeapCollectionTimer()
        {
            shouldCollectOnNextCycle = true;
        }

        /// <summary>
        /// The running coroutines in this pool.
        /// </summary>
        public IEnumerable<Coroutine> RunningCoroutines => coroutines;

        /// <summary>
        /// Adds a coroutine to the pool.
        /// </summary>
        /// <param name="coroutine">The coroutine to add to the pool.</param>
        public void AddCoroutine(Coroutine coroutine)
        {
            coroutines.Add(coroutine);
            coroutine.Join(this);
            coroutine.Running = true;
        }

        /// <summary>
        /// Removes a coroutine from the pool, effectively halting its execution
        /// in the this <see cref="CoroutinePool"/>.
        /// </summary>
        /// <param name="coroutine">The coroutine to remove from the pool.</param>
        public void RemoveCoroutine(Coroutine coroutine)
        {
            coroutinesToRemove.Enqueue(coroutine);
        }

        /// <summary>
        /// Starts the coroutine execution loop in the current thread.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when an attempt is made to start the coroutine pool while it's already running.</exception>
        public void StartPool()
        {
            if(started) {
                throw new InvalidOperationException("The coroutine pool has already been started.");
            }

            if(heapCollectionTimer == null) {
                CreateHeapCollectionTimer();
            }

            started = true;

            while(true) {
                for (int i = coroutines.Count - 1; i >= 0; i--) {
                    var current = coroutines[i];

                    if(current.ExecutionEnded) {
                        coroutines.RemoveAt(i);
                        current.Exit();
                        current.Running = false;
                        continue;
                    }

                    if (current.Halted) {
                        continue;
                    }

                    if (current.CurrentControlPoint == null || current.CurrentControlPoint.CanContinue) {
                        current.Step();
                    }
                }

                foreach (var coroutine in coroutinesToRemove) {
                    if(coroutines.Remove(coroutine)) {
                        coroutine.Exit();
                    }

                    coroutine.Running = false;
                }

                OnCoroutineCycle?.Invoke();

                if(shouldCollectOnNextCycle) {
                    Heap.Collect();
                }
            }
        }
    }
}
