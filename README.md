# cosmos-coroutines
A simple, non-preemptive coroutine scheduler that allows for cooperative multitasking within Cosmos kernels.

This project was created to demonstrate the ability to use C#'s iterator support to achieve cooperative multitasking.

## Limitations
Cosmos.Coroutines has the following limitations:
- non-preemptive; you need to do a `yield return` to hand back control to the coroutine scheduler
- basic round-robin; no priority system
- CPU halting - which occurs in many parts of Cosmos - will also halt the coroutine pool scheduler

Other than the caveats mentioned above, the coroutine system can act like a cooperative kernel task scheduler.

## Installing
Cosmos.Coroutines is available on [NuGet](https://www.nuget.org/packages/Cosmos.Coroutines); either use the NuGet package manager in your IDE of choice, or, in a package manager terminal, type in:
```powershell
NuGet\Install-Package Cosmos.Coroutines -Version 1.0.1
```

## Usage
The following classes are included in the `Cosmos.System.Coroutines` namespace:
- `Coroutine` - represents a coroutine, which can belong to only one `CoroutinePool`.
- `CoroutinePool` - manages multiple coroutines. A global `CoroutinePool` is allocated on startup and can be accessed using `CoroutinePool.Main`. This pool will not affect the execution of the OS in any way until the `StartPool` instance method is called.
- `CoroutineControlPoint` - an object returned by valid coroutine implementations of the `IEnumerator` interface, and accepted by the `Coroutine` constructor. Specifies whether the coroutine should be ticked at a given time.
- `WaitFor` - a `CoroutineControlPoint` that waits for the specified amount of nanoseconds.
- `WaitUntil` - a `CoroutineControlPoint` that waits until a given condition is met.
- `WaitIndefinetly` - a `CoroutineControlPoint` that halts the coroutine until it's explicitly un-halted through said control point.

To use the main `CoroutinePool`, simply do:
```cs
CoroutinePool.Main.StartPool();
```

To create a coroutine:
```cs
var coroutine = new Coroutine(MyCoroutine1());
coroutine.Start(); // will run the coroutine on the main pool; to run it in another, use CoroutinePool.AddCoroutine

// ...

IEnumerator<CoroutineControlPoint> MyCoroutine1()
{
    while(true) {
        Console.WriteLine("This prints every second.");
        yield return WaitFor.Seconds(1);
    }
}
```

You can start as many coroutines as you want, however, please note that with more coroutines, the slower the operating system gets.

> [!WARNING]
> A coroutine is not the same as a traditional C# thread, and you should not mistake the two. A C# thread is **preempted**; that is, if the thread encounters, for example, an infinite loop, the kernel will still continue to execute, as the thread will be automatically switched from (preempted) after a time quantum. A coroutine relies on the method to voluntarily give back control to the pool; if a software bug appears that would make the coroutine refrain from giving back control to the pool, the kernel would halt.

### Creating a "main" function
After performing a cycle over all coroutines, you may want to execute kernel code, to perform e.g. maintanance tasks. This can be easily achieved using the `CoroutinePool.OnCoroutineCycle` delegate list:
```cs
CoroutinePool.Main.OnCoroutineCycle.Add(Main);
CoroutinePool.Main.StartPool();

// ...

void Main() {
    // everything in this method will be executed after a pool cycle
}
```

This is a list of delegates instead of a standard C# event, as these are currently non-functional on Cosmos. [See this issue for more details.](https://github.com/CosmosOS/Cosmos/issues/2765)

## Coroutines and memory management
`CoroutinePool`s can be set to automatically collect all unused objects on the heap after the executor finishes a cycle - that is, when all coroutines in its internal list have been ticked. This is enabled by default for the main pool, but disabled for user-created pools. It's strongly recommended to enable periodic heap collection if you're running the pool on your main thread (as is most likely the case with Cosmos).
