using System;
using Microsoft.Scripting.Hosting;

namespace ClassicUO.LegionScripting;

/// <summary>
/// A script callback channel.
/// <remarks>
/// Script callback channels are used by the <see cref="LegionAPI"/> to invoke user script callbacks in a language-agnostic manner.
/// </remarks>
/// </summary>
public interface ICallbackChannel
{
    /// <summary>
    /// Determines whether the given callback can be invoked via this channel
    /// </summary>
    /// <param name="callback">The callback being tested</param>
    /// <returns>True if the callback can be invoked, false otherwise</returns>
    bool CanInvoke(object callback);

    /// <summary>
    /// Invokes the given callback via this channel
    /// </summary>
    /// <param name="callback">The callback to be invoked</param>
    /// <param name="args">The callback's arguments</param>
    void Invoke(object callback, params object[] args);
}

/// <summary>
/// A callback channel for C# scripts
/// </summary>
public class CSharpCallbackChannel : ICallbackChannel
{
    public bool CanInvoke(object callback) => callback is Delegate;

    public void Invoke(object callback, params object[] args)
    {
        if (CanInvoke(callback))
            ((Delegate)callback).DynamicInvoke(args);
    }
}

/// <summary>
/// A callback channel for Python scripts
/// </summary>
public class PythonCallbackChannel : ICallbackChannel
{
    private readonly ScriptEngine _engine;

    public bool CanInvoke(object callback) => _engine.Operations?.IsCallable(callback) == true;

    public void Invoke(object callback, params object[] args)
    {
        if (CanInvoke(callback))
            _engine.Operations.Invoke(callback, args);
    }

    public PythonCallbackChannel(ScriptEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        _engine = engine;
    }
}
