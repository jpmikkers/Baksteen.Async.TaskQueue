# Baksteen.Async.TaskQueue
TaskQueue is a lightweight C# async implementation of a FIFO execution queue. It enables async queueing of async work that will be executed in guaranteed first-in first-out (FIFO) order.
It is a fork of Gentlee's [SerialQueue](https://github.com/gentlee/SerialQueue), but in this case the implementation is fully async based and maybe a bit easier to understand.

### Interface

```C#
using Baksteen.Async;

class TaskQueue {
    Task Enqueue(Action action);
    Task<T> Enqueue<T>(Func<T> function);
    Task Enqueue(Func<Task> asyncAction);
    Task<T> Enqueue<T>(Func<Task<T>> asyncFunction);
}
```
    
### Example

```C#
readonly TaskQueue queue = new TaskQueue();

async Task SomeAsyncMethod()
{
    await queue.Enqueue(AsyncAction);
    
    var result = await queue.Enqueue(AsyncFunction);
}
```

### Troubleshooting

#### Deadlocks

Nesting and awaiting `queue.Enqueue` leads to deadlock in the queue:

```C#
var queue = new TaskQueue();

await queue.Enqueue(async () =>
{
  await queue.Enqueue(async () =>
  {
    // This code will never run because it waits until the first task executes,
    // and first task awaits while this one finishes.
    // Queue is locked.
  });
});
```
This particular case can be fixed by either not awaiting nested Enqueue or not putting nested task to queue at all, because it is already in the queue.
Overall it is better to implement code not synced first, but later sync it in the upper layer that uses that code, or in a synced wrapper.
