# cosmos-coroutines
A simple, non-preemptive coroutine scheduler that allows for cooperative multitasking within Cosmos kernels.

This project is mainly to demonstrate the ability to use the C# iterator feature to achieve coopertive multi-tasking; in itself, it's very simplistic - the whole implementation is contained in only 6 files.

## Limitations
Cosmos.Coroutines has the following limitations:
- non-preemptive; you need to do a `yield return` to hand back control over to the coroutine scheduler
- basic round-robin; no prority system
- CPU halting in many parts of Cosmos will also halt the coroutine pool scheduler

Other than this, it can act as a normal cooperative kernel task scheduler.

## Installing
To install Cosmos.Coroutines, you can use NuGet; either use the NuGet package manager in your IDE of choice, or, in a package manager terminal, type in:
```powershell
NuGet\Install-Package Cosmos.Coroutines -Version 1.0.0
```

## Usage
After importing the library to your project, you will be able to include a `using Cosmos.System.Coroutines` directive. This will result in the following classes being available to you:
- `Coroutine` - represents a single coroutine. A coroutine can belong to only one `CoroutinePool`.
- `CoroutinePool` - manages multiple coroutines and schedules stepping through iterations (`yield`s). A global `CoroutinePool` is allocated on startup and can be accessed using `CoroutinePool.Main`. This pool will not affect the execution of the OS in any way until `StartPool` will be called.
- `CoroutineControlPoint` - the object returned by `IEnumerator`s that the `Coroutine` constructor accepts. Specifies whether the coroutine should be ticked at a given time.
- `WaitFor` - a `CoroutineControlPoint` that waits for the specified amount of nanoseconds.
- `WaitUntil` - a `CoroutineControlPoint` that waits until a condition is met.
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

You can start as many coroutines as you want, however please note that with more coroutines, the slower the operating system gets.

> **Warning**<br>
> A coroutine is not the same as a traditional C# thread; and you should not mistake the two. A C# thread is **preempted**; that is, if the thread will encounter, for example, an infinite loop, the kernel will still continue to execute, as the thread will be automatically switched from (preempted) after a time quantum. A coroutine relies on the method to volountarily give back control to the pool; if a software bug appears that would make the coroutine refrain from giving back control over to the pool, the kernel would halt.

### Creating a "main" function
Sometimes, after performing a cycle over all coroutines, you want to execute kernel code to perform e.g. maintanance tasks. This can be easily achieved using the `CoroutinePool.OnCoroutineCycle` event:
```cs
CoroutinePool.OnCoroutineCycle += Main;
CoroutinePool.Main.StartPool();

// ...

void Main() {
    // everything in this method will be executed after a pool cycle
}
```

As this is a regular C# event, you can have as many handlers as you want.

## Coroutines and memory management
`CoroutinePool`s can be set to automatically collect all unused objects on the heap after all coroutines finish a single cycle. This is enabled by default for the main pool, but disabled for user-created pools. It's strongly recommended to enable periodic heap collection if you're running the pool on your main thread (most likely the case with Cosmos).