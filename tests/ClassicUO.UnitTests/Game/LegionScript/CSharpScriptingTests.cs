using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClassicUO.LegionScripting;
using ClassicUO.LegionScripting.ApiClasses;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Xunit;

namespace ClassicUO.UnitTests.Game.LegionScript;

/// <summary>
/// Executes real C# scripts through Roslyn scripting against the <see cref="LegionAPI"/>,
/// replicating the proxy-global wrapper that <see cref="ScriptFile.SetupCSharpScript"/> injects.
/// </summary>
[Collection(MainThreadCollection.Name)]
public class CSharpScriptingTests : IDisposable
{
    private readonly LegionAPI _api;
    private readonly ScriptOptions _options;

    public CSharpScriptingTests()
    {
        Client.UnitTestingActive = true;
        _api = new LegionAPI(new CSharpCallbackChannel(), null);

        _options = ScriptOptions.Default
            .WithReferences(
                typeof(object).Assembly,
                typeof(Enumerable).Assembly,
                typeof(List<>).Assembly,
                typeof(LegionAPI).Assembly)
            .WithImports(
                "System",
                "System.Linq",
                "System.Collections.Generic",
                "ClassicUO.LegionScripting",
                "ClassicUO.LegionScripting.ApiClasses");
    }

    public void Dispose() => _api.Dispose();

    private static string Wrap(string userCode) => $$"""
        global using static LegionAPIProxy;

        public static class LegionAPIProxy
        {
            public static LegionAPI API { get; set; }
        }

        LegionAPIProxy.API = GlobalApiInstance;

        {{userCode}}
        """;

    private Task<ScriptState<object>> RunAsync(string code) =>
        CSharpScript.RunAsync(Wrap(code), _options, new ScriptGlobals { GlobalApiInstance = _api });

    private Task<ScriptState<T>> RunAsync<T>(string code) =>
        CSharpScript.RunAsync<T>(Wrap(code), _options, new ScriptGlobals { GlobalApiInstance = _api });

    [Fact]
    public async Task CSharpScript_CanCallApi_AndReturnValue()
    {
        ScriptState<int> state = await RunAsync<int>("API.KnownAbilityNames().Length");

        state.ReturnValue.Should().Be(_api.KnownAbilityNames().Length);
    }

    [Fact]
    public async Task CSharpScript_SharedVars_RoundTripBetweenExecutions()
    {
        _api.ClearSharedVars();
        await RunAsync("API.SetSharedVar(\"class\", \"Warrior\");");

        ScriptState<string> state = await RunAsync<string>("return (string)API.GetSharedVar(\"class\");");

        state.ReturnValue.Should().Be("Warrior");
    }

    [Fact]
    public async Task CSharpScript_CanRunStatements_AndComputeResult()
    {
        ScriptState<int> state = await RunAsync<int>(
            "int total = 0; for (int i = 0; i < 5; i++) total += i; total");

        state.ReturnValue.Should().Be(10);
    }

    [Fact]
    public async Task CSharpScript_TimedCallback_FiresAndRuns()
    {
        await RunAsync("API.ScheduleTimedCallback(5, () => API.SetSharedVar(\"fired\", true), 0);");

        WaitUntil(() =>
        {
            _api.ProcessCallbacks();
            return _api.GetSharedVar("fired") != null;
        });
    }

    [Fact]
    public void ScheduleTimedCallback_WithRepeatZero_FiresExactlyOnce()
    {
        int calls = 0;
        _api.ScheduleTimedCallback(5, () => Interlocked.Increment(ref calls), 0);

        WaitUntil(() =>
        {
            _api.ProcessCallbacks();
            return Volatile.Read(ref calls) >= 1;
        });

        // Give a duplicate dispatch a window to surface, then confirm it never repeats.
        Thread.Sleep(100);
        _api.ProcessCallbacks();
        calls.Should().Be(1);
    }

    [Fact]
    public async Task CSharpScript_CanCreateGump_AndTrackIt()
    {
        ScriptState<ApiUiBaseGump> state = await RunAsync<ApiUiBaseGump>("API.CreateGump()");

        ApiUiBaseGump pyGump = state.ReturnValue;
        pyGump.Should().NotBeNull();
        _api._gumps.Should().Contain(pyGump.Gump);
    }

    [Fact]
    public async Task CSharpScript_CompileError_ThrowsCompilationErrorException()
    {
        Func<Task> run = () => RunAsync("int x = \"not a number\";");

        await run.Should().ThrowAsync<CompilationErrorException>();
    }

    private static void WaitUntil(Func<bool> condition, int timeoutMs = 5000)
    {
        DateTime start = DateTime.UtcNow;

        while (!condition())
        {
            if ((DateTime.UtcNow - start).TotalMilliseconds >= timeoutMs)
                throw new TimeoutException("Condition was not met within the timeout.");

            Thread.Sleep(10);
        }
    }
}
