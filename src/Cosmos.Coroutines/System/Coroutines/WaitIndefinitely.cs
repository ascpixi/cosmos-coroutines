using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cosmos.System.Coroutines
{
    /// <summary>
    /// When yielded from a coroutine executor, halts the execution of the
    /// coroutine until the <see cref="Continue"/> method is called on the object.
    /// </summary>
    public class WaitIndefinitely : CoroutineControlPoint
    {
        bool canContinue = false;

        /// <summary>
        /// Invalidates this wait and makes the coroutine able to execute instructions again.
        /// </summary>
        public void Continue()
        {
            canContinue = true;
        }

        public override bool CanContinue => canContinue;
    }
}
