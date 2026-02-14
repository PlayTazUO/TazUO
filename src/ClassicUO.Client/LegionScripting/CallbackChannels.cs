using System;
using Microsoft.Scripting.Hosting;

namespace ClassicUO.LegionScripting;

public interface ICallbackChannel
{
    bool CanInvoke(object callback);
    void Invoke(object callback, params object[] args);
}

public class CSharpCallbackChannel : ICallbackChannel
{
    public bool CanInvoke(object callback) => callback is Delegate;

    public void Invoke(object callback, params object[] args)
    {
        if (CanInvoke(callback))
            ((Delegate)callback).DynamicInvoke(args);
        ;
    }
}

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
