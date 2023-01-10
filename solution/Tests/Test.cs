namespace Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Baksteen.Async;

[TestClass]
public class Test
{
    [TestMethod]
    public async Task TplIsNotFifo()
    {
        // Assign

        var list = new List<int>();
        var tasks = new List<Task>();
        var range = Enumerable.Range(0, 5);

        // Act

        foreach (var number in range)
        {
            tasks.Add(Task.Factory.StartNew(() => {
                Thread.Sleep(500 - number * 100);
                lock(list) list.Add(number);
            }, TaskCreationOptions.PreferFairness));
        }

        await Task.WhenAll(tasks);

        // Assert

        Assert.IsFalse(range.SequenceEqual(list));
    }

    [TestMethod]
    public async Task EnqueueAction()
    {
        // Assign

        var queue = new TaskQueue();
        var list = new List<int>();
        var tasks = new List<Task>();
        var range = Enumerable.Range(0, 5);

        // Act

        foreach (var number in range)
        {
            tasks.Add(queue.Enqueue(() => { 
                Thread.Sleep(500 - number*100); 
                lock(list) list.Add(number); 
            }));
        }

        await Task.WhenAll(tasks);

        // Assert

        Assert.IsTrue(range.SequenceEqual(list));
    }

    [TestMethod]
    public async Task EnqueueFunction()
    {
        // Assign

        var queue = new TaskQueue();
        var tasks = new List<Task<int>>();
        var range = Enumerable.Range(0, 5);

        // Act

        foreach (var number in range)
        {
            tasks.Add(queue.Enqueue(() => {
                Thread.Sleep(500 - number * 100);
                return number;
            }));
        }

        await Task.WhenAll(tasks);

        // Assert

        Assert.IsTrue(tasks.Select(x => x.Result).SequenceEqual(range));
    }

    [TestMethod]
    public async Task EnqueueAsyncAction()
    {
        // Assign

        var queue = new TaskQueue();
        var list = new List<int>();
        var tasks = new List<Task>();
        var range = Enumerable.Range(0, 5);

        // Act

        foreach (var number in range)
        {
            tasks.Add(queue.Enqueue(async () => {
                await Task.Delay(500 - number * 100);
                lock(list) list.Add(number);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert

        Assert.IsTrue(range.SequenceEqual(list));
    }

    [TestMethod]
    public async Task EnqueueAsyncFunction()
    {
        // Assign

        var queue = new TaskQueue();
        var tasks = new List<Task<int>>();
        var range = Enumerable.Range(0, 5);

        // Act

        foreach (var number in range)
        {
            tasks.Add(queue.Enqueue(async () => {
                await Task.Delay(500 - number * 100);
                return number;
            }));
        }

        await Task.WhenAll(tasks);

        // Assert

        Assert.IsTrue(tasks.Select(x => x.Result).SequenceEqual(range));
    }

    [TestMethod]
    public async Task EnqueueMixed()
    {
        // Assign

        var queue = new TaskQueue();
        var list = new List<int>();
        var tasks = new List<Task>();
        var range = Enumerable.Range(0, 100);

        // Act

        foreach (var number in range)
        {
            if (number % 4 == 0)
            {
                tasks.Add(queue.Enqueue(() => {
                    Thread.Sleep(Random.Shared.Next(10));
                    lock(list) list.Add(number);
                }));
            }
            else if (number % 3 == 0)
            {
                tasks.Add(queue.Enqueue(() => {
                    Thread.Sleep(Random.Shared.Next(10));
                    lock(list) list.Add(number);
                    return number;
                }));
            }
            else if (number % 2 == 0)
            {
                tasks.Add(queue.Enqueue(async () => {
                    await Task.Delay(Random.Shared.Next(10));
                    lock(list) list.Add(number);
                }));
            }
            else
            {
                tasks.Add(queue.Enqueue(async () => {
                    await Task.Delay(Random.Shared.Next(10));
                    lock(list) list.Add(number);
                    return number;
                }));
            }
        }

        await Task.WhenAll(tasks);

        // Assert

        Assert.IsTrue(range.SequenceEqual(list));
    }

    [TestMethod]
    public void EnqueueFromMultipleThreads()
    {
        // Assign

        const int count = 10000;
        var queue = new TaskQueue();
        var list = new List<int>();

        // Act

        var counter = 0;
        for (int i = 0; i < count; i++)
        {
            Task.Run(() => {
                queue.Enqueue(() => { lock(list) list.Add(counter++); });
            });
        }

        while (list.Count != count) { };

        // Assert

        Assert.IsTrue(list.SequenceEqual(Enumerable.Range(0, count)));
    }

    [TestMethod]
    public async Task CatchExceptionFromAction()
    {
        // Assign

        var queue = new TaskQueue();
        var exceptionCatched = false;

        // Act

        await queue.Enqueue(() => Thread.Sleep(10));
        try
        {
            await queue.Enqueue(() => throw new Exception("Test"));
        }
        catch (Exception e)
        {
            if (e.Message == "Test")
            {
                exceptionCatched = true;
            }
        }

        // Assert

        Assert.IsTrue(exceptionCatched);
    }

    [TestMethod]
    public async Task CatchExceptionFromAsyncAction()
    {
        // Assign

        var queue = new TaskQueue();
        var exceptionCatched = false;

        // Act

        await queue.Enqueue(() => Thread.Sleep(10));
        try
        {
            await queue.Enqueue(async () => {
                await Task.Delay(50);
                throw new Exception("Test");
            });
        }
        catch (Exception e)
        {
            if (e.Message == "Test")
            {
                exceptionCatched = true;
            }
        }

        // Assert

        Assert.IsTrue(exceptionCatched);
    }


    [TestMethod]
    public async Task CatchExceptionFromAsyncFunction()
    {
        // Assign

        var queue = new TaskQueue();
        var exceptionCatched = false;

        // Act

        await queue.Enqueue(() => Thread.Sleep(10));
        try
        {
            await queue.Enqueue(asyncFunction: async () => {
                await Task.Delay(50);
                throw new Exception("Test");
#pragma warning disable CS0162 // Unreachable code detected
                return false;
#pragma warning restore CS0162 // Unreachable code detected
            });
        }
        catch (Exception e)
        {
            if (e.Message == "Test")
            {
                exceptionCatched = true;
            }
        }

        // Assert

        Assert.IsTrue(exceptionCatched);
    }
}