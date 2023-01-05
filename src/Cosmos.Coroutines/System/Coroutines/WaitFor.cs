using Cosmos.HAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cosmos.System.Coroutines
{
    /// <summary>
    /// When yielded from a coroutine executor, waits for the given amount of seconds.
    /// </summary>
    public class WaitFor : CoroutineControlPoint
    {
        bool canContinue = false;
        PIT.PITTimer timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitFor"/> class that can be used to
        /// halt the execution of a coroutine for the given amount of time.
        /// </summary>
        /// <param name="nanoseconds">The amount of nanoseconds to wait for.</param>
        public WaitFor(ulong nanoseconds)
        {
            timer = new PIT.PITTimer(TimerElapsedCallback, nanoseconds, false);
            HAL.Global.PIT.RegisterTimer(timer);
        }

        /// <summary>
        /// Returns a <see cref="WaitFor"/> object that can be yielded from a coroutine
        /// executor to halt the execution of the coroutine for the given amount of nanoseconds.
        /// </summary>
        /// <param name="ns">The amount of nanoseconds to halt the coroutine for.</param>
        public static WaitFor Nanoseconds(ulong ns) => new(ns);

        /// <summary>
        /// Returns a <see cref="WaitFor"/> object that can be yielded from a coroutine
        /// executor to halt the execution of the coroutine for the given amount of milliseconds.
        /// </summary>
        /// <param name="ms">The amount of milliseconds to halt the coroutine for.</param>
        public static WaitFor Milliseconds(uint ms) => new(ms * 1000000);

        /// <summary>
        /// Returns a <see cref="WaitFor"/> object that can be yielded from a coroutine
        /// executor to halt the execution of the coroutine for the given amount of seconds.
        /// </summary>
        /// <param name="s">The amount of seconds to halt the coroutine for.</param>
        public static WaitFor Seconds(uint s) => new(s * 1000 * 1000000);

        /// <summary>
        /// Returns a <see cref="WaitFor"/> object that can be yielded from a coroutine
        /// executor to halt the execution of the coroutine for the given amount of minutes.
        /// </summary>
        /// <param name="m">The amount of minutes to halt the coroutine for.</param>
        public static WaitFor Minutes(uint m) => Seconds(m * 60);

        private void TimerElapsedCallback()
        {
            canContinue = true;
        }

        public override bool CanContinue => canContinue;
    }
}
