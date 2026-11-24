using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game;

/// <summary>
///     Covers the crash-recovery allowance: three recoveries inside the window, then no more until the window
///     passes. Static state is cleared around each test because the guard is process-wide.
/// </summary>
public class CrashRecoveryGuardTests
{
    public CrashRecoveryGuardTests() => CrashRecoveryGuard.Reset();

    [Fact]
    public void TryBeginRecovery_AllowsTheConfiguredNumberOfRecoveries()
    {
        for (int i = 0; i < CrashRecoveryGuard.MaxRecoveries; i++)
            CrashRecoveryGuard.TryBeginRecovery(i * 1_000).Should().BeTrue();

        CrashRecoveryGuard.TryBeginRecovery(CrashRecoveryGuard.MaxRecoveries * 1_000).Should().BeFalse();
    }

    [Fact]
    public void TryBeginRecovery_KeepsDenyingUntilTheWindowPasses()
    {
        for (int i = 0; i < CrashRecoveryGuard.MaxRecoveries; i++)
            CrashRecoveryGuard.TryBeginRecovery(i * 1_000).Should().BeTrue();

        CrashRecoveryGuard.TryBeginRecovery(CrashRecoveryGuard.WindowMs - 1).Should().BeFalse();
        CrashRecoveryGuard.TryBeginRecovery(CrashRecoveryGuard.WindowMs - 1).Should().BeFalse();
    }

    [Fact]
    public void TryBeginRecovery_StartsOverOnceTheWindowPasses()
    {
        for (int i = 0; i < CrashRecoveryGuard.MaxRecoveries; i++)
            CrashRecoveryGuard.TryBeginRecovery(i * 1_000).Should().BeTrue();

        CrashRecoveryGuard.TryBeginRecovery(CrashRecoveryGuard.WindowMs).Should().BeTrue();

        for (int i = 1; i < CrashRecoveryGuard.MaxRecoveries; i++)
            CrashRecoveryGuard.TryBeginRecovery(CrashRecoveryGuard.WindowMs + i * 1_000).Should().BeTrue();

        CrashRecoveryGuard.TryBeginRecovery(CrashRecoveryGuard.WindowMs * 2 - 1).Should().BeFalse();
    }
}
