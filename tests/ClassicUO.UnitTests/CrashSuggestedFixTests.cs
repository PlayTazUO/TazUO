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
    public void Get_SdlVideoInitZeroDisplays_ReturnsDisplayAdvice()
    {
        Exception inner = new InvalidOperationException("SDL_Init failed: The video driver did not add any displays");

        SetStackTrace(
            inner,
            "   at Microsoft.Xna.Framework.SDL3_FNAPlatform.ProgramInit(LaunchParameters args) in SDL3_FNAPlatform.cs:line 202\n" +
            "   at Microsoft.Xna.Framework.FNAPlatform..cctor() in FNAPlatform.cs:line 238");

        Exception exception = new TypeInitializationException("Microsoft.Xna.Framework.FNAPlatform", inner);

        string fix = CrashSuggestedFix.Get(exception);

        fix.Should().NotBeNullOrWhiteSpace();
        fix.Should().Contain("display");
        fix.Should().Contain("desktop session");
    }

    [Fact]
    public void Get_AnimationLoaderCrash_ReturnsMismatchedDataFilesAdvice()
    {
        Exception exception = CreateExceptionWithStackTrace(
            "   at ClassicUO.Assets.AnimationsLoader.ReadSpriteData(StackDataReader& reader, ReadOnlySpan`1 palette, FrameInfo& frame, Boolean alphaCheck) in AnimationsLoader.cs:line 1470\n" +
            "   at ClassicUO.Assets.AnimationsLoader.ReadMULAnimationFrames(Int32 fileIndex, AnimationDirection index) in AnimationsLoader.cs:line 1457");

        string fix = CrashSuggestedFix.Get(exception);

        fix.Should().NotBeNullOrWhiteSpace();
        fix.Should().Contain("animation");
        fix.Should().Contain("client version");
    }

    [Fact]
    public void Get_GumpsLoaderCrash_ReturnsMismatchedDataFilesAdvice()
    {
        Exception exception = CreateExceptionWithStackTrace(
            "   at ClassicUO.Assets.GumpsLoader.GetGump(UInt32 index) in GumpsLoader.cs:line 186\n" +
            "   at ClassicUO.Renderer.Gumps.Gump.GetGump(UInt32 idx) in Gump.cs:line 36");

        string fix = CrashSuggestedFix.Get(exception);

        fix.Should().NotBeNullOrWhiteSpace();
        fix.Should().Contain("gump");
        fix.Should().Contain("client version");
    }

    [Fact]
    public void Get_ReadOnlyFileSystemCrash_ReturnsWritableLocationAdvice()
    {
        var inner = new System.IO.IOException("Read-only file system : '/opt/tazuo/Fonts'");
        var exception = new AggregateException(inner);

        string fix = CrashSuggestedFix.Get(exception);

        fix.Should().NotBeNullOrWhiteSpace();
        fix.Should().Contain("read-only");
        fix.Should().Contain("write");
    }

    [Fact]
    public void Get_SdlVideoInitNoVideoDevice_ReturnsDisplayAdvice()
    {
        Exception inner = new Exception("SDL_Init failed: No available video device");

        SetStackTrace(
            inner,
            "   at Microsoft.Xna.Framework.SDL3_FNAPlatform.ProgramInit(LaunchParameters args) in SDL3_FNAPlatform.cs:line 202\n" +
            "   at Microsoft.Xna.Framework.FNAPlatform..cctor() in FNAPlatform.cs:line 238");

        Exception exception = new TypeInitializationException("Microsoft.Xna.Framework.FNAPlatform", inner);

        string fix = CrashSuggestedFix.Get(exception);

        fix.Should().NotBeNullOrWhiteSpace();
        fix.Should().Contain("video device");
        fix.Should().Contain("desktop session");
    }

    [Fact]
    public void Get_FontStashInt32MapCrash_ReturnsScriptThreadAdvice()
    {
        Exception exception = new IndexOutOfRangeException();

        SetStackTrace(
            exception,
            "   at FontStashSharp.Int32Map`1.Insert(Int32 key, TValue value, Boolean add) in Int32Map.cs:line 167\n" +
            "   at FontStashSharp.SpriteFontBase.InternalTextBounds(TextSource source, Vector2 position, Single characterSpacing, Single lineSpacing, FontSystemEffect effect, Int32 effectAmount) in SpriteFontBase.cs:line 103\n" +
            "   at ClassicUO.Game.UI.Controls.TextBox.get_Width() in TextBox.cs:line 211");

        string fix = CrashSuggestedFix.Get(exception);

        fix.Should().NotBeNullOrWhiteSpace();
        fix.Should().Contain("FontStashSharp");
        fix.Should().Contain("script");
    }

    [Fact]
    public void Get_NoStackTrace_ReturnsNull()
    {
        CrashSuggestedFix.Get(new InvalidOperationException("message")).Should().BeNull();
    }

    private static Exception CreateExceptionWithStackTrace(string stackTrace)
    {
        Exception exception = new InvalidOperationException("The calling thread cannot access this object because a different thread owns it.");

        SetStackTrace(exception, stackTrace);

        return exception;
    }

    private static void SetStackTrace(Exception exception, string stackTrace)
    {
        FieldInfo field = typeof(Exception).GetField("_stackTraceString", BindingFlags.NonPublic | BindingFlags.Instance);
        field!.SetValue(exception, stackTrace);
    }
}
