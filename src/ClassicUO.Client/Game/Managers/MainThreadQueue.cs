using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ClassicUO.Game.Managers;

public static class MainThreadQueue
{
    private static int _threadId;
    private static bool _isMainThread => Thread.CurrentThread.ManagedThreadId == _threadId;
    private static ConcurrentQueue<Action> _queuedActions { get; } = new();

    /// <summary>
    /// Must be called from main thread
    /// </summary>
    public static void Load() => _threadId = Thread.CurrentThread.ManagedThreadId;

    /// <summary>
    /// This will not wait for the action to complete.
    /// </summary>
    /// <param name="action"></param>
    public static void EnqueueAction(Action action) => _queuedActions.Enqueue(action);

    /// <summary>
    /// This will wait for the returned result.
    /// </summary>
    /// <param name="func"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T InvokeOnMainThread<T>(Func<T> func)
    {
        if (_isMainThread) return func();

        var resultEvent = new ManualResetEvent(false);
        T result = default;

        void action()
        {
            result = func();
            resultEvent.Set();
        }

        _queuedActions.Enqueue(action);
        resultEvent.WaitOne(); // Wait for the main thread to complete the operation

        return result;
    }

    /// <summary>
    /// This will not wait for the returned result.
    /// </summary>
    /// <param name="action"></param>
    public static void InvokeOnMainThread(Action action)
    {
        if (_isMainThread)
        {
            action();
            return;
        }

        _queuedActions.Enqueue(action);
    }

    /// <summary>
    /// Must only be called on the main thread
    /// </summary>
    public static void ProcessQueue()
    {
        while (_queuedActions.TryDequeue(out Action action))
        {
            action();
        }
    }

    public static void Reset()
    {
        while (_queuedActions.TryDequeue(out _)) { }
    }
}
