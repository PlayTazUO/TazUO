using System;
using ClassicUO.LegionScripting;
using ClassicUO.LegionScripting.ApiClasses;
using FluentAssertions;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Xunit;

namespace ClassicUO.UnitTests.Game.LegionScript;

/// <summary>
/// Executes real Python scripts through IronPython against the <see cref="LegionAPI"/>,
/// verifying the scripting layer that user scripts run on.
/// </summary>
[Collection(MainThreadCollection.Name)]
public class PythonScriptingTests : IDisposable
{
    private readonly LegionAPI _api;
    private readonly ScriptEngine _engine;
    private readonly ScriptScope _scope;

    public PythonScriptingTests()
    {
        Client.UnitTestingActive = true;
        _engine = Python.CreateEngine();
        _api = new LegionAPI(new PythonCallbackChannel(_engine), null);
        _scope = _engine.CreateScope();

        // Mirrors ScriptFile.SetupPythonScope: the API global is installed on the builtin module.
        _engine.GetBuiltinModule().SetVariable("API", _api);
    }

    public void Dispose() => _api.Dispose();

    private void Run(string python) =>
        _engine.CreateScriptSourceFromString(python, SourceCodeKind.File).Execute(_scope);

    [Fact]
    public void PythonScript_CanCallApi_AndReadResultsBack()
    {
        Run("""
            names = API.KnownAbilityNames()
            count = len(names)
            first = names[0]
            """);

        _scope.GetVariable<int>("count").Should().Be(_api.KnownAbilityNames().Length);
        _scope.GetVariable<string>("first").Should().Be("None");
    }

    [Fact]
    public void PythonScript_SharedVars_RoundTripFromCSharp()
    {
        _api.ClearSharedVars();

        Run("""
            API.SetSharedVar("hp", 50)
            API.SetSharedVar("target", "dragon")
            """);

        _api.GetSharedVar("hp").Should().Be(50);
        _api.GetSharedVar("target").Should().Be("dragon");

        Run("API.RemoveSharedVar('hp')");
        _api.GetSharedVar("hp").Should().BeNull();
    }

    [Fact]
    public void PythonScript_Callback_IsInvokedByProcessCallbacks()
    {
        Run("""
            def on_click():
                API.SetSharedVar("clicked", 99)

            cb = on_click
            """);

        object callback = _scope.GetVariable("cb");

        _api.ScheduleCallback(callback);
        _api.ProcessCallbacks();

        _api.GetSharedVar("clicked").Should().Be(99);
    }

    [Fact]
    public void PythonScript_CanCreateGump_AndTrackIt()
    {
        Run("""
            gump = API.CreateGump()
            label = API.CreateGumpLabel("Hello World")
            gump.Add(label)
            """);

        ApiUiBaseGump pyGump = _scope.GetVariable<ApiUiBaseGump>("gump");
        ApiUiLabel pyLabel = _scope.GetVariable<ApiUiLabel>("label");

        pyGump.Should().NotBeNull();
        pyLabel.Should().NotBeNull();
        _api._gumps.Should().Contain(pyGump.Gump);
    }

    [Fact]
    public void PythonScript_Random_ReturnsValueWithinRange()
    {
        Run("roll = API.Random.Next(1, 101)");

        int roll = _scope.GetVariable<int>("roll");
        roll.Should().BeInRange(1, 100);
    }

    [Fact]
    public void PythonScript_IgnoreList_CanBeToggled()
    {
        Run("""
            API.IgnoreObject(1234)
            ignored = API.OnIgnoreList(1234)
            API.UnIgnoreObject(1234)
            notIgnored = API.OnIgnoreList(1234)
            """);

        _scope.GetVariable<bool>("ignored").Should().BeTrue();
        _scope.GetVariable<bool>("notIgnored").Should().BeFalse();
    }

    [Fact]
    public void PythonScript_SyntaxError_ThrowsSyntaxErrorException()
    {
        Action run = () => Run("def broken(:");

        run.Should().Throw<SyntaxErrorException>();
    }
}
