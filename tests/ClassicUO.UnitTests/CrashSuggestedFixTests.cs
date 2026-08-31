using System;
using System.Reflection;
using ClassicUO;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests;

public class CrashSuggestedFixTests
{
    [Fact]
    public void Get_PluginBackgroundThreadCrash_ReturnsPluginAdvice()
    {
        Exception exception = CreateExceptionWithStackTrace(
            "   at Avalonia.Threading.Dispatcher.VerifyAccess()\n" +
            "   at Avalonia.Win32.Win32Platform.Initialize(Win32PlatformOptions options)\n" +
            "   at UoCore.Ui.Program.Start() in D:\\Cloud\\Projects\\Games\\UO\\UoCore\\src\\UoCore.Ui\\Program.cs:line 30\n" +
            "   at UoCore.Ui.Program.<>c__DisplayClass16_0.<ShowWindow>b__3() in D:\\Cloud\\Projects\\Games\\UO\\UoCore\\src\\UoCore.Ui\\Program.cs:line 163\n" +
            "   at System.Threading.Thread.StartHelper.Callback(Object state)\n" +
            "   at System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state)");

        string fix = CrashSuggestedFix.Get(exception);

        fix.Should().NotBeNullOrWhiteSpace();
        fix.Should().Contain("background thread");
        fix.Should().Contain("plugin");
    }

    [Fact]
    public void Get_TazUoOwnBackgroundThread_ReturnsNull()
    {
        Exception exception = CreateExceptionWithStackTrace(
            "   at ClassicUO.Game.Managers.MapWebServer.ListenerLoop()\n" +
            "   at System.Threading.Thread.StartHelper.Callback(Object state)");

        CrashSuggestedFix.Get(exception).Should().BeNull();
    }

    [Fact]
    public void Get_ScriptBackgroundThread_ReturnsNull()
    {
        Exception exception = CreateExceptionWithStackTrace(
            "   at ClassicUO.LegionScripting.LegionScripting.ExecutePythonScript(Object obj)\n" +
            "   at IronPython.Runtime.PythonThread.Run()\n" +
            "   at System.Threading.Thread.StartHelper.Callback(Object state)");

        CrashSuggestedFix.Get(exception).Should().BeNull();
    }

    [Fact]
    public void Get_MainThreadCrash_ReturnsNull()
    {
        Exception exception = CreateExceptionWithStackTrace(
            "   at ClassicUO.Game.Scenes.GameScene.ChatOnMessageReceived(Object sender, MessageEventArgs e)\n" +
            "   at ClassicUO.Game.Managers.MessageManager.HandleMessage(Entity parent, String text, String name, UInt16 hue, MessageType type, Byte font, TextType textType, Boolean unicode, String lang, Boolean skipEventTrigger)");

        CrashSuggestedFix.Get(exception).Should().BeNull();
    }

    [Fact]
    public void Get_NoStackTrace_ReturnsNull()
    {
        CrashSuggestedFix.Get(new InvalidOperationException("message")).Should().BeNull();
    }

    private static Exception CreateExceptionWithStackTrace(string stackTrace)
    {
        Exception exception = new InvalidOperationException("The calling thread cannot access this object because a different thread owns it.");

        FieldInfo field = typeof(Exception).GetField("_stackTraceString", BindingFlags.NonPublic | BindingFlags.Instance);
        field!.SetValue(exception, stackTrace);

        return exception;
    }
}
