using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ClassicUO.Game.Managers;

public static class MainThreadQueue
{
    private static int _threadId;
    private static bool _isMainThread => Thread.CurrentThread.ManagedThreadId == _threadId;
    private static ConcurrentQueue<(Action Action, CancellationToken? Token)> _queuedActions { get; } = new();

    /// <summary>
    ///     Must be called from main thread
    /// </summary>
    public static void Load() => _threadId = Thread.CurrentThread.ManagedThreadId;

    /// <summary>
    ///     This will not wait for the action to complete.
    ///     If a cancellation token is provided, the action will be skipped at execution time if cancelled.
    /// </summary>
    public static void EnqueueAction(Action action, CancellationToken? cancellationToken = null)
        => _queuedActions.Enqueue((action, cancellationToken));

    /// <summary>
    ///     Wraps the given function with a try/catch, returning any caught exception
    /// </summary>
    private static Func<(T, Exception)> WrapCallback<T>(Func<T> callback) =>
        () =>
        {
            try
            {
                return (callback(), null);
            }
            catch (Exception e)
            {
                return (default, e);
            }
        };

    /// <summary>
    ///     Dispatches the given function for invocation on the main thread and waits synchronously for the result
    /// </summary>
    private static T BubblingDispatchToMainThread<T>(Func<T> func, CancellationToken? cancellationToken = null)
    {
        // The MT is so slow there's no real point in spinning; Just wastes CPU.
        var resultEvent = new ManualResetEventSlim(false, 0);

        T mtResult = default;
        Exception ex = null;

        _queuedActions.Enqueue((MtAction, cancellationToken));

        // Wait for the main thread to complete the operation
        resultEvent.Wait(cancellationToken ?? CancellationToken.None);

        return ex != null ? throw ex : mtResult;

        void MtAction()
        {
            (T res, Exception e) = WrapCallback(func)();
            mtResult = res;
            ex = e;
            resultEvent.Set();
        }
    }

    /// <summary>
    ///     Dispatches a given function for execution on the MainThread.
    ///     If the current thread is the main thread, the function will run immediately as-is,
    ///     otherwise, the function will be dispatched and waited for.
    ///     Any exceptions raised on the main thread's context will be captured and bubbled back.
    ///     If a cancellation token is provided and already canceled, returns default without executing.
    /// </summary>
    /// <param name="func">The function to invoke on the main-thread</param>
    /// <param name="cancellationToken">
    ///     <para>
    ///         A token that can be used to cancel the task.
    ///     </para>
    ///     <para>
    ///         Note that once the task has started executing, cancellation will not interrupt it but will prevent further
    ///         waiting for the result.
    ///     </para>
    /// </param>
    /// <typeparam name="T">The type of result being returned</typeparam>
    /// <returns>The result of the given function, as returned from the main-thread</returns>
    public static T BubblingInvokeOnMainThread<T>(Func<T> func, CancellationToken? cancellationToken = null)
    {
        if (cancellationToken?.IsCancellationRequested == true) return default;
        return _isMainThread ? func() : BubblingDispatchToMainThread(func, cancellationToken);
    }

    /// <summary>
    ///     Dispatches a given function for execution on the MainThread.
    ///     If the current thread is the main thread, the function will run immediately as-is,
    ///     otherwise, the function will be dispatched and waited for.
    ///     Any exceptions raised on the main thread's context will be captured and bubbled back.
    ///     If a cancellation token is provided and already canceled, returns default without executing.
    /// </summary>
    /// <param name="action">The action to invoke on the main-thread</param>
    /// <param name="cancellationToken">
    ///     <para>
    ///         A token that can be used to cancel the task.
    ///     </para>
    ///     <para>
    ///         Note that once the task has started executing, cancellation will not interrupt it but will prevent further
    ///         waiting for the result.
    ///     </para>
    /// </param>
    public static void BubblingInvokeOnMainThread(Action action, CancellationToken? cancellationToken = null)
    {
        if (cancellationToken?.IsCancellationRequested == true)
            return;

        if (_isMainThread)
        {
            action();
            return;
        }

        // A fake return to fit the signature
        _ = BubblingDispatchToMainThread(() =>
            {
                action();
                return true;
            }, cancellationToken
        );
    }

    /// <summary>
    ///     This will wait for the returned result.
    ///     If a cancellation token is provided and already cancelled, returns default without executing.
    /// </summary>
    public static T InvokeOnMainThread<T>(Func<T> func, CancellationToken? cancellationToken = null)
    {
        if (cancellationToken?.IsCancellationRequested == true) return default;
        if (_isMainThread) return func();

        // The MT is so slow there's no real point in spinning; Just wastes CPU.
        var resultEvent = new ManualResetEventSlim(false, 0);
        T result = default;

        _queuedActions.Enqueue((Action, cancellationToken));

        try
        {
            // Wait for the main thread to complete the operation.
            // If the token is cancelled, Wait throws and we return default;
            // ProcessQueue will skip the action since the token is cancelled.
            resultEvent.Wait(cancellationToken ?? CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            return default;
        }
        catch (ThreadInterruptedException)
        {
            return default;
        }

        return result;

        void Action()
        {
            result = func();
            resultEvent.Set();
        }
    }

    /// <summary>
    ///     This will not wait for the returned result.
    ///     If a cancellation token is provided, the action is skipped at execution time if canceled.
    /// </summary>
    public static void InvokeOnMainThread(Action action, CancellationToken? cancellationToken = null)
    {
        if (cancellationToken?.IsCancellationRequested == true) return;
        if (_isMainThread)
        {
            action();
            return;
        }

        _queuedActions.Enqueue((action, cancellationToken));
    }

    /// <summary>
    ///     Must only be called on the main thread
    /// </summary>
    public static void ProcessQueue()
    {
        while (_queuedActions.TryDequeue(out (Action Action, CancellationToken? Token) item))
            if (item.Token?.IsCancellationRequested != true)
                item.Action();
    }

    public static void Reset()
    {
        while (_queuedActions.TryDequeue(out _)) { }
    }
}
