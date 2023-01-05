using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cosmos.System.Coroutines
{
    /// <summary>
    /// Represents a coroutine, that generalizes subroutines for non-preemptive multitasking, by allowing execution to be suspended and resumed.
    /// </summary>
    public class Coroutine
    {
        readonly IEnumerator<CoroutineControlPoint?> executor;

        /// <summary>
        /// Initializes a new instance of the <see cref="Coroutine"/> class,
        /// with the given coroutine executor, without starting the coroutine.
        /// </summary>
        /// <param name="executor">The executor to call.</param>
        public Coroutine(IEnumerator<CoroutineControlPoint?> executor)
        {
            this.executor = executor;
        }

        internal void Join(CoroutinePool pool)
        {
            Pool = pool;
        }

        internal void Exit()
        {
            Pool = null;
        }

        internal void Step()
        {
            ExecutionEnded = !executor.MoveNext();
        }

        /// <summary>
        /// Starts the execution of this coroutine on the global coroutine pool.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when an attempt is made to start the coroutine while it's already running.</exception>
        public void Start()
        {
            if(Running) {
                throw new InvalidOperationException("Cannot start an already running Coroutine.");
            }

            CoroutinePool.Main.AddCoroutine(this);
        }

        /// <summary>
        /// Stops this coroutine.
        /// </summary>
        public void Stop()
        {
            Pool?.RemoveCoroutine(this);
            Exit();
        }

        /// <summary>
        /// Returns the current control point of the coroutine.
        /// </summary>
        public CoroutineControlPoint? CurrentControlPoint => executor.Current;

        /// <summary>
        /// Whether the coroutine is attached to a coroutine pool and is running.
        /// </summary>
        public bool Running { get; internal set; }

        /// <summary>
        /// Whether the coroutine is currently halted.
        /// </summary>
        public bool Halted { get; set; } = false;

        /// <summary>
        /// Gets the pool that this <see cref="Coroutine"/> belongs to.
        /// </summary>
        public CoroutinePool? Pool { get; private set; }

        /// <summary>
        /// Whether the execution of the coroutine has ended and no more
        /// instructions can be performed.
        /// </summary>
        public bool ExecutionEnded { get; private set; }
    }
}
