using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ClassicUO.Game.Managers;

public static class MainThreadQueue
{
    public static ConcurrentQueue<Action> QueuedActions { get; } = new();

    /// <summary>
    /// This will not wait for the action to complete.
    /// </summary>
    /// <param name="action"></param>
    public static void EnqueueAction(Action action)
    {
        QueuedActions.Enqueue(action);
    }

    /// <summary>
    /// This will wait for the returned result.
    /// </summary>
    /// <param name="func"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T InvokeOnMainThread<T>(Func<T> func)
    {
        var resultEvent = new ManualResetEvent(false);
        T result = default;

        void action()
        {
            result = func();
            resultEvent.Set();
        }

        QueuedActions.Enqueue(action);
        resultEvent.WaitOne(); // Wait for the main thread to complete the operation

        return result;
    }

    /// <summary>
    /// This will wait for the returned result.
    /// </summary>
    /// <param name="action"></param>
    public static void InvokeOnMainThread(Action action)
    {
        var resultEvent = new ManualResetEvent(false);

        void wrappedAction()
        {
            action();
            resultEvent.Set();
        }

        QueuedActions.Enqueue(wrappedAction);
        resultEvent.WaitOne();
    }

    public static void ProcessQueue()
    {
        while (QueuedActions.TryDequeue(out var action))
        {
            action();
        }
    }

    public static void Reset()
    {
        while (QueuedActions.TryDequeue(out _)) { }
    }
}
