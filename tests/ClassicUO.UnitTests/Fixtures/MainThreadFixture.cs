using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using ClassicUO.Game.Managers;
using Xunit;

namespace ClassicUO.UnitTests.Fixtures;

// ReSharper disable ClassNeverInstantiated.Global

[CollectionDefinition(Name)]
public class MainThreadCollection : ICollectionFixture<MainThreadFixture>
{
    public const string Name = "MainThread collection";
}

/// <summary>
/// Stands up the one thread <see cref="MainThreadQueue" /> treats as the main thread and pumps its
/// queue.
/// <para>
/// A test touching such code joins <see cref="MainThreadCollection" /> and runs the part that cares
/// through <see cref="Invoke" />: xUnit's own thread is never the main thread here, so work started
/// from it is deferred onto this one.
/// </para>
/// </summary>
public class MainThreadFixture : IDisposable
{
    private const int INVOKE_TIMEOUT_SECONDS = 10;

    /// <summary>Wait between empty passes, so the pump does not spin on a core for the whole run.</summary>
    private const int IDLE_POLL_MILLISECONDS = 1;

    /// <summary>Bounds shutdown, so a pump that will not stop fails the run rather than hanging it.</summary>
    private const int SHUTDOWN_TIMEOUT_SECONDS = 5;

    private readonly Thread _mt;
    private readonly ManualResetEventSlim _resetEvent = new();

    private bool _disposed;

    public MainThreadFixture()
    {
        // Background, so an undisposed fixture cannot keep the test process alive.
        _mt = new Thread(Run) { Name = "Test Main Thread", IsBackground = true };
        _mt.Start();
    }

    /// <summary>Runs <paramref name="action" /> on the main thread and waits for it to finish.</summary>
    /// <param name="action">The work to run.</param>
    /// <exception cref="TimeoutException">The main thread did not run it in time.</exception>
    public void Invoke(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        using var completed = new ManualResetEventSlim();
        ExceptionDispatchInfo capturedFailure = null;

        MainThreadQueue.EnqueueAction(() =>
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                capturedFailure = ExceptionDispatchInfo.Capture(e);
            }
            finally
            {
                completed.Set();
            }
        });

        if (!completed.Wait(TimeSpan.FromSeconds(INVOKE_TIMEOUT_SECONDS)))
            throw new TimeoutException($"Main thread did not run the action within {INVOKE_TIMEOUT_SECONDS}s.");

        // Original stack, so a failed assertion inside the action reads as itself.
        capturedFailure?.Throw();
    }

    private void Run()
    {
        MainThreadQueue.Load();

        while (!_resetEvent.IsSet)
        {
            MainThreadQueue.ProcessQueue();
            _resetEvent.Wait(IDLE_POLL_MILLISECONDS);
        }

        // One last pass: work enqueued during teardown still has a caller waiting on it.
        MainThreadQueue.ProcessQueue();
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _resetEvent.Set();

        if (!_mt.Join(TimeSpan.FromSeconds(SHUTDOWN_TIMEOUT_SECONDS)))
            throw new TimeoutException($"Main thread did not stop within {SHUTDOWN_TIMEOUT_SECONDS}s.");

        GC.SuppressFinalize(this);
    }
}
