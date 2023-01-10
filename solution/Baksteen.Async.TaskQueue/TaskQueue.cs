namespace Baksteen.Async;

using System;
using System.Threading;
using System.Threading.Tasks;

public class TaskQueue
{
    private Task _previousTask = Task.CompletedTask;

    public Task Enqueue(Action action)
    {
        return Enqueue(() => { action(); return true; });
    }

    public Task<T> Enqueue<T>(Func<T> function)
    {
        return Enqueue(() => Task.Run(function));
    }

    public Task Enqueue(Func<Task> asyncAction)
    {
        return Enqueue(async () => { 
            await asyncAction().ConfigureAwait(false);
            return true; 
        });
    }

    public async Task<T> Enqueue<T>(Func<Task<T>> asyncFunction)
    {
        // see https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // get predecessor and wait until it's done. Also atomically swap in our own completion task.
        await Interlocked.Exchange(ref _previousTask, tcs.Task).ConfigureAwait(false);
        try
        {
            return await asyncFunction().ConfigureAwait(false);
        }
        finally
        {
            tcs.SetResult();
        }
    }
}
