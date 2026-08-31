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
/// queue, for code that refuses to run its real work anywhere else.
/// <para>
/// Any test touching such code has to join <see cref="MainThreadCollection" />, and has to run the
/// part that cares through <see cref="Invoke" /> - xUnit's own thread is never the main thread here,
/// so work started from it is deferred onto this one and completes whenever it completes.
/// </para>
/// </summary>
public class MainThreadFixture : IDisposable
{
    private const int INVOKE_TIMEOUT_SECONDS = 10;

    private readonly Thread _mt;
    private readonly ManualResetEventSlim _resetEvent = new();

    private bool _disposed;

    public MainThreadFixture()
    {
        _mt = new Thread(Run) { Name = "Test Main Thread" };
        _mt.Start();
    }

    /// <summary>
    /// Runs <paramref name="action" /> on the main thread and waits for it to finish, so a test can
    /// assert on what it did rather than on whether it has got round to it yet.
    /// </summary>
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

        // Rethrown with its original stack, so a failed assertion inside the action reads as itself
        // rather than as something this fixture did.
        capturedFailure?.Throw();
    }

    private void Run()
    {
        MainThreadQueue.Load();

        while (!_resetEvent.IsSet)
            MainThreadQueue.ProcessQueue();
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _resetEvent.Set();
        _mt.Join();
        GC.SuppressFinalize(this);
    }
}
