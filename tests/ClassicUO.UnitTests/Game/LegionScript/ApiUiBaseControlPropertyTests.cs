using ClassicUO.Game.UI.Controls;
using ClassicUO.LegionScripting;
using ClassicUO.LegionScripting.ApiClasses;
using ClassicUO.UnitTests.Fixtures;
using FluentAssertions;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Xunit;

namespace ClassicUO.UnitTests.Game.LegionScript;

/// <summary>
/// Drives the directly settable control properties through real Python, since scripts use
/// <c>label.X = 55</c> style assignment instead of the fluent <c>Set*</c> methods.
/// </summary>
/// <remarks>
/// Python runs on the xUnit thread, which is not the <see cref="ClassicUO.Game.Managers.MainThreadQueue" />
/// main thread, matching how Legion scripts actually run: setters enqueue and getters block until the
/// pump services them in order. <see cref="MainThreadFixture.Invoke" /> with an empty action is used
/// only as a flush barrier before inspecting the underlying control directly.
/// </remarks>
[Collection(MainThreadCollection.Name)]
public class ApiUiBaseControlPropertyTests
{
    #region Private members

    private readonly MainThreadFixture _mt;

    #endregion

    #region Ctor

    public ApiUiBaseControlPropertyTests(MainThreadFixture mt) => _mt = mt;

    #endregion

    #region Public methods

    [Fact]
    public void PythonProperties_SetAndReadBack_OnLabel()
    {
        Client.UnitTestingActive = true;
        (ScriptScope scope, LegionAPI api) = CreateApiScope();

        Run(scope, """
            gump = API.CreateGump()
            label = API.CreateGumpLabel("Hello")
            gump.Add(label)

            label.X = 55
            label.Y = 12
            label.Width = 100
            label.Height = 30
            label.Alpha = 0.5
            label.AcceptMouseInput = True

            x = label.X
            y = label.Y
            width = label.Width
            height = label.Height
            alpha = label.Alpha
            accepts = label.AcceptMouseInput
            """);

        // The Python getters above block on the queue (FIFO), so they already observed the
        // queued setters. This barrier just syncs before reading the control directly.
        _mt.Invoke(() => { });

        Control control = scope.GetVariable<ApiUiLabel>("label").Control;

        control.X.Should().Be(55);
        control.Y.Should().Be(12);
        control.Width.Should().Be(100);
        control.Height.Should().Be(30);
        control.Alpha.Should().Be(0.5f);
        control.AcceptMouseInput.Should().BeTrue();

        scope.GetVariable<int>("x").Should().Be(55);
        scope.GetVariable<int>("y").Should().Be(12);
        scope.GetVariable<int>("width").Should().Be(100);
        scope.GetVariable<int>("height").Should().Be(30);
        scope.GetVariable<double>("alpha").Should().BeApproximately(0.5, 1e-6);
        scope.GetVariable<bool>("accepts").Should().BeTrue();

        api.Dispose();
    }

    [Fact]
    public void PythonBounds_ReadsAndAssignsRect()
    {
        Client.UnitTestingActive = true;
        (ScriptScope scope, LegionAPI api) = CreateApiScope();

        Run(scope, """
            gump = API.CreateGump()
            label = API.CreateGumpLabel("Hello")
            gump.Add(label)
            label.SetRect(10, 20, 30, 40)

            bounds = label.Bounds
            x = bounds.X
            y = bounds.Y
            width = bounds.Width
            height = bounds.Height

            fromMethod = label.GetBounds()
            methodX = fromMethod.X

            moved = label.Bounds
            moved.X = 100
            moved.Width = 200
            movedX = moved.X
            movedWidth = moved.Width
            label.Bounds = moved

            readBack = label.Bounds
            readX = readBack.X
            readWidth = readBack.Width
            """);

        _mt.Invoke(() => { });

        ApiRect bounds = scope.GetVariable<ApiRect>("bounds");
        bounds.X.Should().Be(10);
        bounds.Y.Should().Be(20);
        bounds.Width.Should().Be(30);
        bounds.Height.Should().Be(40);
        scope.GetVariable<int>("x").Should().Be(10);
        scope.GetVariable<int>("y").Should().Be(20);
        scope.GetVariable<int>("width").Should().Be(30);
        scope.GetVariable<int>("height").Should().Be(40);
        scope.GetVariable<int>("methodX").Should().Be(10);

        scope.GetVariable<int>("movedX").Should().Be(100);
        scope.GetVariable<int>("movedWidth").Should().Be(200);
        scope.GetVariable<int>("readX").Should().Be(100);
        scope.GetVariable<int>("readWidth").Should().Be(200);

        Control control = scope.GetVariable<ApiUiLabel>("label").Control;
        control.X.Should().Be(100);
        control.Y.Should().Be(20);
        control.Width.Should().Be(200);
        control.Height.Should().Be(40);

        api.Dispose();
    }

    [Fact]
    public void PythonTooltipProperties_ApplyToControl()
    {
        Client.UnitTestingActive = true;
        (ScriptScope scope, LegionAPI api) = CreateApiScope();

        Run(scope, """
            gump = API.CreateGump()
            label = API.CreateGumpLabel("Hello")
            gump.Add(label)
            label.Tooltip = "a tooltip"
            """);

        _mt.Invoke(() => { });

        Control control = scope.GetVariable<ApiUiLabel>("label").Control;
        control.Tooltip.Should().Be("a tooltip");
        control.AcceptMouseInput.Should().BeTrue();

        Run(scope, "label.EntityTooltip = 123");
        _mt.Invoke(() => { });

        control.Tooltip.Should().Be(123u);

        api.Dispose();
    }

    #endregion

    #region Private methods

    private static (ScriptScope Scope, LegionAPI Api) CreateApiScope()
    {
        ScriptEngine engine = Python.CreateEngine();
        var api = new LegionAPI(new PythonCallbackChannel(engine), null);
        ScriptScope scope = engine.CreateScope();
        engine.GetBuiltinModule().SetVariable("API", api);

        return (scope, api);
    }

    private static void Run(ScriptScope scope, string python) =>
        scope.Engine.CreateScriptSourceFromString(python, SourceCodeKind.File).Execute(scope);

    #endregion
}
