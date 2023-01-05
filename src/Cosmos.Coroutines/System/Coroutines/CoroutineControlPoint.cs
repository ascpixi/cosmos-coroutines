using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cosmos.System.Coroutines
{
    /// <summary>
    /// Represents a coroutine control point - an object that can be yielded by a
    /// <see cref="CoroutineExecutor"/>, representing a halt in the execution of the coroutine.
    /// </summary>
    public abstract class CoroutineControlPoint
    {
        /// <summary>
        /// Whether the coroutine can continue with the execution.
        /// </summary>
        public abstract bool CanContinue { get; }
    }
}
